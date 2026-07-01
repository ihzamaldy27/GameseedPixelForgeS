using UnityEngine;
using System.Collections;

public class BossDeathHandler : MonoBehaviour
{
    [Header("Death Sequence Settings")]
    [SerializeField] private int smallExplosionCount = 6;
    [SerializeField] private float smallExplosionInterval = 0.3f;
    [SerializeField] private float smallExplosionRadius = 1.5f;
    [SerializeField] private float smallExplosionScale = 1.2f;
    [SerializeField] private float bigExplosionScale = 3f;

    [Header("Timing")]
    [SerializeField] private float delayBeforeSmallExplosions = 0.5f;
    [SerializeField] private float delayAfterSmallExplosions = 0.5f;

    private BossController _boss;
    private BossMovement _movement;
    private BossAttackManager _attackManager;
    private SpriteRenderer[] _renderers;
    private Collider2D[] _colliders;
    private bool _isDying = false;

    private void Awake()
    {
        _boss = GetComponent<BossController>();
        _movement = GetComponent<BossMovement>();
        _attackManager = GetComponent<BossAttackManager>();
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        _colliders = GetComponentsInChildren<Collider2D>();

        if (_boss != null)
            _boss.OnDeathStarted += HandleDeathStarted;
    }

    private void OnDestroy()
    {
        if (_boss != null)
            _boss.OnDeathStarted -= HandleDeathStarted;
    }

    private void HandleDeathStarted()
    {
        if (_isDying) return;
        _isDying = true;

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. Stop all movement and attacks
        if (_movement != null)
            _movement.PauseMovement();

        if (_attackManager != null)
            _attackManager.StopAttacks();

        // 2. Disable collision and renderers (they'll be re-enabled for explosions)
        //SetVisibility(false);
        SetCollision(false);

        // 3. Small delay before explosions start
        yield return new WaitForSeconds(delayBeforeSmallExplosions);

        // 4. Spawn small explosions around the boss
        for (int i = 0; i < smallExplosionCount; i++)
        {
            // Random position within radius around boss
            Vector2 randomOffset = Random.insideUnitCircle * smallExplosionRadius;
            Vector3 explosionPosition = transform.position + (Vector3)randomOffset;

            // Spawn small explosion
            if (ExplosionPoolManager.Instance != null)
            {
                ExplosionPoolManager.Instance.SpawnExplosion(explosionPosition, smallExplosionScale);
                AudioManager.instance.PlaySFX("Explode");
            }

            // Small camera shake or screen flash could be added here

            yield return new WaitForSeconds(smallExplosionInterval);
        }

        // 5. Small delay before big explosion
        yield return new WaitForSeconds(delayAfterSmallExplosions);

        // 6. Big explosion at boss center
        if (ExplosionPoolManager.Instance != null)
        {
            ExplosionPoolManager.Instance.SpawnExplosion(transform.position, bigExplosionScale);
            AudioManager.instance.PlaySFX("Boss Explosion");
        }

        // 7. Brief pause for dramatic effect
        yield return new WaitForSeconds(0.2f);

        // 8. Destroy the boss
        Destroy(gameObject);
    }

    private void SetVisibility(bool visible)
    {
        foreach (var renderer in _renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    private void SetCollision(bool enabled)
    {
        foreach (var collider in _colliders)
        {
            if (collider != null)
                collider.enabled = enabled;
        }
    }

    // Optional: if you want to respawn the boss (for debugging)
    public void ResetDeathSequence()
    {
        _isDying = false;
        SetVisibility(true);
        SetCollision(true);
    }
}