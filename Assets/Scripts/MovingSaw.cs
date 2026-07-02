using UnityEngine;

public class MovingSaw : MonoBehaviour
{
    [Header("Titik Pergerakan (Buat Empty GameObject)")]
    public Transform pointA;
    public Transform pointB;

    [Header("Pengaturan Platform")]
    public float speed = 3f;

    private Transform currentTarget;

    void Start()
    {
        // Saat game dimulai, platform akan bergerak ke Titik B
        currentTarget = pointB;
    }

    void Update()
    {
        // 1. Gerakkan platform menuju target
        transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, speed * Time.deltaTime);

        // 2. Cek apakah platform sudah sampai di titik target
        // Kita pakai jarak 0.05f sebagai toleransi agar tidak meleset
        if (Vector2.Distance(transform.position, currentTarget.position) < 0.05f)
        {
            // Tukar arah tujuan
            if (currentTarget == pointA)
            {
                currentTarget = pointB;
            }
            else
            {
                currentTarget = pointA;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Cek jika yang menginjak adalah Player
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.LogWarning("Player terkena gergaji! Nyawa berkurang.");
            // Panggil fungsi untuk mengurangi nyawa player
            PlayerController playerHealth = collision.GetComponent<PlayerController>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1); // Misal mengurangi 1 nyawa
            }
        }
    }   
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // // Cek jika yang menginjak adalah Player
        // if (collision.gameObject.CompareTag("Player"))
        // {
        //     // Jadikan Player sebagai 'Child' dari platform ini
        //     collision.transform.SetParent(transform);
        // }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // // Saat Player melompat atau turun dari platform
        // if (collision.gameObject.CompareTag("Player"))
        // {
        //     // Lepaskan Player agar mandiri lagi
        //     collision.transform.SetParent(null);
        // }
    }

    // --- VISUAL PANDUAN DI EDITOR ---
    private void OnDrawGizmos()
    {
        // Menggambar garis hijau antara Titik A dan B di jendela Scene Unity
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.2f);
            Gizmos.DrawWireSphere(pointB.position, 0.2f);
        }
    }
}
