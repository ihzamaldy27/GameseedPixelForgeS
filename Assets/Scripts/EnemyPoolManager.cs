using UnityEngine;
using System.Collections.Generic;

public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 5;
    [SerializeField] private Transform poolParent; // optional, will use this object as parent

    private Dictionary<EnemyController, ObjectPool<EnemyController>> _pools = 
        new Dictionary<EnemyController, ObjectPool<EnemyController>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (poolParent == null)
            poolParent = transform;
    }

    public EnemyController GetEnemy(EnemyController prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("Enemy prefab is null!");
            return null;
        }

        // Get or create the pool for this prefab
        if (!_pools.TryGetValue(prefab, out ObjectPool<EnemyController> pool))
        {
            pool = new ObjectPool<EnemyController>(prefab, poolParent, initialPoolSize);
            _pools[prefab] = pool;
        }

        EnemyController enemy = pool.Get();
        // Store the original prefab so we can return to the correct pool
        enemy.OriginalPrefab = prefab;
        return enemy;
    }

    public void ReturnEnemy(EnemyController enemy)
    {
        if (enemy == null) return;

        // Find the pool using the stored original prefab
        if (enemy.OriginalPrefab != null && _pools.TryGetValue(enemy.OriginalPrefab, out ObjectPool<EnemyController> pool))
        {
            pool.Return(enemy);
        }
        else
        {
            // Fallback: if no pool found, destroy the object (should not happen)
            Debug.LogWarning($"No pool found for enemy {enemy.name}, destroying.");
            Destroy(enemy.gameObject);
        }
    }
}