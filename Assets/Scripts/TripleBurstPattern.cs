using UnityEngine;
using System.Collections;

public class TripleBurstPattern : MonoBehaviour, IBossAttackPattern
{
    [Header("Bullet Settings")]
    [SerializeField] private BulletController bulletPrefab; // boss bullet prefab
    [SerializeField] private float bulletSpeed = 5f;

    [Header("Pattern Settings")]
    [SerializeField] private int bursts = 3;           // number of rapid shots
    [SerializeField] private float burstInterval = 0.15f; // time between bursts
    [SerializeField] private float angleSpread = 20f;   // upward/downward angle in degrees

    private Transform _firePoint;

    private void Awake()
    {
        // Find a child named "FirePoint" or use this transform's position
        _firePoint = transform.Find("FirePoint");
        if (_firePoint == null)
            _firePoint = transform; // fallback
    }

    public IEnumerator ExecuteAttack()
    {
        for (int i = 0; i < bursts; i++)
        {
            ShootTriple();
            yield return new WaitForSeconds(burstInterval);
        }
    }

    private void ShootTriple()
    {
        if (bulletPrefab == null || BulletPoolManager.Instance == null) return;

        // Angles: straight (0°), up (+angleSpread), down (-angleSpread)
        float[] angles = { 0f, angleSpread, -angleSpread };

        foreach (float angle in angles)
        {
            BulletController bullet = BulletPoolManager.Instance.GetBullet(bulletPrefab);
            if (bullet == null) continue;

            bullet.transform.position = _firePoint.position;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            bullet.gameObject.SetActive(true);

            // Optionally set the bullet's speed (if not set on prefab)
            // We can set speed via a public field in BulletController or modify its movement.
            // Since speed is set in BulletController, we can set it here:
            // bullet.SetSpeed(bulletSpeed); // if we add a SetSpeed method
            // Simpler: set a speed field in BulletController and assign via Inspector.
        }
    }
}