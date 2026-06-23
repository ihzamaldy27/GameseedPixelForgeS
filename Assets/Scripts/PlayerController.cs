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

    [Header("Dash Effects")]
    [SerializeField] private GameObject afterimagePrefab; 
    [SerializeField] private float afterimageSpawnRate = 0.05f; 
    private float nextSpawnTime;

    private List<AfterimageFade> afterimagePool = new List<AfterimageFade>();

    private Rigidbody2D rb;
    private SpriteRenderer playerSR;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerSR = GetComponent<SpriteRenderer>();

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

        // 3. UPDATE TIMER KEBAL PLAYER
        health.UpdateInvincibility(Time.deltaTime);

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
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

        if (playerAnim != null)
        {
            playerAnim.SetFloat("Speed", Mathf.Abs(moveInput.x));
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
        if (value.isPressed && isGrounded && !isDashing)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
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
}