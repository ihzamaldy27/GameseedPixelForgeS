using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
// 1. TAMBAHKAN IDamageable DI SINI
public class PlayerController : MonoBehaviour, IDamageable 
{
    [Header("Health & Status")]
    public int maxHealth = 100;
    public float invincibilityDuration = 1.5f; // I-frame agar player tidak langsung mati saat dikeroyok
    private HealthComponent health;
    private Color originalColor;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 15f;
    private Vector2 moveInput;
    private bool isFacingRight = true;

    [Header("Dash / Dodge")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private float dashTimeLeft;
    private float lastDashTime = -100f;

    [Header("Komponen")]
    public Animator playerAnim;       
    public Animator vfxAnim;          
    public Transform attackPoint;     

    [Header("Pengaturan Combat")]
    public float attackRange = 0.8f;
    public LayerMask enemyLayer;
    public float damageAmount = 1f;

    [Header("Sistem Combo")]
    public float maxComboDelay = 0.6f; 
    
    private int comboStep = 0;
    private int currentAnimatingStep = 0; 
    private float lastClickedTime;
    private bool isAttacking = false;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    private bool isGrounded;

    [Header("Contact Damage & Knockback")]
    public int contactDamage = 1; // Jumlah damage saat menabrak musuh
    public float knockbackForceX = 10f; // Daya dorong ke belakang
    public float knockbackForceY = 6f;  // Daya dorong ke atas (bouncing)
    public float knockbackDuration = 0.25f; // Lama player kehilangan kendali (stunned)
    
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    [Header("Dash Effects")]
    [SerializeField] private GameObject afterimagePrefab; 
    [SerializeField] private float afterimageSpawnRate = 0.05f; 

    [Header("Platform Mekanik")]
    private GameObject currentOneWayPlatform;
    private Collider2D playerCollider;
    private float nextSpawnTime;

    private List<AfterimageFade> afterimagePool = new List<AfterimageFade>();

    private Rigidbody2D rb;
    private SpriteRenderer playerSR;
    private bool isDead = false;
    private Coroutine flashRoutine = null; // Menyimpan referensi korutin yang sedang berjalan

    [Header("Flash Settings")]
    private float flashTimer = 0f;
    private bool isFlashing = false;

    [Header("Hitbox Contact Damage")]
    public float contactRadius = 0.5f; // Besaran area sensor tubuh player

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSR = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        // 2. INISIALISASI SISTEM HEALTH
        if (playerSR != null) originalColor = playerSR.color;

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        
        health = new HealthComponent(maxHealth, invincibilityDuration);
        health.OnDamaged += HandleDamage;
        health.OnDeath += HandleDeath;
    }

    void Update()
    {

        // if (health.CurrentHP <= 0 && !isDead)
        // {
        //     isDead = true; // Kunci agar hanya jalan sekali
        //     TriggerDeath();
        // }

        // Jika player sudah mati, hentikan semua kontrol dan update
        if (health != null && health.IsDead) return;

        // if (isKnockedBack)
        // {
        //     knockbackTimer -= Time.deltaTime;
        //     if (knockbackTimer <= 0)
        //     {
        //         isKnockedBack = false;
        //     }
        //     else
        //     {
        //         // return; digunakan untuk menghentikan Update sementara.
        //         // Ini mencegah player menekan tombol jalan saat sedang terpental!
        //         return; 
        //     }
        // }

        // 3. UPDATE TIMER KEBAL PLAYER
        health.UpdateInvincibility(Time.deltaTime);

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
                //Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
            }
            if (Time.time >= nextSpawnTime)
            {
                SpawnAfterimage();
                nextSpawnTime = Time.time + afterimageSpawnRate;
            }
            return; 
        }

        if (Time.time - lastClickedTime > maxComboDelay && !isAttacking)
        {
            comboStep = 0;
            currentAnimatingStep = 0;
        }

        // --- UPDATE PENGIRIMAN DATA KE ANIMATOR ---
        if (playerAnim != null)
        {
            // Kirim kecepatan horizontal untuk animasi lari
            playerAnim.SetFloat("Speed", Mathf.Abs(moveInput.x));
            
            // Kirim status pijakan tanah
            playerAnim.SetBool("IsGrounded", isGrounded);
            
            // Kirim kecepatan vertikal (positif saat naik, negatif saat turun)
            playerAnim.SetFloat("yVelocity", rb.linearVelocity.y);
        }

        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            
            // Efek kedip cepat: nyala merah - putih - merah - putih
            if (flashTimer > 0)
            {
                // Menggunakan sisa waktu untuk menentukan warna
                playerSR.color = (Mathf.Round(flashTimer * 20) % 2 == 0) ? Color.red : Color.white;
            }
            else
            {
                playerSR.color = Color.white; // Paksa putih saat selesai
                isFlashing = false;
            }
        }

        CheckEnemyContactSensor();
        CheckGrounded();
        ResetCombo();
    }

    void FixedUpdate()
    {
        if (health != null && health.IsDead) return; // Cegah gerak fisika saat mati

        if (isDashing)
        {
            rb.linearVelocity = new Vector2((isFacingRight ? 1 : -1) * dashSpeed, 0f);
            return;
        }

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && !isFacingRight) Flip();
        else if (moveInput.x < 0 && isFacingRight) Flip();
    }

    // --- IMPLEMENTASI IDamageable UNTUK PLAYER ---
    public void TakeDamage(int damage)
    {
        // Fungsi ini akan dipanggil otomatis oleh EnemyMelee saat memukul
        // Jika player sedang dash (Dodge), kamu bisa membatalkan damage dengan cara uncomment baris di bawah:
        if (isDashing) return; 

        StartFlash(); // Efek visual kedip merah
        health.TakeDamage(damage);
    }

    private void HandleDamage(int currentHP)
    {
        if (isDead) return; 

        // Panggil efek visual terpisah
        StartFlash();

        // TriggerDeath TIDAK dipanggil di sini lagi, 
        // akan ditangani otomatis oleh event OnDeath -> HandleDeath
    }

    private void HandleDeath()
    {
        if (isDead) return; // Cegah terpanggil berkali-kali
        isDead = true;

        Debug.Log("<color=black>PLAYER MATI! (GAME OVER)</color>");
        
        // SANGAT PENTING: Kita HAPUS baris '-=' di sini.
        // Player harus tetap terhubung ke sistem Health agar flash tetap menyala setelah respawn.
        
        TriggerDeath(); 
    }

    // Tambahkan ini di tempat kamu mengecek kematian (misal di TakeDamage atau update HP)
    public void TriggerDeath()
    {
        AudioManager.instance.PlaySFX("MC Death");

        if (playerAnim != null) playerAnim.SetTrigger("Die");

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; 
        GetComponent<Collider2D>().enabled = false;

        // Tenggelam ke Acid
        transform.position = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);
        
        if (playerSR != null) playerSR.sortingOrder = -2; 
        
        Invoke("Respawn", 2.0f);
    }

    // private IEnumerator FlashHit()
    // {
    //     // Hentikan korutin lama jika ada
    //     if (flashRoutine != null) StopCoroutine(flashRoutine);
        
    //     // Simpan referensi korutin baru agar bisa dihentikan nanti
    //     flashRoutine = StartCoroutine(FlashRoutine());
    //     yield return null;
    // }
    // // ----------------------------------------------

    // private IEnumerator FlashRoutine()
    // {
    //     if (playerSR == null) playerSR = GetComponent<SpriteRenderer>();

    //     // Efek kedip merah
    //     for (int i = 0; i < 3; i++)
    //     {
    //         playerSR.color = new Color(1f, 0.5f, 0.5f, 1f); 
    //         yield return new WaitForSeconds(0.1f);
    //         playerSR.color = Color.white;
    //         yield return new WaitForSeconds(0.1f);
    //     }
        
    //     // Reset referensi setelah selesai
    //     flashRoutine = null;
    // }

    public void StartFlash()
    {
        isFlashing = true;
        flashTimer = 0.3f; // Durasi total kedip merah
    }

    // --- INPUT SYSTEM CALLBACKS ---
    public void OnMove(InputValue value) 
    {
        moveInput = value.Get<Vector2>();
        // if (Mathf.Abs(moveInput.x) > 0.1f && !isDashing)
        // {
        //     playerAnim.SetFloat("Speed", Mathf.Abs(moveInput.x));
        // } 
    }

    public void OnJump(InputValue value)
    {
         

        if (moveInput.y < -0.5f && currentOneWayPlatform != null)
        {
            // Jalankan fungsi turun menembus lantai
            StartCoroutine(FallThrough());
        }
        else if (value.isPressed && isGrounded && !isDashing)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            isDashing = true;
            dashTimeLeft = dashDuration;
            lastDashTime = Time.time;
            rb.linearVelocity = Vector2.zero; 
            SpawnAfterimage(); 
            nextSpawnTime = Time.time + afterimageSpawnRate;
            //Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            lastClickedTime = Time.time;
            if (!isAttacking)
            {
                comboStep = 1;
                TriggerAttackAnimation();
            }
            else
            {
                comboStep++;
                comboStep = Mathf.Clamp(comboStep, 1, 3);
            }
        }
    }

    // --- FUNGSI COMBAT & LAINNYA ---
    private void TriggerAttackAnimation()
    {
        isAttacking = true;
        currentAnimatingStep++; 
        playerAnim.SetInteger("ComboStep", currentAnimatingStep);
        playerAnim.SetTrigger("Attack");
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void ResetCombo()
    {
        if (comboStep > 0 && Time.time - lastClickedTime > maxComboDelay && !isAttacking)
        {
            comboStep = 0;
            currentAnimatingStep = 0;
            playerAnim.SetInteger("ComboStep", 0);
        }
    }

    private IEnumerator FallThrough()
    {
        if (currentOneWayPlatform != null)
        {
            Collider2D platformCollider = currentOneWayPlatform.GetComponent<Collider2D>();
            
            // 1. Matikan tabrakan fisik antara Player dan Platform ini
            Physics2D.IgnoreCollision(playerCollider, platformCollider, true);
            
            // 2. Beri waktu 0.5 detik agar tubuh Player selesai jatuh melewati ketebalan lantai
            yield return new WaitForSeconds(0.5f);
            
            // 3. Nyalakan tabrakannya kembali agar lantai menjadi padat lagi
            Physics2D.IgnoreCollision(playerCollider, platformCollider, false);
        }
    }

    private void SpawnAfterimage()
    {
        AfterimageFade poolable = null;
        for (int i = 0; i < afterimagePool.Count; i++)
        {
            if (!afterimagePool[i].gameObject.activeInHierarchy) { poolable = afterimagePool[i]; break; }
        }
        if (poolable == null)
        {
            GameObject newObj = Instantiate(afterimagePrefab);
            poolable = newObj.GetComponent<AfterimageFade>();
            afterimagePool.Add(poolable);
        }
        poolable.SetAfterimage(playerSR.sprite, transform.position, transform.rotation, transform.localScale);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }

    public void ExecuteDamageHitbox()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        AudioManager.instance.PlaySFX("Damage");
        foreach (Collider2D enemy in hitEnemies)
        {
            IKnockbackable knockbackable = enemy.GetComponent<IKnockbackable>();
            if (knockbackable != null) knockbackable.ApplyKnockback(transform.position);

            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = Mathf.RoundToInt(damageAmount);
                damageable.TakeDamage(finalDamage);
                Debug.Log($"Kena tebas Combo ke-{currentAnimatingStep}! (Kirim {finalDamage} Damage)");
            }
        }
    }

    public void EndAttackStep()
    {
        if (comboStep > currentAnimatingStep && (Time.time - lastClickedTime <= maxComboDelay))
        {
            TriggerAttackAnimation();
        }
        else
        {
            isAttacking = false;
            comboStep = 0;
            currentAnimatingStep = 0;
            playerAnim.SetInteger("ComboStep", 0);
        }
    }

    public void Respawn()
    {
        transform.position = SavePoint.lastCheckpointPosition;
        rb.bodyType = RigidbodyType2D.Dynamic;
        GetComponent<Collider2D>().enabled = true;
        
        // Reset Visual & Flash
        isFlashing = false;
        flashTimer = 0f;
        if (playerSR != null) 
        {
            playerSR.color = Color.white;
            playerSR.sortingOrder = 0; // Kembalikan ke 0 agar tidak terus-terusan tenggelam
        }
        
        // --- FIX BUG ANIMASI SERANG MACET ---
        isAttacking = false;
        comboStep = 0;
        currentAnimatingStep = 0;
        if (playerAnim != null) playerAnim.SetInteger("ComboStep", 0);
        // ------------------------------------
        
        health.ResetHealth();
        isDead = false;
        
        if (playerAnim != null)
        {
            playerAnim.ResetTrigger("Die");
            playerAnim.Play("IdleMC");
        }
        
        this.enabled = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Acid"))
        {
            TakeDamage(100); // Langsung mati jika kena asam
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Spike"))
        {
            TakeDamage(1); // Misal 1 damage untuk Spike
            Debug.LogWarning("Player terkena Spike! Nyawa berkurang.");
        }
    }

    // --- DETEKSI SENTUHAN FISIK DENGAN MUSUH ---
    
    // Terpanggil saat pertama kali nabrak
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            Debug.Log("Player menempel pada OneWayPlatform: " + collision.gameObject.name);
            currentOneWayPlatform = collision.gameObject;
        }

        //CheckEnemyContact(collision.gameObject);
    }

    // Terpanggil jika player terus menempel pada musuh (misal tersudut di tembok)
    private void OnCollisionStay2D(Collision2D collision)
    {
        //CheckEnemyContact(collision.gameObject);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentOneWayPlatform = null;
        }
    }

    private void CheckEnemyContactSensor()
    {
        // Jangan terima damage jika sedang dash atau sudah mati
        if (isDashing || (health != null && health.IsDead)) return;

        // Bikin lingkaran tak terlihat di badan Player. Jika ada Enemy yang masuk, kena damage!
        Collider2D enemyHit = Physics2D.OverlapCircle(transform.position, contactRadius, enemyLayer);
        
        if (enemyHit != null)
        {
            TakeDamage(contactDamage); 
            // Fungsi TakeDamage akan memicu I-Frames dan animasi Flash otomatis dari HealthComponent
        }
    }
}