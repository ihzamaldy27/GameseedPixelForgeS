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
    public float sightDistance = 10f; 
    public float awarenessRadius = 4f; 
    public float shootRange = 7f; 
    public LayerMask playerLayer;
    public Transform eyePosition; 

    [Header("Combat & Attack")]
    public GameObject bulletPrefab;
    public Transform firePoint; 
    public float telegraphDuration = 0.6f; 
    public float attackCooldown = 2.5f;      
    
    [Header("VFX & Animation")]
    public Animator enemyAnim; 
    public string shootTriggerName = "Shoot";

    [Header("Telegraph Warning")]
    public bool useWarningSign = true;     
    public GameObject warningSignObject;   

    [Header("Status & Knockback")]
    public int maxHealth = 25; 
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
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (stateTimer <= 0) 
                {
                    Flip(); 
                    SwitchState(EnemyState.Patrol); 
                }
                DetectPlayer();
                break;
            case EnemyState.Chase:
                ChaseLogic();
                break;
            case EnemyState.Telegraph:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                // --- PERBAIKAN: BATAL MENEMBAK JIKA PLAYER KABUR ---
                if (targetPlayer != null)
                {
                    float dist = Vector2.Distance(transform.position, targetPlayer.position);
                    if (dist > shootRange)
                    {
                        // Player kabur dari jangkauan tembak! Batal nembak, lanjut kejar.
                        if (warningSignObject != null) warningSignObject.SetActive(false);
                        SwitchState(EnemyState.Chase);
                        break; 
                    }
                }
                else
                {
                    SwitchState(EnemyState.Patrol);
                    break;
                }
                
                // Jika player masih di dalam jangkauan dan waktu bidik habis, tembak!
                if (stateTimer <= 0) ExecuteAttack();
                break;
            case EnemyState.Cooldown:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // <-- TAMBAHKAN REM INI
                if (stateTimer <= 0) SwitchState(EnemyState.Patrol);
                break;
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

        bool isGroundAhead = Physics2D.Raycast(groundCheck.position, Vector2.down, 1f, groundLayer);
        bool isWallAhead = Physics2D.Raycast(wallCheck.position, isFacingRight ? Vector2.right : Vector2.left, 0.5f, groundLayer);

        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);
        int chaseDir = targetPlayer.position.x > transform.position.x ? 1 : -1;

        if ((chaseDir > 0 && !isFacingRight) || (chaseDir < 0 && isFacingRight)) Flip();

        if (distanceToPlayer <= shootRange)
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
        else if (!isGroundAhead || isWallAhead) 
        {
            targetPlayer = null; 
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            
            if (enemyAnim != null)
            {
                enemyAnim.ResetTrigger("Idle"); // Anti-stack untuk idle
                enemyAnim.SetTrigger("Idle");
            }
            
            SwitchState(EnemyState.Idle); 
            stateTimer = patrolWaitTime; 
        }
        else
        {
            rb.linearVelocity = new Vector2(chaseDir * chaseSpeed, rb.linearVelocity.y);
        }
    }

    private void ExecuteAttack()
    {
        if (warningSignObject != null) warningSignObject.SetActive(false);

        if (enemyAnim != null)
        {
            // --- PERBAIKAN: ANTI-STACKING ---
            enemyAnim.ResetTrigger(shootTriggerName); 
            enemyAnim.SetTrigger(shootTriggerName);
        }

        if (bulletPrefab != null && firePoint != null)
        {
            Quaternion bulletRotation = isFacingRight ? Quaternion.identity : Quaternion.Euler(0, 180, 0);
            Instantiate(bulletPrefab, firePoint.position, bulletRotation);
        }

        SwitchState(EnemyState.Cooldown);
        stateTimer = attackCooldown;
    }

    private void SwitchState(EnemyState newState) { currentState = newState; }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
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