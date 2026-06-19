using UnityEngine;

public class PlaneHitbox : MonoBehaviour
{
    private IDamageable _damageable;

    private void Awake()
    {
        // Find the first component in the parent hierarchy that implements IDamageable
        _damageable = GetComponentInParent<IDamageable>();
        if (_damageable == null)
        {
            Debug.LogError("PlayerHitbox requires a parent with an IDamageable component!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only process if we have a valid damageable target
        if (_damageable == null) return;

        // Check if the collider is an enemy or enemy bullet
        if (other.CompareTag("EnemyBullet") || other.CompareTag("Enemy"))
        {
            // Determine damage amount (default 1)
            int damage = 1;
            DamageData damageData = other.GetComponent<DamageData>();
            if (damageData != null)
            {
                damage = damageData.damage;
            }

            // Delegate the damage application to the composed HealthHandler
            _damageable.TakeDamage(damage);

            // Destroy the enemy projectile
            if (other.CompareTag("EnemyBullet"))
            {
                Destroy(other.gameObject);
            }
        }
    }
}
