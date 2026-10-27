using UnityEngine;

public class BossGroggyState : BossState
{
    public BossGroggyState(BossStateMachine machine) : base(machine)
    {
    }

    private const float Duration = 3.5f;
    private const float _animMix = 0.4f;

    private float _elapsed;

    public override void Enter()
    {
        Machine.Animator.CrossFade("Groggy", _animMix);
        Machine.IsGroggy = true;
        _elapsed = 0f;
    }

    public override void Tick(float deltaTime)
    {
        Machine.Controller.SimpleMove(Vector3.zero);

        _elapsed += deltaTime;

        if (_elapsed >= Duration)
        {
            Machine.ChangeState(new BossChaseState(Machine));
            return;
        }
    }

    public override void Exit()
    {
        Machine.Poise.ResetPoise();
        Machine.IsGroggy = false;
    }
}
