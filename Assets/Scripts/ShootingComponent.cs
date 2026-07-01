using UnityEngine;

public class ShootingComponent
{
    private readonly Transform _firePoint;
    private readonly GameObject _bulletPrefab;
    private readonly float _fireRate;
    private float _nextFireTime;
    private AudioClip _shootSfx;

    public ShootingComponent(Transform firePoint, GameObject bulletPrefab, float fireRate)
    {
        _firePoint = firePoint;
        _bulletPrefab = bulletPrefab;
        _fireRate = fireRate;
        _nextFireTime = 0f;
    }

    public ShootingComponent(Transform firePoint, GameObject bulletPrefab, float fireRate, AudioClip shootSfx)
    {
        _firePoint = firePoint;
        _bulletPrefab = bulletPrefab;
        _fireRate = fireRate;
        _nextFireTime = 0f;
        _shootSfx = shootSfx;
    }

    public void HandleShoot(bool firePressed)
    {
        if (firePressed && Time.time >= _nextFireTime)
        {
            Shoot();
            _nextFireTime = Time.time + _fireRate;
            if (_shootSfx != null)
                AudioManager.instance.PlaySFXClip(_shootSfx);
        }
    }

    private void Shoot()
    {
        BulletController bullet = BulletPoolManager.Instance.GetBullet(_bulletPrefab.GetComponent<BulletController>());
        if (bullet != null)
        {
            //GameObject bullet = Object.Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
            bullet.transform.position = _firePoint.position;
            bullet.transform.rotation = _firePoint.rotation;
            bullet.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log("Bullet is empty");
        }
    }
}
