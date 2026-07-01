using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHP = 2;
    [SerializeField] private float invincibilityDuration = 0.1f;

    [Header("Shooting (Optional)")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 1.5f;

    [Header("Explosion")]
    [SerializeField] private float explosionScale = 1f; // set 2 for elite

    [Header("Sprite")]
    [SerializeField] private SpriteDirectionComponent spriteHandler;

    // Composed objects
    private IMovementPattern _movementPattern;
    private HealthComponent _health;
    private ShootingComponent _shooting; // optional

    // Reference to the pool that owns this instance
    private EnemyPool _ownerPool;

    public EnemyController OriginalPrefab { get; set; } // set by pool manager

    public event System.Action<int, int> OnHealthChanged; // currentHP, maxHP

    private Vector3 _previousPosition;

    public int CurrentHP => _health.CurrentHP;

    public int MaxHP => _health.MaxHP;

    // Setter for pool owner (called by pool when getting)
    public void SetOwnerPool(EnemyPool pool)
    {
        _ownerPool = pool;
    }

    // Setter for the movement pattern (called by WaveManager after instantiation)
    public void SetMovementPattern(IMovementPattern pattern)
    {
        _movementPattern = pattern;
    }

    private void Awake()
    {
        _health = new HealthComponent(maxHP, invincibilityDuration);
        _health.OnDamaged += (currentHP) => OnHealthChanged?.Invoke(currentHP, maxHP);
        _health.OnDeath += HandleDeath;

        // If bulletPrefab is assigned, enable shooting
        if (bulletPrefab != null && firePoint != null)
        {
            _shooting = new ShootingComponent(firePoint, bulletPrefab, fireRate);
        }
    }

    private void OnEnable()
    {
        // Reset previous position when reused from pool
        _previousPosition = transform.position;
    }

    // Call this before reusing the enemy
    public void ResetState()
    {
        // Reset health (create new HealthComponent or reset internally)
        _health = new HealthComponent(maxHP, invincibilityDuration);
        _health.OnDamaged += (currentHP) => OnHealthChanged?.Invoke(currentHP, maxHP);
        _health.OnDeath += HandleDeath;

        // Reset any other state (e.g., position, rotation)
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // Reset movement pattern to null (must be set by spawner)
        _movementPattern = null;

        // Optionally reset shooting cooldown if ShootingComponent exposes reset
        // For simplicity, we recreate the shooting component
        if (bulletPrefab != null && firePoint != null)
        {
            _shooting = new ShootingComponent(firePoint, bulletPrefab, fireRate);
        }

        _previousPosition = transform.position;
    }

    private void Update()
    {
        // Store previous position before movement
        _previousPosition = transform.position;

        // Update movement if pattern exists
        _movementPattern?.UpdateMovement(transform, Time.deltaTime);

        // Compute movement direction from actual position delta
        Vector3 delta = transform.position - _previousPosition;
        if (spriteHandler != null)
        {
            if (delta.magnitude > 0.001f)
                spriteHandler.SetDirection(delta.normalized);
            else
                spriteHandler.SetDirection(Vector2.zero); // fallback to straight
        }

        // Update invincibility
        _health.UpdateInvincibility(Time.deltaTime);

        // Optionally shoot (e.g., every frame if shoot is automatic)
        if (_shooting != null)
        {
            // Here we make enemies shoot automatically; you could add a cooldown based on pattern
            _shooting.HandleShoot(true); // Always shoot when possible
        }
    }

    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        _health.TakeDamage(damage);

        // Workaround: The hit impact only works on elite and boss enemy
        if (maxHP > 2)
            AudioManager.instance.PlaySFX("Hit Impact");
    }

    public void ReturnToPool()
    {
        if (EnemyPoolManager.Instance != null)
            EnemyPoolManager.Instance.ReturnEnemy(this);
        else
        {
            Destroy(gameObject);
        }
    }

    private void HandleDeath()
    {
        // Spawn explosion at current position with the specified scale
        if (ExplosionPoolManager.Instance != null)
            ExplosionPoolManager.Instance.SpawnExplosion(transform.position, explosionScale);

        AudioManager.instance.PlaySFX("Explode");

        ReturnToPool();
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    // Optional: OnTriggerEnter2D for player bullets (tag "PlayerBullet")
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            //TakeDamage(1);
            //Destroy(other.gameObject);
            BulletController bullet = other.GetComponent<BulletController>();

            if (bullet != null)
            {
                TakeDamage(bullet.damage);
                bullet.ReturnToPool(); // recycle the bullet
            }
        }
    }
}
