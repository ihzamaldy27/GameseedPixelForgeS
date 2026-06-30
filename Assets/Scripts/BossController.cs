using UnityEngine;

public class BossController : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHP = 20;
    [SerializeField] private float invincibilityDuration = 0.2f;

    [Header("Explosion")]
    [SerializeField] private float explosionScale = 2f; // bigger explosion

    [Header("Components")]
    [SerializeField] private BossMovement movement; // assign in inspector
    [SerializeField] private BossAttackManager attackManager;

    private HealthComponent _health;

    // Events for sprite handler
    public event System.Action<int, int> OnHealthChanged; // currentHP, maxHP
    public int CurrentHP => _health?.CurrentHP ?? 0;
    public int MaxHP => _health?.MaxHP ?? maxHP;

    private void Awake()
    {
        _health = new HealthComponent(maxHP, invincibilityDuration);
        _health.OnDamaged += (currentHP) => OnHealthChanged?.Invoke(currentHP, maxHP);
        _health.OnDeath += HandleDeath;
    }

    private void Update()
    {
        // Movement is handled by BossMovement's own Update
        // We only update invincibility frames if needed (health component updates itself)
        _health.UpdateInvincibility(Time.deltaTime);
    }

    // IDamageable implementation
    public void TakeDamage(int damage)
    {
        _health.TakeDamage(damage);
        // Optionally flash or trigger hit effects here
    }

    private void HandleDeath()
    {
        // Stop attacks when boss dies
        if (attackManager != null)
            attackManager.StopAttacks();

        // Spawn explosion
        if (ExplosionPoolManager.Instance != null)
            ExplosionPoolManager.Instance.SpawnExplosion(transform.position, explosionScale);

        // Destroy the boss
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnDeath -= HandleDeath;
    }

    // Collision with player bullets
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            BulletController bullet = other.GetComponent<BulletController>();
            if (bullet != null)
            {
                TakeDamage(bullet.damage);
                bullet.ReturnToPool();
            }
        }
    }
}