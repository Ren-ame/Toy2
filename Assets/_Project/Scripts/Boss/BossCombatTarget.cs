using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(PoiseComponent))]
public class BossCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;
    private PoiseComponent _poise;
    private BossStateMachine _machine;

    public bool IsAlive => !_health.IsDead;
    public bool IsBroken => _poise.IsBroken;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _poise = GetComponent<PoiseComponent>();
        _machine = GetComponent<BossStateMachine>();
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return DamageResult.Ignored;

        _health.Reduce(info.Damage);
        Debug.Log($"[Boss] -{info.Damage} → HP {_health.CurrentHealth}");
        if (IsAlive) _poise.Add(info.PoiseDamage);

        DamageResult result = IsAlive ? DamageResult.Hit : DamageResult.Killed;
        _machine.OnDamaged(result);
        return result;
    }
}
