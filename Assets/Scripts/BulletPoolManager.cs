using UnityEngine;
using System.Collections.Generic;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    [SerializeField] private int initialPoolSize = 10;

    private Dictionary<BulletController, ObjectPool<BulletController>> _pools =
        new Dictionary<BulletController, ObjectPool<BulletController>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public BulletController GetBullet(BulletController prefab)
    {
        if (prefab == null) return null;
        
        if (!_pools.TryGetValue(prefab, out ObjectPool<BulletController> pool))
        {
            pool = new ObjectPool<BulletController>(prefab, transform, initialPoolSize);
            _pools[prefab] = pool;
        }

        BulletController bullet = pool.Get();
        bullet.OriginalPrefab = prefab;
        return bullet;
    }

    public void ReturnBullet(BulletController bullet)
    {
        if (bullet == null) return;

        if (bullet.OriginalPrefab != null &&
            _pools.TryGetValue(bullet.OriginalPrefab, out ObjectPool<BulletController> pool))
        {
            pool.Return(bullet);
        }
        else
        {
            // Fallback (should not happen)
            Destroy(bullet.gameObject);
        }
    }
}