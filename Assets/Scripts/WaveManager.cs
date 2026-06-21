using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave List")]
    public List<WaveDefinition> waves = new List<WaveDefinition>();

    [Header("Spawner Settings")]
    public Transform spawnPoint; // spawn position reference (e.g., right edge)

    [Header("Pool Reference")]
    public EnemyPool enemyPool; // assign in inspector

    private int _currentWaveIndex = 0;
    private bool _isSpawning = false;

    private void Start()
    {
        StartCoroutine(StartNextWave());
    }

    private IEnumerator StartNextWave()
    {
        while (_currentWaveIndex < waves.Count)
        {
            _isSpawning = true;
            WaveDefinition wave = waves[_currentWaveIndex];
            Debug.Log($"Starting Wave {_currentWaveIndex + 1}");

            float startTime = Time.time;
            foreach (SpawnEvent spawn in wave.spawnEvents)
            {
                // Wait until the spawn time
                float waitTime = spawn.time - (Time.time - startTime);
                if (waitTime > 0)
                    yield return new WaitForSeconds(waitTime);

                // Spawn the enemy
                SpawnEnemy(spawn);
            }

            // Wait a bit before next wave (optional)
            yield return new WaitForSeconds(2f);
            _currentWaveIndex++;
        }

        Debug.Log("All waves completed!");
        _isSpawning = false;
    }

    private void SpawnEnemy(SpawnEvent spawn)
    {
        if (spawn.enemyPrefab == null || enemyPool == null) return;

        // Get enemy from pool
        EnemyController enemy = enemyPool.GetEnemy();
        // Set the owner pool so it knows where to return
        enemy.SetOwnerPool(enemyPool);

        // Position it
        Vector3 spawnPos = spawnPoint.position + (Vector3)spawn.spawnPosition;
        enemy.transform.position = spawnPos;

        // Reset internal state
        enemy.ResetState();

        // Set movement pattern based on spawn event
        IMovementPattern pattern = CreatePattern(spawn);
        enemy.SetMovementPattern(pattern);
    }

    private IMovementPattern CreatePattern(SpawnEvent spawn)
    {
        switch (spawn.patternType)
        {
            case MovementPatternType.Straight:
                return new StraightPattern(spawn.speed);
            case MovementPatternType.Sine:
                return new SinePattern(spawn.speed, spawn.amplitude, spawn.frequency);
            default:
                return new StraightPattern(spawn.speed);
        }
    }

    // Optional: Method to force start next wave manually (for testing)
    public void SkipWave() { /* ... */ }
}