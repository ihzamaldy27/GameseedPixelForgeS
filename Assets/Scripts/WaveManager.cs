using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    [Header("Wave List")]
    public List<WaveDefinition> waves = new List<WaveDefinition>();

    [Header("Spawner Settings")]
    public Transform spawnPoint; // spawn position reference (e.g., right edge)

    public event System.Action<int> OnWaveComplete; // passes index of completed wave

    private int _currentWaveIndex = 0;
    private bool _isSpawning = false;
    private List<EnemyController> _activeEnemies = new List<EnemyController>();

    private void Start()
    {
        StartCoroutine(StartNextWave());
    }

    private IEnumerator StartNextWave()
    {
        while (_currentWaveIndex < waves.Count)
        {
            _isSpawning = true;
            _activeEnemies.Clear(); // clear list for new wave
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

            // Wait until all enemies are cleared (destroyed or returned to pool)
            while (_activeEnemies.Count > 0)
            {
                // Remove any enemies that are no longer active (pool deactivates them)
                _activeEnemies.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);
                yield return null; // wait one frame
            }

            // Wait a bit before next wave (optional)
            yield return new WaitForSeconds(0.5f);
            OnWaveComplete?.Invoke(_currentWaveIndex);
            _currentWaveIndex++;
        }

        Debug.Log("All waves completed!");
        _isSpawning = false;
    }

    private void SpawnEnemy(SpawnEvent spawn)
    {
        if (spawn.enemyPrefab == null) return;

        EnemyController enemy = EnemyPoolManager.Instance.GetEnemy(spawn.enemyPrefab.GetComponent<EnemyController>());
        if (enemy == null) return;


        // Reset internal state
        enemy.ResetState();

        // Position it
        Vector3 spawnPos = spawnPoint.position + (Vector3)spawn.spawnPosition;
        enemy.transform.position = spawnPos;

        // Set movement pattern
        IMovementPattern pattern = CreatePattern(spawn);
        enemy.SetMovementPattern(pattern);

        // Add to active list so we can track when it's cleared
        _activeEnemies.Add(enemy);
    }

    public void RestartFromWave(int waveIndex)
    {
        if (_isSpawning)
        {
            StopAllCoroutines();
            _isSpawning = false;
        }
         _activeEnemies.Clear(); // clear any remaining references
        _currentWaveIndex = waveIndex;
        StartCoroutine(StartNextWave());
    }

    private IMovementPattern CreatePattern(SpawnEvent spawn)
    {
        switch (spawn.patternType)
        {
            case MovementPatternType.Straight:
                return new StraightPattern(spawn.speed);
            case MovementPatternType.Sine:
                return new SinePattern(spawn.speed, spawn.amplitude, spawn.frequency);
            case MovementPatternType.EaseInVertical:
                return new EaseInVerticalPattern(spawn.speed, spawn.verticalSpeed, spawn.transitionDuration);
            default:
                return new StraightPattern(spawn.speed);
        }
    }

    // Optional: Method to force start next wave manually (for testing)
    public void SkipWave() { /* ... */ }
}