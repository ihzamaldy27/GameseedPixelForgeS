using UnityEngine;

public class ScreenClampHandler : MonoBehaviour
{
    [Header("Clamp Settings")]
    [SerializeField] private float padding = 0.5f; // offset from screen edges
    [SerializeField] private bool clampX = true;
    [SerializeField] private bool clampY = true;

    private Camera _mainCamera;
    private float _halfWidth;
    private float _halfHeight;
    private float _leftBound;
    private float _rightBound;
    private float _bottomBound;
    private float _topBound;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("ScreenClampHandler requires a Main Camera in the scene!");
            return;
        }

        UpdateBounds();
    }

    private void UpdateBounds()
    {
        if (_mainCamera == null) return;

        // Calculate world bounds based on camera viewport
        Vector3 bottomLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = _mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

        // Get the player's renderer bounds if available for more accurate clamping
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            _halfWidth = renderer.bounds.extents.x;
            _halfHeight = renderer.bounds.extents.y;
        }
        else
        {
            // Fallback: use collider bounds or assume a default size
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                _halfWidth = collider.bounds.extents.x;
                _halfHeight = collider.bounds.extents.y;
            }
            else
            {
                // Default size if no renderer or collider
                _halfWidth = 0.5f;
                _halfHeight = 0.5f;
            }
        }

        // Apply padding
        _leftBound = bottomLeft.x + _halfWidth + padding;
        _rightBound = topRight.x - _halfWidth - padding;
        _bottomBound = bottomLeft.y + _halfHeight + padding;
        _topBound = topRight.y - _halfHeight - padding;
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) return;

        // Update bounds in case camera moved (e.g., if scrolling)
        UpdateBounds();

        // Clamp position
        Vector3 pos = transform.position;
        if (clampX)
            pos.x = Mathf.Clamp(pos.x, _leftBound, _rightBound);
        if (clampY)
            pos.y = Mathf.Clamp(pos.y, _bottomBound, _topBound);
        transform.position = pos;
    }

    // Public method to update bounds manually (e.g., if camera changes)
    public void RefreshBounds()
    {
        UpdateBounds();
    }

    // Optional: visualize bounds in editor
    private void OnDrawGizmosSelected()
    {
        if (_mainCamera == null) return;

        Vector3 bottomLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = _mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

        float hw = 0.5f;
        float hh = 0.5f;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            hw = renderer.bounds.extents.x;
            hh = renderer.bounds.extents.y;
        }

        float left = bottomLeft.x + hw + padding;
        float right = topRight.x - hw - padding;
        float bottom = bottomLeft.y + hh + padding;
        float top = topRight.y - hh - padding;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(left, bottom, 0), new Vector3(right, bottom, 0));
        Gizmos.DrawLine(new Vector3(right, bottom, 0), new Vector3(right, top, 0));
        Gizmos.DrawLine(new Vector3(right, top, 0), new Vector3(left, top, 0));
        Gizmos.DrawLine(new Vector3(left, top, 0), new Vector3(left, bottom, 0));
    }
}
