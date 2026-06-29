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
    public float damageAmount = 10f;

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
    public int contactDamage = 10; // Jumlah damage saat menabrak musuh
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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSR = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        // 2. INISIALISASI SISTEM HEALTH
        if (playerSR != null) originalColor = playerSR.color;
        
        health = new HealthComponent(maxHealth, invincibilityDuration);
        health.OnDamaged += HandleDamage;
        health.OnDeath += HandleDeath;
    }

    void Update()
    {
        // Jika player sudah mati, hentikan semua kontrol dan update
        if (health != null && health.IsDead) return;

        if (isKnockedBack)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                isKnockedBack = false;
            }
            else
            {
                // return; digunakan untuk menghentikan Update sementara.
                // Ini mencegah player menekan tombol jalan saat sedang terpental!
                return; 
            }
        }

        // 3. UPDATE TIMER KEBAL PLAYER
        health.UpdateInvincibility(Time.deltaTime);

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
                Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
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

        health.TakeDamage(damage);
    }

    private void HandleDamage(int currentHP)
    {
        Debug.Log($"<color=red>PLAYER TERKENA HIT!</color> Sisa HP: {currentHP} / {health.MaxHP}");
        
        // Memutar animasi kedip merah
        StartCoroutine(FlashHit());
    }

    private void HandleDeath()
    {
        Debug.Log("<color=black>PLAYER MATI! (GAME OVER)</color>");
        
        health.OnDamaged -= HandleDamage;
        health.OnDeath -= HandleDeath;
        
        // Matikan velocity agar player jatuh ke tanah dan tidak meluncur
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        
        // Matikan script ini agar player tidak bisa dikontrol lagi
        this.enabled = false;

        // TODO: Panggil animasi mati (misal: playerAnim.SetTrigger("Die"); )
        // TODO: Munculkan panel UI Game Over
    }

    private IEnumerator FlashHit()
    {
        if (playerSR != null)
        {
            // Berkedip merah beberapa kali untuk menandakan durasi kebal (I-frames)
            for (int i = 0; i < 3; i++)
            {
                playerSR.color = new Color(1f, 0.5f, 0.5f, 0.5f); // Merah transparan
                yield return new WaitForSeconds(0.1f);
                playerSR.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    // ----------------------------------------------


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
            Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
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

    // --- DETEKSI SENTUHAN FISIK DENGAN MUSUH ---
    
    // Terpanggil saat pertama kali nabrak
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            Debug.Log("Player menempel pada OneWayPlatform: " + collision.gameObject.name);
            currentOneWayPlatform = collision.gameObject;
        }

        CheckEnemyContact(collision.gameObject);
    }

    // Terpanggil jika player terus menempel pada musuh (misal tersudut di tembok)
    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckEnemyContact(collision.gameObject);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OneWayPlatform"))
        {
            currentOneWayPlatform = null;
        }
    }

    private void CheckEnemyContact(GameObject enemyObj)
    {
        if (enemyObj.CompareTag("Enemy") && !isKnockedBack && !isDashing) 
        {
            TakeDamage(contactDamage); 
            isKnockedBack = true;
            knockbackTimer = knockbackDuration;

            int knockbackDir = transform.position.x < enemyObj.transform.position.x ? -1 : 1;
            rb.linearVelocity = Vector2.zero; 
            rb.AddForce(new Vector2(knockbackForceX * knockbackDir, knockbackForceY), ForceMode2D.Impulse);
        }

        // Pastikan yang ditabrak adalah musuh, dan player tidak sedang dalam kondisi terpental
        if (enemyObj.CompareTag("Enemy") && !isKnockedBack)
        {
            // 1. Berikan Damage
            // (Memanggil fungsi TakeDamage yang sudah ada karena player memiliki antarmuka IDamageable)
            TakeDamage(contactDamage); 

            // 2. Aktifkan status Knockback
            isKnockedBack = true;
            knockbackTimer = knockbackDuration;

            // 3. Tentukan Arah Pentalan
            // Jika posisi X player lebih kecil dari musuh (di kiri), pentalan ke kiri (-1). Jika tidak, ke kanan (1).
            int knockbackDir = transform.position.x < enemyObj.transform.position.x ? -1 : 1;

            // 4. Dorong Player
            rb.linearVelocity = Vector2.zero; // Rem mendadak agar pentalannya konsisten
            rb.AddForce(new Vector2(knockbackForceX * knockbackDir, knockbackForceY), ForceMode2D.Impulse);

            // Opsional: Jika kamu punya animasi terluka, kamu bisa memicunya di sini
            if (playerAnim != null)
            {
                // playerAnim.SetTrigger("Hit"); 
            }
        }
    }
}