using UnityEngine;

public class BulletAnimator : MonoBehaviour
{
    [Header("Animation Frames")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 0.1f; // seconds per frame

    private SpriteRenderer _spriteRenderer;
    private int _currentFrame;
    private float _timer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            Debug.LogError("BulletAnimator requires a SpriteRenderer component.");
    }

    private void OnEnable()
    {
        ResetAnimation();
    }

    public void ResetAnimation()
    {
        _currentFrame = 0;
        _timer = 0f;
        if (_spriteRenderer != null && frames != null && frames.Length > 0)
            _spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= frameRate)
        {
            _timer = 0f;
            _currentFrame = (_currentFrame + 1) % frames.Length;
            if (_spriteRenderer != null)
                _spriteRenderer.sprite = frames[_currentFrame];
        }
    }
}
