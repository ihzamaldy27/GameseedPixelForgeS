using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Pengaturan Peluru")]
    public float speed = 12f;
    public int damage = 10;
    public float lifeTime = 3f; // Waktu maksimal peluru hidup sebelum hancur sendiri
    
    [Header("Deteksi Hit")]
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    private void Start()
    {
        // Otomatis hancurkan peluru setelah beberapa detik agar memori tidak penuh
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        // Peluru bergerak lurus ke depan (Vector3.right akan mengikuti rotasi firePoint nantinya)
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Jika menabrak Player
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                // Opsional: Jika peluru punya efek dorongan
                IKnockbackable knockback = other.GetComponent<IKnockbackable>();
                if (knockback != null) knockback.ApplyKnockback(transform.position);
            }
            Destroy(gameObject); // Hancurkan peluru setelah kena Player
        }
        // 2. Jika menabrak Tanah/Tembok
        else if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            // Opsional: Panggil VFX peluru hancur/meledak di sini sebelum di-Destroy
            Destroy(gameObject); 
        }
    }
}