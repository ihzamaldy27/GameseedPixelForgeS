using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class DummyEnemy : MonoBehaviour, IDamageable, IKnockbackable // <-- Tambahkan IKnockbackable di sini
{
    [Header("Status")]
    public int maxHealth = 50;

    [Header("Visual Feedback")]
    [SerializeField] private Color flashColor = Color.red; 
    [SerializeField] private float flashDuration = 0.1f;   
    
    [Header("Physics & Knockback")]
    public float knockbackForce = 10f; 

    private Color originalColor; 
    private Vector2 lastHitPosition; 
    
    private HealthComponent health;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    [SerializeField] private Animator damageTaken; // Prefab efek hit yang akan dipasang di Inspector

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (sr != null)
        {
            originalColor = sr.color; 
        }

        health = new HealthComponent(maxHealth, 0.2f); 
        health.OnDamaged += HandleDamage;
        health.OnDeath += HandleDeath;
    }

    void Update()
    {
        health.UpdateInvincibility(Time.deltaTime);
    }

    // --- IMPLEMENTASI IKnockbackable ---
    public void ApplyKnockback(Vector2 sourcePosition)
    {
        // Simpan posisi penyerang untuk digunakan saat damage berhasil masuk
        lastHitPosition = sourcePosition;
    }

    // --- IMPLEMENTASI IDamageable ---
    public void TakeDamage(int damage)
    {
        // Parameter kembali menjadi 1 saja (int damage)
        damageTaken.SetTrigger("DamageTaken"); // Mainkan animasi efek hit
        health.TakeDamage(damage);
    }

    private void HandleDamage(int currentHP)
    {
        Debug.Log($"<color=orange>Dummy Terkena Hit!</color> Sisa HP: {currentHP} / {health.MaxHP}");
        
        // --- LOGIKA KNOCKBACK ---
        Vector2 knockbackDirection = ((Vector2)transform.position - lastHitPosition).normalized;
        knockbackDirection.y = 0; 
        
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        // ------------------------

        StartCoroutine(FlashHit());
    }

    private void HandleDeath()
    {
        Debug.Log("<color=red>Dummy HANCUR!</color>");
        
        health.OnDamaged -= HandleDamage;
        health.OnDeath -= HandleDeath;
        
        Destroy(gameObject);
    }

    private IEnumerator FlashHit()
    {
        if (sr != null)
        {
            sr.color = flashColor; 
            yield return new WaitForSeconds(flashDuration); 
            sr.color = originalColor;
        }
    }
}