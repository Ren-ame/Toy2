using System;
using UnityEngine;

public class StaminaComponent : MonoBehaviour, IGauge
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _regenPerSecond = 35f;
    [SerializeField] private float _regenDelay = 1.0f;

    public float CurrentStamina { get; private set; }
    public float MaxStamina => _maxStamina;
    public float Ratio => CurrentStamina / MaxStamina;

    public event Action<float, float> OnChanged;

    private float _regenTimer;

    void Awake()
    {
        CurrentStamina = _maxStamina;
    }

    private void Update()
    {
        if (_regenTimer > 0f)
        {
            _regenTimer -= Time.deltaTime;
            return;
        }

        if (CurrentStamina >= MaxStamina) return;

        CurrentStamina = Mathf.Clamp(CurrentStamina + _regenPerSecond * Time.deltaTime, 0f, MaxStamina);
        OnChanged?.Invoke(CurrentStamina, MaxStamina);

    }

    public bool TryConsume(float amount)
    {
        if (CurrentStamina < amount) return false;

        CurrentStamina = Mathf.Clamp(CurrentStamina - amount, 0f, MaxStamina);
        _regenTimer = _regenDelay;
        OnChanged?.Invoke(CurrentStamina, MaxStamina);
        return true;

    }

    public void Restore(float amount)
    {
        CurrentStamina = Mathf.Clamp(CurrentStamina + amount, 0f, MaxStamina);
        OnChanged?.Invoke(CurrentStamina, MaxStamina);
    }

    public bool CanConsume(float amount)
    {
        if (CurrentStamina < amount) return false;
        return true;
    }
}
