using UnityEngine;

public class BossReadyState : BossState
{
    public BossReadyState(BossStateMachine machine) : base(machine)
    {
    }

    private const float ReadyTime = 0.3f;
    private const float RotateSpeed = 180f;

    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0;
        Machine.Animator.CrossFade("Idle", 0.15f);
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;
        Machine.Controller.SimpleMove(Vector3.zero);
        Machine.FaceTarget(RotateSpeed, deltaTime);

        if (_elapsed >= ReadyTime)
        {
            Machine.ChangeState(new BossAttackState(Machine));
        }
    }
}
