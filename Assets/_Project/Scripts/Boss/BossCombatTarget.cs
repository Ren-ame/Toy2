using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class BossCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;

    public bool IsAlive => !_health.IsDead;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return DamageResult.Ignored;

        _health.Reduce(info.Damage);
        Debug.Log($"[Boss] -{info.Damage} → HP {_health.CurrentHealth}");

        DamageResult result = IsAlive ? DamageResult.Hit : DamageResult.Killed;
        return result;
    }
}
