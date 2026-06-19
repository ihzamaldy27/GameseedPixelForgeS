using UnityEngine;

public class ShootingComponent : MonoBehaviour
{
    private readonly Transform _firePoint;
    private readonly GameObject _bulletPrefab;
    private readonly float _fireRate;
    private float _nextFireTime;

    public ShootingComponent(Transform firePoint, GameObject bulletPrefab, float fireRate)
    {
        _firePoint = firePoint;
        _bulletPrefab = bulletPrefab;
        _fireRate = fireRate;
        _nextFireTime = 0f;
    }

    public void HandleShoot(bool firePressed)
    {
        if (firePressed && Time.time >= _nextFireTime)
        {
            Shoot();
            _nextFireTime = Time.time + _fireRate;
        }
    }

    private void Shoot()
    {
        if (_bulletPrefab != null && _firePoint != null)
        {
            GameObject bullet = Object.Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
            // Optionally set bullet velocity or direction via a Bullet component
        }
    }
}
