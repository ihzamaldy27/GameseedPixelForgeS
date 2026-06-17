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

    [Header("Combat (3x Combo)")]
    public float comboResetTime = 1f;
    private int comboStep = 0;
    private float lastAttackTime;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;
    private bool isGrounded;

    [Header("Dash Effects")]
    [SerializeField] private GameObject afterimagePrefab; // Masukkan prefab tadi ke sini
    [SerializeField] private float afterimageSpawnRate = 0.05f; // Seberapa sering bayangan muncul saat dash
    private float nextSpawnTime;

    // Object Pool Sederhana
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

        CheckGrounded();
        ResetCombo();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            // Eksekusi physics dash
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
            SpawnAfterimage(); // Spawn pertama langsung saat tombol ditekan
            nextSpawnTime = Time.time + afterimageSpawnRate;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isDashing && isGrounded) // Asumsi attack hanya di tanah
        {
            lastAttackTime = Time.time;
            comboStep++;
            
            if (comboStep > 3) 
            {
                comboStep = 1; // Kembali ke serangan pertama jika melebihi 3
            }

            Debug.Log($"Eksekusi Attack Combo: {comboStep}");
            
            // TODO: Panggil Animator di sini
            // animator.SetInteger("ComboStep", comboStep);
            // animator.SetTrigger("Attack");
        }
    }

    // --- UTILITY METHODS ---
    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void ResetCombo()
    {
        // Reset kombo jika pemain tidak menyerang dalam waktu tertentu
        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
            // animator.SetInteger("ComboStep", 0);
        }
    }

    private void SpawnAfterimage()
    {
        // Cari objek yang sedang 'mati' di pool
        AfterimageFade poolable = null;
        for (int i = 0; i < afterimagePool.Count; i++)
        {
            if (!afterimagePool[i].gameObject.activeInHierarchy)
            {
                poolable = afterimagePool[i];
                break;
            }
        }

        // Jika tidak ada yang mati, buat baru
        if (poolable == null)
        {
            GameObject newObj = Instantiate(afterimagePrefab);
            poolable = newObj.GetComponent<AfterimageFade>();
            afterimagePool.Add(poolable); // Masukkan ke pool
        }

        // Aktifkan bayangan dengan posisi & sprite player saat ini
        poolable.SetAfterimage(playerSR.sprite, transform.position, transform.rotation, transform.localScale);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
    
    // Untuk melihat radius GroundCheck di Editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}