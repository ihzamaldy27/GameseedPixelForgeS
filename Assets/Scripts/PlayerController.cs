using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
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
    public Animator playerAnim;       // Animator karakter utama
    public Animator vfxAnim;          // Animator objek SlashVFX
    public Transform attackPoint;     // Titik tengah area serangan

    [Header("Pengaturan Combat")]
    public float attackRange = 0.8f;
    public LayerMask enemyLayer;
    public float damageAmount = 10f;

    [Header("Sistem Combo")]
    public float maxComboDelay = 0.6f; // Waktu maksimal pemain boleh menunda klik berikutnya
    
    private int comboStep = 0;
    private int currentAnimatingStep = 0; // TAMBAHAN: Untuk melacak animasi yang sedang diputar
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
    }

    void Update()
    {
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

        // Reset kombo jika pemain terlalu lama tidak menekan tombol attack
        if (Time.time - lastClickedTime > maxComboDelay && !isAttacking)
        {
            comboStep = 0;
            currentAnimatingStep = 0;
        }

        CheckGrounded();
        ResetCombo();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = new Vector2((isFacingRight ? 1 : -1) * dashSpeed, 0f);
            return;
        }

        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && !isFacingRight) Flip();
        else if (moveInput.x < 0 && isFacingRight) Flip();
    }

    // --- INPUT SYSTEM CALLBACKS ---
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && isGrounded && !isDashing)
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
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            lastClickedTime = Time.time;

            // Jika sedang idle, mulai serangan dari 1
            if (!isAttacking)
            {
                comboStep = 1;
                TriggerAttackAnimation();
            }
            else // Jika sedang menyerang, tambahkan antrean combo
            {
                comboStep++;
                comboStep = Mathf.Clamp(comboStep, 1, 3);
            }
        }
    }

    private void TriggerAttackAnimation()
    {
        isAttacking = true;
        currentAnimatingStep++; // Naikkan step animasi yang sedang berjalan
        
        // Memicu animasi karakter
        playerAnim.SetInteger("ComboStep", currentAnimatingStep);
        playerAnim.SetTrigger("Attack");

        // // Opsional: Memicu animasi VFX hanya jika animatornya ada
        // if (vfxAnim != null)
        // {
        //     vfxAnim.SetTrigger("Slash" + currentAnimatingStep); 
        // }
    }

    // --- UTILITY METHODS ---
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
            if (!afterimagePool[i].gameObject.activeInHierarchy)
            {
                poolable = afterimagePool[i];
                break;
            }
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
            // 1. Cek & Kirim data posisi penyerang DULU (jika musuh mendukung Knockback)
            IKnockbackable knockbackable = enemy.GetComponent<IKnockbackable>();
            if (knockbackable != null)
            {
                knockbackable.ApplyKnockback(transform.position);
            }

            // 2. Cek & Eksekusi Damage (Mode Minigames & Gameplay)
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
        // Cek apakah pemain sudah menekan tombol lagi dan MASIH ADA sisa combo
        if (comboStep > currentAnimatingStep && (Time.time - lastClickedTime <= maxComboDelay))
        {
            TriggerAttackAnimation();
        }
        else
        {
            // Jika mentok di combo ke-3 atau waktu tunggu habis, hentikan serangan
            isAttacking = false;
            comboStep = 0;
            currentAnimatingStep = 0;
            playerAnim.SetInteger("ComboStep", 0);
        }
    }
}