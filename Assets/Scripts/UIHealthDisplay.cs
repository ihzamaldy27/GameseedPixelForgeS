using UnityEngine;
using UnityEngine.UI;
using TMPro; // Optional: if using TextMeshPro

public class UIHealthDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text healthText; // or TextMeshProUGUI
    [SerializeField] private Image healthBar; // optional health bar
    [SerializeField] private TextMeshProUGUI healthTextTMP; // if using TMP

    [Header("Display Format")]
    [SerializeField] private string textFormat = "HP: {0}/{1}";
    [SerializeField] private bool showAsPercentage = false;

    private PlayerPlane _player;
    private bool _isSubscribed = false;

    private void Awake()
    {
        // Find the player
        _player = FindFirstObjectByType<PlayerPlane>();
        if (_player == null)
        {
            Debug.LogError("UIHealthDisplay: No PlayerPlane found in scene!");
            return;
        }

        // Subscribe to health change events
        SubscribeToPlayer();
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayer();
    }

    private void SubscribeToPlayer()
    {
        if (_player == null || _isSubscribed) return;

        // Subscribe to the OnDamaged event
        _player.OnDamaged += UpdateHealthDisplay;
        _player.OnHealthRestored += UpdateHealthDisplay; // if you add health restoration

        _isSubscribed = true;

        // Initial update
        UpdateHealthDisplay(_player.CurrentHP);
    }

    private void UnsubscribeFromPlayer()
    {
        if (_player == null || !_isSubscribed) return;

        _player.OnDamaged -= UpdateHealthDisplay;
        _player.OnHealthRestored -= UpdateHealthDisplay;
        _isSubscribed = false;
    }

    private void UpdateHealthDisplay(int currentHP)
    {
        if (_player == null) return;

        int maxHP = _player.MaxHP;

        // Update text
        if (healthText != null)
        {
            if (showAsPercentage)
            {
                float percentage = (float)currentHP / maxHP * 100f;
                healthText.text = $"HP: {percentage:F0}%";
            }
            else
            {
                healthText.text = string.Format(textFormat, currentHP, maxHP);
            }
        }

        // Update TMP text
        if (healthTextTMP != null)
        {
            if (showAsPercentage)
            {
                float percentage = (float)currentHP / maxHP * 100f;
                healthTextTMP.text = $"HP: {percentage:F0}%";
            }
            else
            {
                healthTextTMP.text = string.Format(textFormat, currentHP, maxHP);
            }
        }

        // Update health bar
        if (healthBar != null)
        {
            float fillAmount = (float)currentHP / maxHP;
            healthBar.fillAmount = fillAmount;
        }
    }

    // Public method to manually refresh display
    public void RefreshDisplay()
    {
        if (_player != null)
            UpdateHealthDisplay(_player.CurrentHP);
    }
}