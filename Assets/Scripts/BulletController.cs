using UnityEngine;

public class BulletController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;

    [Header("Damage")]
    public int damage = 1;

    // Reference to the original prefab (used by the pool manager)
    public BulletController OriginalPrefab { get; set; }

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
            Debug.LogError("No MainCamera found in scene!");
    }

    private void OnEnable()
    {
        // Reset any state if needed (e.g., rotation, position is set by ShootingHandler)
    }

    private void Update()
    {
        // Move along the bullet's local right axis
        transform.Translate(Vector3.right * speed * Time.deltaTime);

        // Off‑screen check – return to pool if too far
        if (_mainCamera != null)
        {
            Vector3 viewportPos = _mainCamera.WorldToViewportPoint(transform.position);
            if (viewportPos.x < -0.5f || viewportPos.x > 1.5f ||
                viewportPos.y < -0.5f || viewportPos.y > 1.5f)
            {
                ReturnToPool();
            }
        }
    }

    public void ReturnToPool()
    {
        if (BulletPoolManager.Instance != null)
            BulletPoolManager.Instance.ReturnBullet(this);
        else
            Destroy(gameObject); // fallback
    }
}