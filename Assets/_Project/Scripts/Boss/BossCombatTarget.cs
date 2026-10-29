using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(PoiseComponent))]
public class BossCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;
    private PoiseComponent _poise;
    private BossStateMachine _machine;

    public bool IsAlive => !_health.IsDead;
    public bool IsBroken => _poise.IsBroken;

    [SerializeField] private float _critMultiplier = 5f;

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
        float damage = isCrit ? info.Damage * _critMultiplier : info.Damage;

        float before = _health.CurrentHealth;
        _health.Reduce(damage);
        float dealt = before - _health.CurrentHealth;
        Progression.AddCurrency(Mathf.RoundToInt(dealt));

        if (IsAlive) _poise.Add(info.PoiseDamage);

        if (isCrit && IsAlive)
            _machine.ChangeState(new BossRecoverState(_machine, 2f));

        DamageResult result = IsAlive ? DamageResult.Hit : DamageResult.Killed;
        _machine.OnDamaged(result);
        return result;
    }
}
