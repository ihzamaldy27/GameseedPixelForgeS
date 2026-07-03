using UnityEngine;
using System.Collections;

public class PlayerVictoryDash : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float stepBackDistance = 2f;
    [SerializeField] private float stepBackSpeed = 3f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float pauseBeforeDash = 0.5f;
    [SerializeField] private float delayBeforeVictory = 1.5f; // wait for boss death animation

    [Header("References")]
    [SerializeField] private PlayerPlane playerController;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private ScreenClampHandler screenClamp;

    private bool _isDashing = false;
    private BossController _boss;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private WaveManager _waveManager;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerPlane>();
        
        if (playerRenderer == null)
            playerRenderer = GetComponent<SpriteRenderer>();
        
        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();

        // Find ScreenClampHandler if not assigned
        if (screenClamp == null)
            screenClamp = GetComponent<ScreenClampHandler>();

        if (screenClamp == null)
            Debug.LogWarning("PlayerVictoryDash: ScreenClampHandler not found. Clamping won't be disabled during dash.");
        
        // Find WaveManager and subscribe to boss spawn events
        _waveManager = FindFirstObjectByType<WaveManager>();
        if (_waveManager != null)
        {
            _waveManager.OnBossSpawned += HandleBossSpawned;
        }
        else
        {
            Debug.LogWarning("PlayerVictoryDash: WaveManager not found in scene.");
        }

        // Also try to find existing boss (if already spawned)
        TryFindBoss();
    }

    private void OnDestroy()
    {
         if (_waveManager != null)
            _waveManager.OnBossSpawned -= HandleBossSpawned;

        if (_boss != null)
            _boss.OnDeathStarted -= HandleBossDeathStarted;
    }

    private void TryFindBoss()
    {
        // Try to find any existing boss in the scene
        BossController existingBoss = FindFirstObjectByType<BossController>();
        if (existingBoss != null)
        {
            HandleBossSpawned(existingBoss);
        }
    }

    private void HandleBossSpawned(BossController boss)
    {
        // Unsubscribe from previous boss if any
        if (_boss != null)
            _boss.OnDeathStarted -= HandleBossDeathStarted;

        // Subscribe to new boss
        _boss = boss;
        _boss.OnDeathStarted += HandleBossDeathStarted;
        Debug.Log("PlayerVictoryDash: Subscribed to boss death event.");
    }

    private void HandleBossDeathStarted()
    {
        if (_isDashing) return;
        StartCoroutine(VictoryDashSequence());
    }

    private IEnumerator VictoryDashSequence()
    {
        _isDashing = true;

        if (screenClamp != null)
            screenClamp.EnableClamping(false);

        // 1. Wait for boss death animation to play
        yield return new WaitForSeconds(delayBeforeVictory);

        // 2. Disable player controls and collision
        if (playerController != null)
            playerController.enabled = false;

        if (playerCollider != null)
            playerCollider.enabled = false;

        // 3. Store start position and rotation
        _startPosition = transform.position;
        _startRotation = transform.rotation;

        // 4. Step back (move left)
        Vector3 stepBackTarget = _startPosition + Vector3.left * stepBackDistance;
        float stepBackDuration = stepBackDistance / stepBackSpeed;
        float stepBackTimer = 0f;

        while (stepBackTimer < stepBackDuration)
        {
            stepBackTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stepBackTimer / stepBackDuration);
            transform.position = Vector3.Lerp(_startPosition, stepBackTarget, t);
            yield return null;
        }
        transform.position = stepBackTarget;

        // 5. Brief pause before dashing
        yield return new WaitForSeconds(pauseBeforeDash);

        // 5.5 Disable the music
        AudioManager.instance.StopBGM(2f);

        // 6. Dash forward (move right) until off-screen
        Camera mainCamera = Camera.main;
        float rightEdge = mainCamera != null ? 
            mainCamera.ViewportToWorldPoint(new Vector3(1.5f, 0f, 0f)).x : 25f;

        Vector3 dashStartPosition = transform.position;
        float dashDistance = rightEdge - dashStartPosition.x;
        float dashDuration = dashDistance / dashSpeed;
        float dashTimer = 0f;

        while (transform.position.x < rightEdge)
        {
            dashTimer += Time.deltaTime;
            float t = Mathf.Clamp01(dashTimer / dashDuration);
            
            // Use ease-in for dramatic effect
            float eased = t * t; // quadratic ease-in
            float currentX = Mathf.Lerp(dashStartPosition.x, rightEdge, eased);
            transform.position = new Vector3(currentX, transform.position.y, transform.position.z);
            
            yield return null;
        }

        // 7. Hide player (optional - can leave visible as it goes off-screen)
        if (playerRenderer != null)
            playerRenderer.enabled = false;

        // 8. Wait a moment then reset player (for potential reuse)
        yield return new WaitForSeconds(0.5f);

        // 9. Optional: show victory UI or load next level
        // You can trigger a "Victory" event here
        
        Debug.Log("Player victory dash complete!");

        _isDashing = false;

        // 10. Load cutscene
        GameSceneManager.instance.LoadSceneAsync(GameSceneManager.SceneName.CutSceneFinal);
    }

    // Public method to manually trigger victory (e.g., for testing)
    public void TriggerVictory()
    {
        if (!_isDashing)
            StartCoroutine(VictoryDashSequence());
    }

    // Reset method for checkpoint respawn
    public void ResetState()
    {
        _isDashing = false;
        if (playerController != null)
            playerController.enabled = true;
        
        if (playerCollider != null)
            playerCollider.enabled = true;
        
        if (playerRenderer != null)
            playerRenderer.enabled = true;

        transform.position = _startPosition;
        transform.rotation = _startRotation;
    }
}