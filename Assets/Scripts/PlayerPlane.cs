using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPlane : MonoBehaviour, IDamageable, PlaneControl.IPlayerActions
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Shooting Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private AudioClip shootSFX;

    [Header("Health Settings")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Sprite")]
    [SerializeField] private SpriteDirectionComponent spriteHandler;

    public event System.Action OnPlayerDied;

    // Composition: the player owns separate handlers for movement and shooting
    private PlaneControlComponent _movement;
    private ShootingComponent _shooting;
    private HealthComponent _health;
    private Vector2 _direction;
    private bool _firePressed;

    private void Awake()
    {
        // Instantiate the composed objects, passing any needed dependencies
        _movement = new PlaneControlComponent(transform, moveSpeed);
        _shooting = new ShootingComponent(firePoint, bulletPrefab, fireRate, shootSFX);
        _health = new HealthComponent(maxHP, invincibilityDuration);

        // Subscribe to health events
        _health.OnDamaged += HandleDamaged;
        _health.OnDeath += HandleDeath;
    }

    private void Update()
    {
        // If dead, stop processing input
        if (_health.IsDead) return;

        // Read input
        float horizontal = _direction.x;
        float vertical = _direction.y;
        bool firePressed = _firePressed;

        // Delegate to the composed handlers
        _movement.Move(horizontal, vertical);
        _shooting.HandleShoot(firePressed);

        // Update invincibility frames
        _health.UpdateInvincibility(Time.deltaTime);

        // Update sprite based on movement direction
        if (spriteHandler != null)
            spriteHandler.SetDirection(_direction);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _direction = context.ReadValue<Vector2>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        float _value = context.ReadValue<float>();
        _firePressed = _value > 0 ? true : false;
    }

    // --- IDamageable Implementation ---
    public void TakeDamage(int damage)
    {
        _health.TakeDamage(damage);
    }

    // --- Event Handlers ---
    private void HandleDamaged(int currentHP)
    {
        Debug.Log($"Player hit! HP: {currentHP}");
        // Optional: Trigger a sprite flash, sound, or UI update here.
        // You could raise a separate C# event here if other systems need to know.
    }

    private void HandleDeath()
    {
        Debug.Log("Player has died!");
        AudioManager.instance.PlaySFX("Explode");
        enabled = false;
        OnPlayerDied?.Invoke();

        // Optional: Destroy the player after a delay
        // Destroy(gameObject, 1f);
    }

    public void Respawn(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;

        // Recreate health component to reset HP and invincibility
        _health = new HealthComponent(maxHP, invincibilityDuration);
        _health.OnDamaged += HandleDamaged;
        _health.OnDeath += HandleDeath;

        // Reset other state (e.g., shooting cooldown, if needed)
        // For simplicity, we recreate shooting component as well
        if (bulletPrefab != null && firePoint != null)
        {
            _shooting = new ShootingComponent(firePoint, bulletPrefab, fireRate, shootSFX);
        }

        enabled = true; // re-enable controls
    }

    // Optional: Unsubscribe from events when destroyed to avoid memory leaks
    private void OnDestroy()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnDeath -= HandleDeath;
        }
    }
}
