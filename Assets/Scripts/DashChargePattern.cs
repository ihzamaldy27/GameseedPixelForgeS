using UnityEngine;
using System.Collections;

public class DashChargePattern : MonoBehaviour, IBossAttackPattern
{
    [Header("Dash Settings")]
    [SerializeField] private float stepBackDistance = 2f;
    [SerializeField] private float stepBackSpeed = 5f;
    [SerializeField] private float chargeSpeed = 15f;
    [SerializeField] private float offScreenDelay = 2f; // time before reappearing

    private BossMovement _movement;
    private Transform _transform;

    private void Awake()
    {
        _movement = GetComponent<BossMovement>();
        _transform = transform;
        if (_movement == null)
            Debug.LogError("DashChargePattern requires BossMovement component on the same GameObject.");
    }

    public IEnumerator ExecuteAttack()
    {
        if (_movement == null) yield break;

        // Store original position
        Vector3 startPos = _transform.position;

        // 1. Pause normal movement
        _movement.PauseMovement();

        // 2. Step backward
        Vector3 stepBackTarget = startPos + Vector3.right * stepBackDistance;
        float stepBackTimer = 0f;
        float stepBackDuration = stepBackDistance / stepBackSpeed;

        while (stepBackTimer < stepBackDuration)
        {
            stepBackTimer += Time.deltaTime;
            float t = Mathf.Clamp01(stepBackTimer / stepBackDuration);
            _transform.position = Vector3.Lerp(startPos, stepBackTarget, t);
            yield return null;
        }
        _transform.position = stepBackTarget;

        // 3. Charge forward (move left) until off-screen
        Camera mainCamera = Camera.main;
        float leftEdge = mainCamera != null ? 
            mainCamera.ViewportToWorldPoint(new Vector3(-0.3f, 0f, 0f)).x : 20f;

        while (_transform.position.x > leftEdge)
        {
            _transform.Translate(Vector3.left * chargeSpeed * Time.deltaTime);
            yield return null;
        }

        // 4. Wait off-screen
        yield return new WaitForSeconds(offScreenDelay);

        // 5. Reappear from the right edge (preserve Y position)
        float currentY = _transform.position.y;
        _movement.ResetToRightEdge(currentY);

        // 6. Resume normal movement (entry + patrol)
        _movement.ResumeMovement();
    }
}