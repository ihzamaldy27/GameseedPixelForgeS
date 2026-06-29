using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFlying : MonoBehaviour, IDamageable, IKnockbackable 
{
    public enum EnemyState { Patrol, Chase, Telegraph, Dash, Cooldown, Stunned }
    [Header("Current State")]
    public EnemyState currentState = EnemyState.Patrol;

    [Header("Movement & Patrol")]
    public float patrolSpeed = 2f;
    public float patrolRadius = 5f; // Jarak dia melayang-layang dari titik awal
    public float waypointWaitTime = 1f;

    [Header("Detection & Chase")]
    public float chaseSpeed = 3f;
    public float sightDistance = 12f; 
    public float safeDistance = 5f; // Jarak aman dia menjaga jarak dari player
    public LayerMask playerLayer;

    [Header("Combat & Dash Attack")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.4f; // Berapa lama dia melesat
    public int attackDamage = 15;
    public float telegraphDuration = 0.6f; 
    public float attackCooldown = 3f;      
    
    [Header("VFX & Animation")]
    public Animator enemyAnim; 
    public string dashTriggerName = "Dash"; // Pemicu animasi saat melesat

    [Header("Telegraph Warning")]
    public bool useWarningSign = true;     
    public GameObject warningSignObject;   

    [Header("Status & Knockback")]
    public int maxHealth = 20; 
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

    // Variabel khusus Flying
    private Vector2 startPosition;
    private Vector2 randomWaypoint;
    private Vector2 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // SANGAT PENTING: Matikan gravitasi agar dia tidak jatuh
        rb.gravityScale = 0f; 
        
        startPosition = transform.position;
        PickNewWaypoint();

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
            case EnemyState.Chase:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                ChaseLogic();
                break;
            case EnemyState.Telegraph:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                TelegraphLogic();
                break;
            case EnemyState.Dash:
                // Sedang melesat, logika kecepatannya diatur di ExecuteDash()
                if (stateTimer <= 0) 
                {
                    rb.linearVelocity = Vector2.zero; // Rem setelah dash selesai
                    SwitchState(EnemyState.Cooldown);
                    stateTimer = attackCooldown;
                }
                break;
            case EnemyState.Cooldown:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (stateTimer <= 0) SwitchState(EnemyState.Patrol);
                break;
            case EnemyState.Stunned:
                if (stateTimer <= 0) SwitchState(EnemyState.Patrol);
                break;
        }
    }

    private void PatrolLogic()
    {
        // Terbang menuju waypoint acak
        Vector2 direction = (randomWaypoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * patrolSpeed;

        FaceDirection(direction.x);

        // Jika sudah sampai di waypoint
        if (Vector2.Distance(transform.position, randomWaypoint) < 0.5f)
        {
            rb.linearVelocity = Vector2.zero;
            if (stateTimer <= 0)
            {
                stateTimer = waypointWaitTime; // Diam sejenak
                PickNewWaypoint();             // Lalu cari titik baru
            }
        }
    }

    private void PickNewWaypoint()
    {
        // Mencari titik terbang acak di sekitar posisi awalnya
        float randomX = Random.Range(-patrolRadius, patrolRadius);
        float randomY = Random.Range(-patrolRadius, patrolRadius);
        randomWaypoint = startPosition + new Vector2(randomX, randomY);
    }

    private void DetectPlayer()
    {
        // Deteksi memutar 360 derajat (lingkaran) karena dia terbang bebas
        Collider2D hit = Physics2D.OverlapCircle(transform.position, sightDistance, playerLayer);
        if (hit != null)
        {
            targetPlayer = hit.transform;
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
        Vector2 directionToPlayer = (targetPlayer.position - transform.position).normalized;

        FaceDirection(directionToPlayer.x);

        if (distanceToPlayer > sightDistance)
        {
            // Player kabur jauh, lupakan dan kembali patroli
            targetPlayer = null;
            SwitchState(EnemyState.Patrol);
        }
        else if (distanceToPlayer > safeDistance)
        {
            // Jika masih jauh dari jarak aman, mendekatlah
            rb.linearVelocity = directionToPlayer * chaseSpeed;
        }
        else
        {
            // Sudah berada di jarak aman (melayang/menjaga posisi)
            rb.linearVelocity = Vector2.zero;

            // Jika cooldown siap, mulai membidik!
            if (stateTimer <= 0)
            {
                SwitchState(EnemyState.Telegraph);
                stateTimer = telegraphDuration;
                if (useWarningSign && warningSignObject != null) warningSignObject.SetActive(true);
            }
        }
    }

    private void TelegraphLogic()
    {
        if (targetPlayer != null)
        {
            // Terus kunci arah hadap ke player saat membidik
            Vector2 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            FaceDirection(directionToPlayer.x);
            
            // Kunci arah dash untuk dieksekusi nanti
            dashDirection = directionToPlayer; 
        }

        rb.linearVelocity = Vector2.zero; // Berhenti melayang saat membidik

        // Waktu bidik habis, luncurkan serangan!
        if (stateTimer <= 0) 
        {
            ExecuteDash();
        }
    }

    private void ExecuteDash()
    {
        if (warningSignObject != null) warningSignObject.SetActive(false);

        if (enemyAnim != null)
        {
            enemyAnim.ResetTrigger(dashTriggerName);
            enemyAnim.SetTrigger(dashTriggerName);
        }

        SwitchState(EnemyState.Dash);
        stateTimer = dashDuration;
        
        // Melesat dengan kecepatan tinggi ke arah yang sudah dikunci!
        rb.linearVelocity = dashDirection * dashSpeed; 
    }

    // --- DETEKSI SERANGAN SAAT DASH ---
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Jika sedang nge-dash dan menabrak Player, berikan damage!
        if (currentState == EnemyState.Dash && collision.gameObject.CompareTag("Player"))
        {
            IDamageable playerDamageable = collision.gameObject.GetComponent<IDamageable>();
            if (playerDamageable != null)
            {
                playerDamageable.TakeDamage(attackDamage);
            }
            
            // Opsional: Langsung berhenti/terpental sedikit setelah menabrak player
            rb.linearVelocity = Vector2.zero;
            SwitchState(EnemyState.Cooldown);
            stateTimer = attackCooldown;
        }
    }

    private void SwitchState(EnemyState newState) { currentState = newState; }

    private void FaceDirection(float directionX)
    {
        if (directionX > 0 && !isFacingRight) Flip();
        else if (directionX < 0 && isFacingRight) Flip();
    }

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

        // Knockback terbang 360 derajat (boleh terpental ke atas/bawah)
        Vector2 knockbackDir = ((Vector2)transform.position - lastHitPosition).normalized;
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(startPosition == Vector2.zero ? (Vector2)transform.position : startPosition, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, safeDistance);
    }
}