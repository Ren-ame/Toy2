using UnityEngine;

public class PlayerHeavyAttackState : PlayerState
{
    public PlayerHeavyAttackState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float UseStamina = 0.05f;
    private const float HitboxOn = 0.5f;
    private const float HitboxOff = 0.75f;
    private const float AttackFinish = 1.05f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    private bool _staminaConsumed;
    public const float HeavyStaminaCost = 30f;

    private const float _damage = 30f;

    private Transform _aimTarget;

    private float _elapsed;

    public override void Enter()
    {
        _hitboxState = HitboxState.Waiting;
        _staminaConsumed = false;
        _aimTarget = Machine.LockOn.GetAttackTarget();
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= UseStamina && _staminaConsumed == false)
        {
            _staminaConsumed = true;
            Machine.Stamina.TryConsume(HeavyStaminaCost);
            Debug.Log("강공격");
        }

        if (_elapsed >= HitboxOn && _hitboxState == HitboxState.Waiting)
        {
            Debug.Log("Heavy Attack Hitbox On");
            Machine.Hitbox.Activate(_damage);
            _hitboxState = HitboxState.Active;
        }

        if (_elapsed >= HitboxOff && _hitboxState == HitboxState.Active)
        {
            Debug.Log("Heavy Attack Hitbox Off");
            Machine.Hitbox.Deactivate();
            _hitboxState = HitboxState.Finished;
        }

        if (_elapsed >= AttackFinish)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
            return;
        }

        if (_hitboxState == HitboxState.Waiting && _aimTarget != null)
        {
            Machine.Locomotion.FaceTowards(_aimTarget.position);
        }

        Machine.Locomotion.StayInPlace(deltaTime);
    }

    public override void Exit()
    {
        if (_hitboxState == HitboxState.Active)
        {
            Debug.Log("Heavy Attack Hitbox Off");
            Machine.Hitbox.Deactivate();
        }
    }
}