using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PlayerHitEffect : MonoBehaviour
{
    [Header("Hit Flash")]
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("Invincibility Flicker")]
    [SerializeField] private float flickerInterval = 0.1f; // time between blinks
    [SerializeField] private bool flickerEnabled = true;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private PlayerPlane _player;
    private Color _originalColor;
    private Coroutine _hitFlashCoroutine;
    private Coroutine _flickerCoroutine;
    private bool _isInvincible = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogError("PlayerHitEffect requires a SpriteRenderer component!");
            return;
        }

        _originalColor = spriteRenderer.color;
        _player = GetComponent<PlayerPlane>();

        if (_player != null)
        {
            // Subscribe to health events
            _player.OnDamaged += HandlePlayerDamaged;
            _player.OnInvincibilityStateChanged += HandleInvincibilityStateChanged;
        }
        else
        {
            Debug.LogError("PlayerHitEffect requires a PlayerPlane component!");
        }
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDamaged -= HandlePlayerDamaged;
            _player.OnInvincibilityStateChanged -= HandleInvincibilityStateChanged;
        }
    }

    private void HandlePlayerDamaged(int currentHP)
    {
        // Play hit flash
        if (_hitFlashCoroutine != null)
            StopCoroutine(_hitFlashCoroutine);
        _hitFlashCoroutine = StartCoroutine(HitFlash());
    }

    private void HandleInvincibilityStateChanged(bool isInvincible)
    {
        _isInvincible = isInvincible;

        if (isInvincible && flickerEnabled)
        {
            // Start flickering
            if (_flickerCoroutine != null)
                StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = StartCoroutine(InvincibilityFlicker());
        }
        else
        {
            // Stop flickering and restore visibility
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }
            SetSpriteVisibility(true);
        }
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;

        // Flash to hit color
        spriteRenderer.color = hitColor;
        yield return new WaitForSeconds(hitFlashDuration);

        // Return to original color (or current flicker state)
        if (!_isInvincible || !flickerEnabled)
        {
            spriteRenderer.color = _originalColor;
        }
        // If invincible, the flicker coroutine will handle visibility
    }

    private IEnumerator InvincibilityFlicker()
    {
        if (spriteRenderer == null) yield break;

        bool visible = true;
        while (_isInvincible)
        {
            // Toggle visibility
            visible = !visible;
            SetSpriteVisibility(visible);
            yield return new WaitForSeconds(flickerInterval);
        }

        // Ensure sprite is visible when invincibility ends
        SetSpriteVisibility(true);
    }

    private void SetSpriteVisibility(bool visible)
    {
        if (spriteRenderer == null) return;

        if (visible)
        {
            // Restore to original color (or hit flash color if just hit)
            spriteRenderer.color = _originalColor;
        }
        else
        {
            // Make transparent
            Color transparent = _originalColor;
            transparent.a = 0f;
            spriteRenderer.color = transparent;
        }
    }

    // Optional: public method to manually reset effects
    public void ResetEffects()
    {
        if (_hitFlashCoroutine != null)
        {
            StopCoroutine(_hitFlashCoroutine);
            _hitFlashCoroutine = null;
        }
        if (_flickerCoroutine != null)
        {
            StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = null;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = _originalColor;
            SetSpriteVisibility(true);
        }
        _isInvincible = false;
    }
}