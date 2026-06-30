using System;
using UnityEngine;

public class HealthComponent
{
    private int _currentHP;
    private int _maxHP;
    private bool _isInvincible;
    private float _invincibilityDuration;
    private float _invincibilityTimer;

    // Events to notify the View/Controller (e.g., UI updates, death sequence)
    public event Action<int> OnDamaged;   // Passes current HP
    public event Action OnDeath;

    public HealthComponent(int maxHP, float invincibilityDuration = 0.5f)
    {
        _maxHP = maxHP;
        _currentHP = maxHP;
        _invincibilityDuration = invincibilityDuration;
        _isInvincible = false;
    }

    public void TakeDamage(int damage)
    {
        if (_isInvincible || _currentHP <= 0) return;

        _currentHP -= damage;
        _currentHP = Mathf.Max(_currentHP, 0);

        OnDamaged?.Invoke(_currentHP);

        if (_currentHP <= 0)
        {
            OnDeath?.Invoke();
        }
        else
        {
            // Activate invincibility frames to prevent chain-hits
            _isInvincible = true;
            _invincibilityTimer = 0f;
        }
    }

    // Call this from MonoBehaviour.Update() to handle invincibility cooldown
    public void UpdateInvincibility(float deltaTime)
    {
        if (!_isInvincible) return;

        _invincibilityTimer += deltaTime;
        if (_invincibilityTimer >= _invincibilityDuration)
        {
            _isInvincible = false;
        }
    }

    public void ResetHealth()
    {
        _currentHP = _maxHP;
        _isInvincible = false;
        _invincibilityTimer = 0f;
        OnDamaged?.Invoke(_currentHP);
    }

    public bool IsDead => _currentHP <= 0;
    public int CurrentHP => _currentHP;
    public int MaxHP => _maxHP;
}
