using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [SerializeField] private EnemyController enemyPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private ObjectPool<EnemyController> _pool;

    private void Awake()
    {
        _pool = new ObjectPool<EnemyController>(enemyPrefab, transform, initialPoolSize);
    }

    public EnemyController GetEnemy()
    {
        return _pool.Get();
    }

    public void ReturnEnemy(EnemyController enemy)
    {
        _pool.Return(enemy);
    }
}