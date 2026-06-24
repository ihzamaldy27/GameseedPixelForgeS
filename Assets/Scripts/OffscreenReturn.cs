using UnityEngine;

public class OffscreenReturn : MonoBehaviour
{
    [Tooltip("Seconds off-screen before returning to pool")]
    [SerializeField] private float offscreenDelay = 2f;

    private EnemyController _enemy;
    private Camera _mainCamera;
    private float _offScreenTimer = 0f;
    private bool _isOffScreen = false;

    private void Awake()
    {
        _enemy = GetComponent<EnemyController>();
        if (_enemy == null)
        {
            Debug.LogError("OffscreenReturn needs an EnemyController component on the same GameObject.");
        }
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("No MainCamera found in the scene.");
        }
    }

    private void OnEnable()
    {
        // Reset state when the enemy is reused from the pool
        _isOffScreen = false;
        _offScreenTimer = 0f;
    }

    private void Update()
    {
        if (_enemy == null || _mainCamera == null) return;

        // Check if the enemy is outside the camera's viewport
        Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);
        bool offScreen = viewportPos.x < -0.1f || viewportPos.x > 1.1f ||
                         viewportPos.y < -0.1f || viewportPos.y > 1.1f;

        if (offScreen)
        {
            if (!_isOffScreen)
            {
                _isOffScreen = true;
                _offScreenTimer = 0f;
            }

            _offScreenTimer += Time.deltaTime;
            if (_offScreenTimer >= offscreenDelay)
            {
                // Return the enemy to the pool
                _enemy.ReturnToPool();
                // Reset timer to avoid repeated triggers (though the object will be deactivated)
                _offScreenTimer = 0f;
            }
        }
        else
        {
            // Reset timer if the enemy comes back on-screen
            _isOffScreen = false;
            _offScreenTimer = 0f;
        }
    }
}