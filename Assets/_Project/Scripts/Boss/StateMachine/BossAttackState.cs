using UnityEngine;

public class BossAttackState : BossState
{
    public BossAttackState(BossStateMachine machine) : base(machine)
    {
    }

    private const float RotateSpeed = 180f;

    private const float HitboxOn = 0.6f;
    private const float HitboxOff = 0.8f;
    private const float Finish = 1.6f;
    private const float Damage = 25f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;
 
    private float _elapsed;

    public override void Enter()
    {
        _hitboxState = HitboxState.Waiting;
        _elapsed = 0f;

        Machine.Body.material.color = Color.red;
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        Machine.Controller.SimpleMove(Vector3.zero);

        if (_hitboxState == HitboxState.Waiting)
        {
            Machine.FaceTarget(RotateSpeed, deltaTime);
        }

        if (_elapsed >= HitboxOn && _hitboxState == HitboxState.Waiting)
        {
            Machine.Body.material.color = Machine.BaseColor;
            Machine.Hitbox.Activate(Damage);
            _hitboxState = HitboxState.Active;
        }

        if (_elapsed >= HitboxOff && _hitboxState == HitboxState.Active)
        {
            Machine.Hitbox.Deactivate();
            _hitboxState = HitboxState.Finished;
        }

        if (_elapsed >= Finish && _hitboxState == HitboxState.Finished)
        {
            Machine.ChangeState(new BossChaseState(Machine));
        }
    }

    public override void Exit()
    {
        if (_hitboxState == HitboxState.Active)
        {
            Machine.Hitbox.Deactivate();
        }

        Machine.Body.material.color = Machine.BaseColor;
    }
}
