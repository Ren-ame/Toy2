using System;
using UnityEngine;

public class PoiseComponent : MonoBehaviour, IGauge
{
    [SerializeField] private  float _maxPoise = 100f;
    public float CurrentPoise { get; private set; }
    public float MaxPoise => _maxPoise;
    public float Ratio => CurrentPoise / MaxPoise;
    public bool IsBroken => _hasBroken;

    public event Action<float, float> OnChanged;
    public event Action OnBroken;

    [SerializeField] private float _recoveryDelay = 3f;
    [SerializeField] private float _recoveryPerSecond = 10f;
    private float _lastHitTime = 0f;

    private bool _hasBroken;

    private void Awake()
    {
        CurrentPoise = 0f;
    }

    public void Add(float amount)
    {
        if (_hasBroken || amount <= 0) return;

        CurrentPoise = Mathf.Clamp(CurrentPoise + amount, 0f, MaxPoise);
        _lastHitTime = Time.time;
        OnChanged?.Invoke(CurrentPoise, MaxPoise);

        if (CurrentPoise >= MaxPoise)
        {
            _hasBroken = true;
            OnBroken?.Invoke();
        }
    }

    private void Update()
    {
        float currentTime = Time.time;

        if (!_hasBroken && CurrentPoise > 0 && currentTime - _lastHitTime >= _recoveryDelay)
        {
            CurrentPoise = Mathf.Clamp(CurrentPoise - _recoveryPerSecond * Time.deltaTime, 0f, MaxPoise);
            OnChanged?.Invoke(CurrentPoise, MaxPoise);
        }
    }

    public void ResetPoise()
    {
        _hasBroken = false;
        CurrentPoise = 0f;
        OnChanged?.Invoke(CurrentPoise, MaxPoise);
    }
}
