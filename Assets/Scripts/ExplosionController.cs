using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Sprite[] frames; // 6 frames
    [SerializeField] private float frameRate = 0.05f; // seconds per frame

    private SpriteRenderer _spriteRenderer;
    private float _timer;
    private int _currentFrame;
    private bool _isPlaying;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            Debug.LogError("ExplosionController requires a SpriteRenderer.");
    }

    private void OnEnable()
    {
        // Reset when reused from pool
        _timer = 0f;
        _currentFrame = 0;
        _isPlaying = true;
        SetFrame();
    }

    public void Play(float scale)
    {
        transform.localScale = Vector3.one * scale;
        gameObject.SetActive(true);
        _timer = 0f;
        _currentFrame = 0;
        _isPlaying = true;
        SetFrame();
    }

    private void Update()
    {
        if (!_isPlaying || frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        if (_timer >= frameRate)
        {
            _timer = 0f;
            _currentFrame++;
            if (_currentFrame >= frames.Length)
            {
                // Animation finished – return to pool
                _isPlaying = false;
                ExplosionPoolManager.Instance.ReturnExplosion(this);
                return;
            }
            SetFrame();
        }
    }

    private void SetFrame()
    {
        if (_spriteRenderer != null && frames != null && _currentFrame < frames.Length)
            _spriteRenderer.sprite = frames[_currentFrame];
    }
}
