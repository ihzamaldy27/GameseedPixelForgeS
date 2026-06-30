using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BossDamageStateSprite
{
    [Tooltip("Health ratio threshold (0..1). The first state with HP/maxHP <= threshold will be used.")]
    public float healthThreshold;
    [Tooltip("3 frames for the straight/patrol animation.")]
    public Sprite[] frames; // must have exactly 3 frames
}

public class BossSpriteHandler : MonoBehaviour
{
    [Header("Damage States (sorted by threshold descending)")]
    [SerializeField] private List<BossDamageStateSprite> damageStates;

    [Header("Animation")]
    [SerializeField] private float frameRate = 0.15f; // seconds per frame

    [Header("Debug")]
    [SerializeField] private bool logStateChanges = false;

    private SpriteRenderer _spriteRenderer;
    private BossController _boss;
    private float _timer;
    private int _currentFrameIndex;
    private int _currentStateIndex = -1;
    private float _currentHealthRatio = 1f;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null)
            Debug.LogError("BossSpriteHandler requires a SpriteRenderer!");

        _boss = GetComponent<BossController>();
        if (_boss != null)
        {
            _boss.OnHealthChanged += UpdateDamageState;
            // Initial state with full health
            _currentHealthRatio = 1f;
            UpdateStateIndex();
            SetSpriteForState();
        }
        else
        {
            Debug.LogError("BossSpriteHandler needs a BossController on the same GameObject.");
        }
    }

    private void OnEnable()
    {
        // Reset animation when re‑used (if pooling is added later)
        _timer = 0f;
        _currentFrameIndex = 0;
        if (_boss != null)
        {
            // Re‑evaluate health state
            _currentHealthRatio = (float)_boss.CurrentHP / _boss.MaxHP;
            UpdateStateIndex();
            SetSpriteForState();
        }
    }

    private void OnDestroy()
    {
        if (_boss != null)
            _boss.OnHealthChanged -= UpdateDamageState;
    }

    private void UpdateDamageState(int currentHP, int maxHP)
    {
        _currentHealthRatio = (float)currentHP / maxHP;
        UpdateStateIndex();
    }

    private void UpdateStateIndex()
    {
        // Find the first state whose threshold is >= current ratio
        int newIndex = -1;
        for (int i = 0; i < damageStates.Count; i++)
        {
            if (_currentHealthRatio <= damageStates[i].healthThreshold)
            {
                newIndex = i;
            }
            else
            {
                break;
            }
        }
        // If no state matches, use the last state (lowest threshold) – fallback
        if (newIndex == -1 && damageStates.Count > 0)
            newIndex = damageStates.Count - 1;

        if (newIndex != _currentStateIndex)
        {
            _currentStateIndex = newIndex;
            if (logStateChanges)
                Debug.Log($"Boss state changed to index {_currentStateIndex} at health ratio {_currentHealthRatio}");
            // Reset animation when state changes
            _timer = 0f;
            _currentFrameIndex = 0;
            SetSpriteForState();
        }
    }

    private void SetSpriteForState()
    {
        if (_spriteRenderer == null || _currentStateIndex < 0 || _currentStateIndex >= damageStates.Count)
            return;

        Sprite[] frames = damageStates[_currentStateIndex].frames;
        if (frames != null && frames.Length > 0)
        {
            int frameIndex = Mathf.Clamp(_currentFrameIndex, 0, frames.Length - 1);
            _spriteRenderer.sprite = frames[frameIndex];
        }
    }

    private void Update()
    {
        if (_spriteRenderer == null || _currentStateIndex < 0)
            return;

        Sprite[] frames = damageStates[_currentStateIndex].frames;
        if (frames == null || frames.Length == 0)
            return;

        // Advance timer
        _timer += Time.deltaTime;
        if (_timer >= frameRate)
        {
            _timer = 0f;
            // Loop through frames (assume exactly 3, but handle any count)
            _currentFrameIndex = (_currentFrameIndex + 1) % frames.Length;
            _spriteRenderer.sprite = frames[_currentFrameIndex];
        }
    }

    // Optional: method to manually set animation speed
    public void SetFrameRate(float newRate)
    {
        frameRate = newRate;
    }
}