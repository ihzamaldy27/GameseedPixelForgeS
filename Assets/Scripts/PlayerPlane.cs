using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

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

    public GameObject healthBarUI; // Referensi ke UI Health Bar
    [SerializeField] private GameObject[] healthSegments; // Array untuk menyimpan segmen-segmen health bar 

    [Header("Sprite")]
    [SerializeField] private SpriteDirectionComponent spriteHandler;

    [Header("Screen Effects (Vignette & Fade)")]
    public CanvasGroup vignetteGroup;
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1.5f;        // Lama waktu layar menjadi gelap
    public float vignettePulseSpeed = 3f;  // Kecepatan detak vignette
    private bool isLowHealth = false;

    public event System.Action OnPlayerDied;
    public event System.Action<int> OnDamaged; // currentHP
    public event System.Action<int> OnHealthRestored; // currentHP (if you add healing)
    public event System.Action<bool> OnInvincibilityStateChanged; // isInvincible

    // Public properties for UI
    public int CurrentHP => _health?.CurrentHP ?? 0;
    public int MaxHP => _health?.MaxHP ?? maxHP;

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

        if (healthBarUI != null)
        {
            int childCount = healthBarUI.transform.childCount;
            healthSegments = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                healthSegments[i] = healthBarUI.transform.GetChild(i).gameObject;
            }
        }

        // Subscribe to health events
        _health.OnDamaged += HandleDamaged;
        _health.OnDeath += HandleDeath;
    }

    private void Update()
    {
        //Vignette effect when low health
        if (isLowHealth && vignetteGroup != null && _health != null && !_health.IsDead)
        {
            vignetteGroup.alpha = 0.55f + Mathf.Sin(Time.time * vignettePulseSpeed) * 0.25f;
        }

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
        bool wasInvincible = _health.IsInvincible;
        _health.UpdateInvincibility(Time.deltaTime);
        bool isInvincible = _health.IsInvincible;

        // Notify if invincibility state changed
        if (wasInvincible != isInvincible)
        {
            OnInvincibilityStateChanged?.Invoke(isInvincible);

        }

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
        OnDamaged?.Invoke(currentHP);
        UpdateHealthUI();
        // Optional: Trigger a sprite flash, sound, or UI update here.
        // You could raise a separate C# event here if other systems need to know.
        if (currentHP > 0)
            OnInvincibilityStateChanged?.Invoke(true);
    }

    // Optional: if you add health restoration (e.g., health pickups)
    public void RestoreHealth(int amount)
    {
        // You would add this logic to HealthHandler
        // Then trigger OnHealthRestored event
    }

    private void HandleDeath()
    {
        Debug.Log("Player has died!");
        AudioManager.instance.PlaySFX("Explode");
        enabled = false;
        OnPlayerDied?.Invoke();
        isLowHealth = false;
        if (vignetteGroup != null) vignetteGroup.alpha = 0f;
        if (fadeGroup != null) StartCoroutine(FadeOutRoutine());

        // Optional: Destroy the player after a delay
        // Destroy(gameObject, 1f);
    }

    public void EnableControls()
    {
        enabled = true;
    }

    public void DisableControls()
    {
        enabled = false;
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
        UpdateHealthUI(); // Perbarui UI agar penuh lagi
        if (fadeGroup != null) StartCoroutine(FadeInRoutine());
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

    private void UpdateHealthUI()
    {
        if (healthSegments == null || _health == null) return;

        for (int i = 0; i < healthSegments.Length; i++)
        {
            if (healthSegments[i] != null)
            {
                healthSegments[i].SetActive(i < _health.CurrentHP);
            }
        }

        if (_health != null)
        {
            // Jika darah 2 atau 1, nyalakan status sekarat
            if (_health.CurrentHP <= 2 && _health.CurrentHP > 0)
            {
                isLowHealth = true;
            }
            else
            {
                isLowHealth = false;
                if (vignetteGroup != null) vignetteGroup.alpha = 0f; // SANGAT PENTING: Bersihkan layar jika darah tidak sekarat
            }
        }
    }

    // --- EFEK LAYAR (FADE) ---
    private IEnumerator FadeOutRoutine()
    {
        if (fadeGroup == null) yield break;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f; // Pastikan benar-benar gelap
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeGroup == null) yield break;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f; // Pastikan benar-benar transparan
    }
}
