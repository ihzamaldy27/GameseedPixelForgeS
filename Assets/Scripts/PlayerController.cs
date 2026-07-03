using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
// 1. TAMBAHKAN IDamageable DI SINI
public class PlayerController : MonoBehaviour, IDamageable 
{
    [Header("Health & Status")]
    public int maxHealth = 5;
    public float invincibilityDuration = 1.5f; // I-frame agar player tidak langsung mati saat dikeroyok
    private HealthComponent health;
    private Color originalColor;

    public GameObject healthBarUI; // Referensi ke UI Health Bar

    
    [SerializeField] private GameObject[] healthSegments; // Array untuk menyimpan segmen-segmen health bar 

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
    public bool canAttack = true; // Flag untuk mengontrol apakah player bisa menyerang atau tidak

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
    
    //private bool isKnockedBack = false;
    //private float knockbackTimer = 0f;

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
    //private Coroutine flashRoutine = null; // Menyimpan referensi korutin yang sedang berjalan

    [Header("Flash Settings")]
    private float flashTimer = 0f;
    private bool isFlashing = false;

    [Header("Screen Effects (Vignette & Fade)")]
    public CanvasGroup vignetteGroup;
    public CanvasGroup fadeGroup;
    public float fadeDuration = 1.5f;        // Lama waktu layar menjadi gelap
    public float vignettePulseSpeed = 3f;  // Kecepatan detak vignette
    private bool isLowHealth = false;

    [Header("Hitbox Contact Damage")]
    public float contactRadius = 0.5f; // Besaran area sensor tubuh player

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSR = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();

        if (healthBarUI != null)
        {
            int childCount = healthBarUI.transform.childCount;
            healthSegments = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                healthSegments[i] = healthBarUI.transform.GetChild(i).gameObject;
            }
        }

        // 2. INISIALISASI SISTEM HEALTH
        if (playerSR != null) originalColor = playerSR.color;

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);
        
        health = new HealthComponent(maxHealth, invincibilityDuration);
        health.OnDamaged += HandleDamage;
        health.OnDeath += HandleDeath;
    }

    void Update()
    {
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

        if (isLowHealth && vignetteGroup != null && !isDead)
        {
            // Menggunakan Mathf.Sin untuk membuat efek detak jantung (naik turun secara halus)
            // Alpha akan bolak-balik antara 0.3 (samar) sampai 0.8 (jelas)
            vignetteGroup.alpha = 0.55f + Mathf.Sin(Time.time * vignettePulseSpeed) * 0.25f;
        }

        if (isDead)
        {
            // Kunci kecepatan horizontal jadi 0, tapi biarkan kecepatan vertikal (jatuh) berjalan
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return; // Hentikan paksa seluruh fungsi Update di bawah baris ini!
        }

        
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
        //ResetAttackState(); kalo taro disini Bug saat mati kena acid
        // Fungsi ini akan dipanggil otomatis oleh EnemyMelee saat memukul
        // Jika player sedang dash (Dodge), kamu bisa membatalkan damage dengan cara uncomment baris di bawah:
        if (isDashing) return; 

        StartFlash(); // Efek visual kedip merah
        health.TakeDamage(damage);
    }

    private void HandleDamage(int currentHP)
    {
        if (isDead) return; 
        StartFlash();
        UpdateHealthUI();
        ResetAttackState();
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
        //AudioManager.instance.StopWalkSFX();

        // 1. Pemicu animasi mati
        if (playerAnim != null) playerAnim.SetTrigger("Die");

        // 2. Pastikan tubuh tetap Dynamic agar bisa ditarik gravitasi jatuh ke bawah
        rb.bodyType = RigidbodyType2D.Dynamic; 

        // 3. Matikan kecepatan jalannya, biarkan dia jatuh alami
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // PENTING: Kita TIDAK LAGI mematikan Collider! 
        // Biarkan collider menyala agar saat dia jatuh, badannya akan menabrak tanah/ground.
        
        // 4. Turunkan Sorting Order agar jika jatuh ke Acid, badannya ada di belakang gambar Acid
        if (playerSR != null) playerSR.sortingOrder = -2; 

        isLowHealth = false;
        if (vignetteGroup != null) vignetteGroup.alpha = 0f;
        if (fadeGroup != null) StartCoroutine(FadeOutRoutine());
        
        // 5. Panggil Respawn mutlak setelah 2 detik (tidak peduli sudah nyentuh tanah atau belum)
        Invoke("Respawn", 2.0f);
    }

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
            AudioManager.instance.PlaySFX("MC Jump");
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && Time.time >= lastDashTime + dashCooldown && !isDashing)
        {
            isDashing = true;
            AudioManager.instance.PlaySFX("MC Dash");
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
        if (!canAttack) return; // Jika flag canAttack false, hentikan fungsi ini

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

    private void UpdateHealthUI()
    {
        if (healthSegments == null || health == null) return;

        for (int i = 0; i < healthSegments.Length; i++)
        {
            if (healthSegments[i] != null)
            {
                // Jika index (i) lebih kecil dari HP saat ini, ikon menyala (true)
                // Jika index sama atau lebih besar, ikon mati (false)
                healthSegments[i].SetActive(i < health.CurrentHP);
            }
        }

        if (health != null)
        {
            // Jika darah 2 atau 1, nyalakan status sekarat
            if (health.CurrentHP <= 2 && health.CurrentHP > 0)
            {
                isLowHealth = true;
            }
            else
            {
                isLowHealth = false;
                if (vignetteGroup != null) vignetteGroup.alpha = 0f; // Matikan vignette jika darah aman
            }
        }
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
        
        bool didHitAnyEnemy = false;

        foreach (Collider2D enemy in hitEnemies)
        {
            IKnockbackable knockbackable = enemy.GetComponent<IKnockbackable>();
            if (knockbackable != null) knockbackable.ApplyKnockback(transform.position);

            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int finalDamage = Mathf.RoundToInt(damageAmount);
                damageable.TakeDamage(finalDamage);
                didHitAnyEnemy = true;
            }
        }

        // Tentukan pitch berdasarkan langkah kombo saat ini
        float currentPitch = 1f;
        if (comboStep == 1) currentPitch = 1.0f;       // Nada normal
        else if (comboStep == 2) currentPitch = 1.15f; // Nada sedikit lebih tinggi (lebih cepat)
        else if (comboStep == 3) currentPitch = 1.3f;  // Nada paling tinggi (sangat agresif)

        // 3. Play SFX berdasarkan hasil dengan pitch yang sudah ditentukan
        if (didHitAnyEnemy)
        {
            AudioManager.instance.PlaySFXwithPitch("Damage", currentPitch);
        }
        else
        {
            AudioManager.instance.PlaySFXwithPitch("MC Attack", currentPitch);
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
        
        // 1. Kembalikan Posisi & Fisika
        transform.position = SavePoint.lastCheckpointPosition;
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        // Pastikan collider aktif (jaga-jaga)
        if (playerCollider != null) playerCollider.enabled = true; 
        
        // 2. Bersihkan Visual & Efek
        isFlashing = false;
        flashTimer = 0f;
        if (playerSR != null) 
        {
            playerSR.color = Color.white;
            playerSR.sortingOrder = 0; // Kembalikan agar tidak terus-terusan tenggelam
        }
        
        // 3. Bersihkan Status Combat & Input
        isAttacking = false;
        isDashing = false;
        comboStep = 0;
        currentAnimatingStep = 0;
        if (playerAnim != null) playerAnim.SetInteger("ComboStep", 0);
        
        // 4. Reset Darah dan UI
        health.ResetHealth();
        UpdateHealthUI(); // Sinkronkan UI darah
        
        isDead = false;
        
        // 5. Reset Animasi
        if (playerAnim != null)
        {
            playerAnim.ResetTrigger("Die");
            playerAnim.Play("IdleMC");
        }
        
        if (fadeGroup != null) StartCoroutine(FadeInRoutine());

        this.enabled = true;
    }

    private void ResetAttackState()
    {
        // 1. Reset variabel logic
        isAttacking = false;
        comboStep = 0;
        currentAnimatingStep = 0;

        // 2. Reset Animator parameter
        if (playerAnim != null)
        {
            playerAnim.SetInteger("ComboStep", 0);
            playerAnim.ResetTrigger("Attack"); // Sesuaikan dengan nama trigger attack kamu
            
            // 3. Force ke posisi Idle agar tidak melayang di state Attack
            playerAnim.Play("IdleMC");
        }
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

    // Fungsi ini dipanggil otomatis oleh Animation Event di klip RunMC
    // Variabel timer untuk mencegah bug suara menumpuk saat transisi animasi
    private float lastStepTime = 0f; 

    // Fungsi ini dipanggil otomatis oleh Animation Event di klip RunMC
    public void PlayStepSound()
    {
        if (!isDead)
        {
            // Mencegah bug Unity memanggil event 2x di waktu yang bersamaan
            if (Time.time - lastStepTime < 0.15f) return;
            lastStepTime = Time.time;
            float randomPitch = Random.Range(0.9f, 1.1f);
            AudioManager.instance.PlayWalkSFX("MC Walk", randomPitch);
            //Debug.Log("Step sound played with pitch: " + randomPitch);
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadeGroup == null) yield break;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration); // Gelapkan layar perlahan
            yield return null;
        }
        fadeGroup.alpha = 1f; // Pastikan benar-benar hitam
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadeGroup == null) yield break;
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            fadeGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration); // Terangkan layar perlahan
            yield return null;
        }
        fadeGroup.alpha = 0f; // Pastikan benar-benar transparan
    }

    public void LandSFX()
    {
        AudioManager.instance.PlaySFX("MC Landing");
    }
}