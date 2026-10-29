using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class PlayerCombatTarget : MonoBehaviour, IDamageable
{
    private HealthComponent _health;
    private StaminaComponent _stamina;
    private PlayerStateMachine _machine;

    [SerializeField] private float GuardStaminaCost = 20f;
    [SerializeField] private float GuardDamageRatio = 0.2f;
    [SerializeField] private float GuardAngle = 160f;

    public bool IsAlive => !_health.IsDead;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
        _stamina = GetComponent<StaminaComponent>();
        _machine = GetComponent<PlayerStateMachine>();
        _health.SetMaxHealth(Progression.MaxHp);
        _stamina.SetStats(Progression.MaxStamina, Progression.StaminaRegen);
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        if (!IsAlive) return DamageResult.Ignored;                              // 죽음
        if (_machine.IsInvulnerable) return DamageResult.Ignored;               // 회피
        if (_machine.IsParrying && IsFrontal(info) && info.IsParryable)         // 패리
        {
            _machine.OnDamaged(DamageResult.Parried);
            return DamageResult.Parried;
        }
        if ((_machine.IsGuarding || _machine.IsParrying) && IsFrontal(info))    // 가드
        {
            if (_stamina.TryConsume(GuardStaminaCost) == true)
            {
                _health.Reduce(info.Damage * GuardDamageRatio);

                if (IsAlive == true)
                    return DamageResult.Blocked;
                else
                {
                    _machine.OnDamaged(DamageResult.Killed);
                    return DamageResult.Killed;
                }
            }
            else
            {
                // 가드 브레이크: 스태미나 부족 → 아래 일반 피격으로 진행
            }
        }

        _health.Reduce(info.Damage);

        DamageResult result = IsAlive ? DamageResult.Hit : DamageResult.Killed;
        _machine.OnDamaged(result);
        return result;
    }

    private bool IsFrontal(in DamageInfo info)
    {
        if (info.Causer == null)
            return false;

        Vector3 dir = info.Causer.transform.position - transform.position;
        dir.y = 0f;

        bool isInGuardRange = Vector3.Angle(transform.forward, dir) <= GuardAngle * 0.5f;
        return isInGuardRange;
    }
}
