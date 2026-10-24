using UnityEngine;

public class PlayerHeavyAttackState : PlayerState
{
    public PlayerHeavyAttackState(PlayerStateMachine machine) : base(machine)
    {
    }

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    public const float HeavyStaminaCost = 30f;

    private const float DamageRatio = 3f;

    private const int HeavyEventIndex = 3;

    private Transform _aimTarget;

    public override void Enter()
    {
        base.Enter();
        _hitboxState = HitboxState.Waiting;
        _aimTarget = Machine.LockOn.GetAttackTarget();

        PlayAnimation("HeavyAttack");
        Machine.Stamina.TryConsume(HeavyStaminaCost);
    }

    public override void Tick(float deltaTime)
    {
        if (_hitboxState == HitboxState.Waiting && _aimTarget != null)
        {
            Machine.Locomotion.FaceTowards(_aimTarget.position);
        }

        Machine.Locomotion.MoveByDelta(Machine.RootMotionDelta, deltaTime);
    }

    public override void OnAnimationEvent(string eventName, int index)
    {
        if (index != HeavyEventIndex) return;

        switch (eventName)
        {
            case "HitboxOn":
                if (_hitboxState == HitboxState.Waiting)
                {
                    Machine.Hitbox.Activate(Machine.Atk * DamageRatio);
                    _hitboxState = HitboxState.Active;
                }
                break;

            case "HitboxOff":
                if (_hitboxState == HitboxState.Active)
                {
                    Machine.Hitbox.Deactivate();
                    _hitboxState = HitboxState.Finished;
                }
                break;

            case "AttackEnd":
                Machine.ChangeState(new PlayerIdleState(Machine));
                break;
        }
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