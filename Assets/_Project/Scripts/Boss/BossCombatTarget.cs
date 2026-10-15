using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class BossCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;
    private BossStateMachine _machine;

    public bool IsAlive => !_health.IsDead;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _machine = GetComponent<BossStateMachine>();
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return DamageResult.Ignored;

        _health.Reduce(info.Damage);
        Debug.Log($"[Boss] -{info.Damage} → HP {_health.CurrentHealth}");

        DamageResult result = IsAlive ? DamageResult.Hit : DamageResult.Killed;
        _machine.OnDamaged(result);
        return result;
    }
}
