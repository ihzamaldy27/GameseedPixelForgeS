using UnityEngine;

public class SpriteDirectionComponent : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite straightSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    [Header("Sensitivity")]
    [SerializeField] private float threshold = 0.1f; // vertical input threshold

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            Debug.LogError("SpriteDirectionHandler requires a SpriteRenderer on the same GameObject.");
    }

    public void SetDirection(Vector2 direction)
    {
        if (_spriteRenderer == null) return;

        // Normalize to get pure vertical component
        float vertical = direction.normalized.y;

        if (vertical > threshold)
            _spriteRenderer.sprite = upSprite;
        else if (vertical < -threshold)
            _spriteRenderer.sprite = downSprite;
        else
            _spriteRenderer.sprite = straightSprite;
    }
}
