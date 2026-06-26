using UnityEngine;

public class ExplosionPoolManager : MonoBehaviour
{
    public static ExplosionPoolManager Instance { get; private set; }

    [SerializeField] private ExplosionController explosionPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private ObjectPool<ExplosionController> _pool;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _pool = new ObjectPool<ExplosionController>(explosionPrefab, transform, initialPoolSize);
    }

    public ExplosionController SpawnExplosion(Vector3 position, float scale)
    {
        ExplosionController explosion = _pool.Get();
        explosion.transform.position = position;
        explosion.Play(scale);
        return explosion;
    }

    public void ReturnExplosion(ExplosionController explosion)
    {
        _pool.Return(explosion);
    }
}
