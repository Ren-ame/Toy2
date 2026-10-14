using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class PlayerCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;
    private PlayerStateMachine _machine;

    public bool IsAlive => !_health.IsDead;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _machine = GetComponent<PlayerStateMachine>();
    }

    private void Update()
    {
        
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return DamageResult.Ignored;
        if (_machine.IsInvulnerable) return DamageResult.Ignored;

        _health.Reduce(info.Damage);
        Debug.Log($"[Player] -{info.Damage} → HP {_health.CurrentHealth}");

        return IsAlive ? DamageResult.Hit : DamageResult.Killed;
    }
}
