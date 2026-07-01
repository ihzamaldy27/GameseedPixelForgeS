using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Waves (0‑based indices)")]
    [SerializeField] private List<int> checkpointWaves = new List<int>(); // e.g., 2,5,8

    [Header("Respawn Settings")]
    [SerializeField] private Transform playerSpawnPoint;

    private int _currentCheckpointWave = 0; // start at wave 0
    private WaveManager _waveManager;
    private PlayerPlane _player;

    private void Awake()
    {
        _waveManager = FindAnyObjectByType<WaveManager>();
        _player = FindAnyObjectByType<PlayerPlane>();

        if (_player != null)
            _player.OnPlayerDied += HandlePlayerDied;
        if (_waveManager != null)
            _waveManager.OnWaveComplete += HandleWaveComplete;
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnPlayerDied -= HandlePlayerDied;
        if (_waveManager != null)
            _waveManager.OnWaveComplete -= HandleWaveComplete;
    }

    private void HandleWaveComplete(int waveIndex)
    {
        // If this wave is a checkpoint, advance to the next wave
        if (checkpointWaves.Contains(waveIndex))
        {
            _currentCheckpointWave = waveIndex + 1;
            Debug.Log($"Checkpoint reached at wave {_currentCheckpointWave}");
        }
    }

    private void HandlePlayerDied()
    {
        RespawnAtCheckpoint();
    }

    public void RespawnAtCheckpoint()
    {
        if (_player == null || _waveManager == null) return;

        // Clear all active enemies (return to pool)
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.ReturnToPool();
        }

        // Clean up bosses via WaveManager
        _waveManager.CleanUpBosses();

        // Additional cleanup: find any boss death handlers that might be running
        BossDeathHandler[] deathHandlers = FindObjectsByType<BossDeathHandler>(FindObjectsSortMode.None);
        foreach (var handler in deathHandlers)
        {
            if (handler != null && handler.gameObject != null)
            {
                // Stop any coroutines and destroy the GameObject
                Destroy(handler.gameObject);
            }
        }

        // Also destroy any BossController that might have been missed
        BossController[] bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        foreach (var boss in bosses)
        {
            if (boss != null && boss.gameObject != null)
                Destroy(boss.gameObject);
        }

        // Reset player victory dash state if active
        PlayerVictoryDash victoryDash = _player.GetComponent<PlayerVictoryDash>();
        if (victoryDash != null)
            victoryDash.ResetState();

        // Reset player
        _player.Respawn(playerSpawnPoint.position);

        // Restart wave from checkpoint
        _waveManager.RestartFromWave(_currentCheckpointWave);

        Debug.Log($"Respawned at checkpoint wave {_currentCheckpointWave}");
    }
}