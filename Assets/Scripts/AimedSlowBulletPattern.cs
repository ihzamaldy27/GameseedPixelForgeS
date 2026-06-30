using UnityEngine;
using System.Collections;

public class AimedSlowBulletPattern : MonoBehaviour, IBossAttackPattern
{
    [Header("Bullet Settings")]
    [SerializeField] private BulletController bulletPrefab; // large slow bullet
    [SerializeField] private float bulletSpeed = 2f;

    [Header("Fire Point")]
    [SerializeField] private Transform firePoint; // optional, falls back to transform

    private Transform _playerTransform;

    private void Awake()
    {
        if (firePoint == null)
            firePoint = transform;

        // Find the player by tag (assumes player is tagged "Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;
        else
            Debug.LogError("AimedSlowBulletPattern: Player not found in scene.");
    }

    public IEnumerator ExecuteAttack()
    {
        if (_playerTransform == null)
        {
            Debug.LogWarning("Player not found – skipping aimed shot.");
            yield break;
        }

        // Calculate direction to player
        Vector3 direction = (_playerTransform.position - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Spawn bullet from pool
        BulletController bullet = BulletPoolManager.Instance.GetBullet(bulletPrefab);
        if (bullet != null)
        {
            bullet.transform.position = firePoint.position;
            bullet.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            bullet.SetSpeed(bulletSpeed); // override speed
            bullet.gameObject.SetActive(true);
        }

        // Yield to allow the shot to happen (no delay for this pattern)
        yield return null; 
    }
}