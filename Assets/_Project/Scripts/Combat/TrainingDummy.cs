using System.Threading;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class TrainingDummy : MonoBehaviour, IDamageable
{
    private HealthComponent _health;

    public bool IsAlive => !_health.IsDead;

    [SerializeField] private float _resetDelay = 3f;
    private float _lastHitTimer;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void Update()
    {
        if (_health.CurrentHealth >= _health.MaxHealth) return;

        if (Time.time - _lastHitTimer >= _resetDelay)
        {
            _health.ResetHealth();
            Debug.Log("[Dummy] HP 초기화");
        }
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        _lastHitTimer = Time.time;

        if (!IsAlive) return DamageResult.Ignored;

        _health.Reduce(info.Damage);
        Debug.Log($"[Dummy] -{info.Damage} → HP {_health.CurrentHealth}");

        return IsAlive ? DamageResult.Hit : DamageResult.Killed;
    }
}