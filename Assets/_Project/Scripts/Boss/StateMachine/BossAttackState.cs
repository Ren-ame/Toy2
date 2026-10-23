using UnityEngine;

public class BossAttackState : BossState
{
    public BossAttackState(BossStateMachine machine) : base(machine)
    {
    }

    private const float RotateSpeed = 180f;

    private const float Damage = 25f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    public override void Enter()
    {
        _hitboxState = HitboxState.Waiting;

        Machine.Animator.CrossFade("Attack1", 0.15f, 0, 0f);
        Machine.SetEyeGlow(true);
    }

    public override void Tick(float deltaTime)
    {
        Machine.Controller.SimpleMove(Vector3.zero);

        if (_hitboxState == HitboxState.Waiting)
        {
            Machine.FaceTarget(RotateSpeed, deltaTime);
        }
    }

    public override void OnAnimationEvent(string eventName)
    {
        switch (eventName)
        {
            case "HitboxOn":
                if (_hitboxState == HitboxState.Waiting)
                {
                    Machine.SetEyeGlow(false);
                    Machine.Hitbox.Activate(Damage);
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
                Machine.ChangeState(new BossChaseState(Machine));
                break;
        }
    }

    public override void Exit()
    {
        if (_hitboxState == HitboxState.Active)
        {
            Machine.Hitbox.Deactivate();
        }

        Machine.SetEyeGlow(false);
    }
}
