using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IGauge
{
    [SerializeField] private float _maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => _maxHealth;
    public float Ratio => CurrentHealth / MaxHealth;
    public float Max => _maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    public event Action<float, float> OnChanged;
    public event Action OnDied;

    private bool _hasDied;

    void Awake()
    {
        CurrentHealth = _maxHealth;
    }

    public void Reduce(float amount)
    {
        if (_hasDied || amount <= 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth - amount, 0f, MaxHealth);
        OnChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0f)
        {
            _hasDied = true;
            OnDied?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (_hasDied || amount <= 0f) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, MaxHealth);
        OnChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void ResetHealth()
    {
        _hasDied = false;
        CurrentHealth = _maxHealth;
        OnChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void SetMaxHealth(float max)
    {
        _maxHealth = max;
        CurrentHealth = max;
        OnChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
