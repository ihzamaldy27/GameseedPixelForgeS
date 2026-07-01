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
    private List<GameObject> _activeEnemies = new List<GameObject>();
    private List<GameObject> _activeBosses = new List<GameObject>(); // track bosses separately

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
            _activeBosses.Clear();
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
                _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
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

        if (spawn.isBoss)
        {
            // Spawn boss directly (not pooled)
            GameObject bossObj = Instantiate(spawn.enemyPrefab, spawnPoint.position + (Vector3)spawn.spawnPosition, Quaternion.identity);
            // Optionally set the boss's movement stop position based on some offset
            BossController boss = bossObj.GetComponent<BossController>();
            if (boss != null)
            {
                // The boss movement component might need to know its stop position; we can set it here
                BossMovement move = boss.GetComponent<BossMovement>();
                if (move != null)
                {
                    // Optionally set stopPosition from inspector in the prefab, or configure per wave
                    // We can leave it as set in prefab.
                }
            }
            // Add to active enemies list (so wave waits for its death)
            _activeEnemies.Add(bossObj);
            _activeBosses.Add(bossObj); // track as boss

            // Change BGM to Boss Battle
            AudioManager.instance.PlayBGMAfterDecay("Boss Battle", 1f);
        }
        else
        {
            // Normal enemy from pool
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
            _activeEnemies.Add(enemy.gameObject);
        }
    }

    public void RestartFromWave(int waveIndex)
    {
        if (_isSpawning)
        {
            StopAllCoroutines();
            _isSpawning = false;
        }
        _activeEnemies.Clear(); // clear any remaining references
        _activeBosses.Clear();
        _currentWaveIndex = waveIndex;
        StartCoroutine(StartNextWave());
    }

    // Public method for CheckpointManager to clean up bosses
    public void CleanUpBosses()
    {
        /*foreach (var bossObj in _activeBosses)
        {
            if (bossObj != null)
                Destroy(bossObj);
        }*/

        // Find any bosses in the scene (including those in death sequence)
        BossController[] bosses = FindObjectsByType<BossController>(FindObjectsSortMode.None);
        foreach (var boss in bosses)
        {
            if (boss != null && boss.gameObject != null)
                Destroy(boss.gameObject);
        }

        // Also check for BossDeathHandler components (in case boss is destroyed but death sequence is still running)
        BossDeathHandler[] deathHandlers = FindObjectsByType<BossDeathHandler>(FindObjectsSortMode.None);
        foreach (var handler in deathHandlers)
        {
            if (handler != null && handler.gameObject != null)
                Destroy(handler.gameObject);
        }

        _activeBosses.Clear();
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
            case MovementPatternType.EaseOutVertical:
                return new EaseOutVerticalPattern(spawn.speed, spawn.initialVerticalSpeed, spawn.transitionDuration);
            default:
                return new StraightPattern(spawn.speed);
        }
    }

    // Optional: Method to force start next wave manually (for testing)
    public void SkipWave() { /* ... */ }
}