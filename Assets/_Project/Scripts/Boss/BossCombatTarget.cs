using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(PoiseComponent))]
public class BossCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;
    private PoiseComponent _poise;
    private BossStateMachine _machine;

    public bool IsAlive => !_health.IsDead;
    public bool IsBroken => _poise.IsBroken;

    [SerializeField] private float _critDamage = 250f;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _poise = GetComponent<PoiseComponent>();
        _machine = GetComponent<BossStateMachine>();
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return DamageResult.Ignored;

        bool isCrit = _machine.IsGroggy && info.IsHeavyAttack;
        float damage = isCrit ? _critDamage : info.Damage;

        _health.Reduce(damage);
        Debug.Log($"[Boss] -{damage} → HP {_health.CurrentHealth}");
        if (IsAlive) _poise.Add(info.PoiseDamage);

        if (isCrit && IsAlive)
            _machine.ChangeState(new BossChaseState(_machine));

        DamageResult result = IsAlive ? DamageResult.Hit : DamageResult.Killed;
        _machine.OnDamaged(result);
        return result;
    }
}
