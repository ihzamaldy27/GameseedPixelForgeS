using UnityEngine;
using System.Collections;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionScale = 1.5f;

    [Header("Fade Transition")]
    [SerializeField] private FadeTransition fadeTransition;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    private PlayerPlane _player;
    private CheckpointManager _checkpointManager;
    private bool _isRespawning = false;

    private void Awake()
    {
        _player = GetComponent<PlayerPlane>();
        if (_player == null)
            Debug.LogError("PlayerDeathHandler requires PlayerPlane component.");

        _checkpointManager = FindAnyObjectByType<CheckpointManager>();
        if (_checkpointManager == null)
            Debug.LogError("CheckpointManager not found in scene!");

        if (fadeTransition == null)
            fadeTransition = FindAnyObjectByType<FadeTransition>();

        // Subscribe to player death event
        if (_player != null)
            _player.OnPlayerDied += HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnPlayerDied -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        if (_isRespawning) return;
        
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        _isRespawning = true;

        // 1. Spawn explosion at player position
        if (ExplosionPoolManager.Instance != null)
        {
            ExplosionPoolManager.Instance.SpawnExplosion(transform.position, explosionScale);
        }

        // 2. Hide the player (disable renderer and collider)
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var renderer in renderers)
            renderer.enabled = false;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
            collider.enabled = false;

        // 3. Fade out (if transition available)
        if (fadeTransition != null)
            yield return StartCoroutine(fadeTransition.FadeOut());

        // 4. Wait briefly (optional)
        yield return new WaitForSeconds(0.3f);

        // 5. Respawn at checkpoint
        if (_checkpointManager != null)
            _checkpointManager.RespawnAtCheckpoint();

        // 6. Wait for respawn to complete (player will be repositioned)
        yield return new WaitForSeconds(0.2f);

        // 7. Show player again
        foreach (var renderer in renderers)
            renderer.enabled = true;

        foreach (var collider in colliders)
            collider.enabled = true;

        // 8. Fade in
        if (fadeTransition != null)
            yield return StartCoroutine(fadeTransition.FadeIn());

        _isRespawning = false;
    }
}