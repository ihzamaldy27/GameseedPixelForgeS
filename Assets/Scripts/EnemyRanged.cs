using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRanged : MonoBehaviour, IDamageable, IKnockbackable 
{
    public enum EnemyState { Patrol, Idle, Chase, Telegraph, Attack, Cooldown, Stunned }
    [Header("Current State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Movement & Patrol")]
    public float patrolSpeed = 2f;
    public float patrolWaitTime = 1.5f; 
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;

    [Header("Detection & Chase")]
    public float chaseSpeed = 3f;
    public float sightDistance = 10f; // Jarak pandang biasanya lebih jauh dari Melee
    public float awarenessRadius = 4f; 
    public float shootRange = 7f; // Jarak berhenti untuk mulai menembak
    public LayerMask playerLayer;
    public Transform eyePosition; 

    [Header("Combat & Attack")]
    public GameObject bulletPrefab;
    public Transform firePoint; // Titik keluarnya peluru
    public float telegraphDuration = 0.6f; // Waktu membidik
    public float attackCooldown = 2.5f;      
    
    [Header("VFX & Animation")]
    public Animator enemyAnim; // Jika ada animasi menembak
    public string shootTriggerName = "Shoot";

    [Header("Telegraph Warning")]
    public bool useWarningSign = true;     
    public GameObject warningSignObject;   

    [Header("Status & Knockback")]
    public int maxHealth = 25; // Ranged biasanya HP-nya lebih kecil dari Melee
    public float knockbackForce = 6f;
    public float stunDuration = 0.4f; 

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
                if (stateTimer <= 0) 
                {
                    Flip(); 
                    SwitchState(EnemyState.Patrol); // <--- Paksa kembali ke jalan setelah muter
                }
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

        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        int chaseDir = targetPlayer.position.x > transform.position.x ? 1 : -1;

        // Pastikan musuh selalu menghadap player saat di mode Chase
        if ((chaseDir > 0 && !isFacingRight) || (chaseDir < 0 && isFacingRight)) Flip();

        // Jika player sudah masuk jarak tembak
        if (distanceToPlayer <= shootRange)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Berhenti lari
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
            // Maju mendekat jika masih terlalu jauh
            rb.linearVelocity = new Vector2(chaseDir * chaseSpeed, rb.linearVelocity.y);
        }
    }

    private void ExecuteAttack()
    {
        if (warningSignObject != null) warningSignObject.SetActive(false);

        if (enemyAnim != null)
        {
            enemyAnim.SetTrigger(shootTriggerName);
        }

        // --- FIX LOGIKA ARRAH PELURU ---
        if (bulletPrefab != null && firePoint != null)
        {
            // Jika musuh hadap kanan, pakai rotasi normal (0). Jika kiri, putar 180 derajat di sumbu Y.
            Quaternion bulletRotation = isFacingRight ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            
            // Instansiasi peluru dengan rotasi baru yang sudah disesuaikan
            Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        }
        // -------------------------------

        SwitchState(EnemyState.Cooldown);
        stateTimer = attackCooldown;
    }

    private void SwitchState(EnemyState newState) { currentState = newState; }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        
        // Membalik objek secara keseluruhan (termasuk firePoint)
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        
        // Catatan: Jika musuh berbalik (scale X jadi negatif), 
        // rotasi Y dari Transform secara teknis terbalik sehingga peluru akan meluncur ke arah yang benar.
    }

    public void ApplyKnockback(Vector2 sourcePosition) { lastHitPosition = sourcePosition; }

    public void TakeDamage(int damage) { health.TakeDamage(damage); }

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
        Gizmos.DrawWireSphere(transform.position, shootRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, awarenessRadius);
    }
}