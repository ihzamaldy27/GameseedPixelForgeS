using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMelee : MonoBehaviour, IDamageable, IKnockbackable 
{
    // --- STATE MACHINE ---
    public enum EnemyState { Patrol, Idle, Chase, Telegraph, Attack, Cooldown, Stunned }
    [Header("Current State (Lihat di Inspector)")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Movement & Patrol")]
    public float patrolSpeed = 2f;
    public float patrolWaitTime = 1.5f; 
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;

    [Header("Detection & Chase")]
    public float chaseSpeed = 4f;
    public float sightDistance = 8f; 
    public float awarenessRadius = 3f; // Radius pendengaran
    public LayerMask playerLayer;
    public Transform eyePosition; 

    [Header("Combat & Attack")]
    public float attackRange = 1.2f;
    public int attackDamage = 10;
    public float telegraphDuration = 0.5f; 
    public float attackCooldown = 2f;      
    
    // --- TAMBAHAN: Variabel VFX ---
    [Header("VFX & Animation")]
    public Animator vfxAnim; // Animator untuk SlashVFX
    public string[] slashTriggerNames = { "Slash1", "Slash2", "Slash3" };

    [Header("Telegraph Warning")]
    public bool useWarningSign = true;     
    public GameObject warningSignObject;   

    [Header("Status & Knockback")]
    public int maxHealth = 30;
    public float knockbackForce = 5f;
    public float stunDuration = 0.4f; 

    // Private variables
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private HealthComponent health;
    private bool isFacingRight = true;
    private float stateTimer;
    private Vector2 lastHitPosition;
    private Color originalColor;
    private Transform targetPlayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (sr != null) originalColor = sr.color;
        if (warningSignObject != null) warningSignObject.SetActive(false);

        health = new HealthComponent(maxHealth, 0.2f);
        health.OnDamaged += HandleDamage;
        health.OnDeath += HandleDeath;
    }

    void Update()
    {
        health.UpdateInvincibility(Time.deltaTime);

        if (stateTimer > 0) stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolLogic();
                DetectPlayer();
                break;
            case EnemyState.Idle:
                if (stateTimer <= 0) Flip(); 
                DetectPlayer();
                break;
            case EnemyState.Chase:
                ChaseLogic();
                break;
            case EnemyState.Telegraph:
                if (stateTimer <= 0) ExecuteAttack();
                break;
            case EnemyState.Cooldown:
            case EnemyState.Stunned:
                if (stateTimer <= 0) SwitchState(EnemyState.Patrol);
                break;
        }
    }

    private void PatrolLogic()
    {
        rb.linearVelocity = new Vector2((isFacingRight ? 1 : -1) * patrolSpeed, rb.linearVelocity.y);

        bool isGroundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, 1f, groundLayer);
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, isFacingRight ? Vector2.right : Vector2.left, 0.5f, groundLayer);

        if (!isGroundAhead || isWallAhead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SwitchState(EnemyState.Idle);
            stateTimer = patrolWaitTime; 
        }
    }

    private void DetectPlayer()
    {
        Vector2 rayDir = isFacingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hitForward = Physics2D.Raycast(eyePosition.position, rayDir, sightDistance, playerLayer);

        if (hitForward.collider != null)
        {
            targetPlayer = hitForward.transform;
            SwitchState(EnemyState.Chase);
            return; 
        }

        Collider2D hitAround = Physics2D.OverlapCircle(transform.position, awarenessRadius, playerLayer);
        if (hitAround != null)
        {
            targetPlayer = hitAround.transform;
            SwitchState(EnemyState.Chase);
        }
    }

    private void ChaseLogic()
    {
        if (targetPlayer == null)
        {
            SwitchState(EnemyState.Patrol);
            return;
        }

        // Cek jarak dengan target
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            SwitchState(EnemyState.Telegraph);
            stateTimer = telegraphDuration;
            
            if (useWarningSign && warningSignObject != null) warningSignObject.SetActive(true);
        }
        else if (distanceToPlayer > sightDistance && distanceToPlayer > awarenessRadius)
        {
            targetPlayer = null;
            SwitchState(EnemyState.Patrol);
        }
        else
        {
            int chaseDir = targetPlayer.position.x > transform.position.x ? 1 : -1;
            rb.linearVelocity = new Vector2(chaseDir * chaseSpeed, rb.linearVelocity.y);
            
            if ((chaseDir > 0 && !isFacingRight) || (chaseDir < 0 && isFacingRight)) Flip();
        }
    }

    private void ExecuteAttack()
    {
        // Memulai fungsi Coroutine untuk memberikan jeda waktu
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        if (warningSignObject != null) warningSignObject.SetActive(false);

        // 1. Picu animasi VFX secara acak (DENGAN ANTI-STACKING)
        if (vfxAnim != null && slashTriggerNames.Length > 0)
        {
            // BERSIIHKAN SEMUA ANTREAN TRIGGER TERLEBIH DAHULU
            foreach (string triggerName in slashTriggerNames)
            {
                vfxAnim.ResetTrigger(triggerName);
            }

            // BARU PILIH DAN AKTIFKAN SATU TRIGGER SECARA ACAK
            int randomIndex = Random.Range(0, slashTriggerNames.Length);
            vfxAnim.SetTrigger(slashTriggerNames[randomIndex]);
        }

        // 2. Berikan jeda sepersekian detik agar animasi pedang berayun dulu
        yield return new WaitForSeconds(0.15f); 

        // 3. Pastikan musuh tidak sedang kena stun/mati saat jeda berlangsung
        if (currentState == EnemyState.Telegraph)
        {
            Collider2D playerHit = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
            if (playerHit != null)
            {
                IDamageable playerDamageable = playerHit.GetComponent<IDamageable>();
                if (playerDamageable != null)
                {
                    playerDamageable.TakeDamage(attackDamage);
                }
            }

            SwitchState(EnemyState.Cooldown);
            stateTimer = attackCooldown;
        }
    }

    private void SwitchState(EnemyState newState)
    {
        currentState = newState;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        SwitchState(EnemyState.Patrol); 
    }

    public void ApplyKnockback(Vector2 sourcePosition)
    {
        lastHitPosition = sourcePosition;
    }

    public void TakeDamage(int damage)
    {
        health.TakeDamage(damage);
    }

    private void HandleDamage(int currentHP)
    {
        if (warningSignObject != null) warningSignObject.SetActive(false);
        SwitchState(EnemyState.Stunned);
        stateTimer = stunDuration; 

        Vector2 knockbackDir = ((Vector2)transform.position - lastHitPosition).normalized;
        knockbackDir.y = 0; 
        
        rb.linearVelocity = Vector2.zero; 
        rb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(FlashHit());
    }

    private void HandleDeath()
    {
        health.OnDamaged -= HandleDamage;
        health.OnDeath -= HandleDeath;
        Destroy(gameObject);
    }

    private IEnumerator FlashHit()
    {
        if (sr != null)
        {
            sr.color = Color.red; 
            yield return new WaitForSeconds(0.1f); 
            sr.color = originalColor; 
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        
        if (eyePosition != null)
        {
            Gizmos.color = Color.yellow;
            Vector2 rayDir = isFacingRight ? Vector2.right : Vector2.left;
            Gizmos.DrawRay(eyePosition.position, rayDir * sightDistance);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);
    }

    public void ExecuteDamageHitbox()
    {
        // Dibiarkan kosong karena logika damage Enemy sudah ditangani oleh ExecuteAttack()
    }

    public void EndAttackStep()
    {
        // Dibiarkan kosong karena Enemy menggunakan sistem Timer (State Machine) untuk kembali ke Patrol
    }
}