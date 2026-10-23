using UnityEngine;

public class BossChaseState : BossState
{
    public BossChaseState(BossStateMachine machine) : base(machine)
    {
    }

    private const float MoveSpeed = 3.5f;
    private const float RotateSpeed = 360f;
    private const float StopRange = 2.5f;

    public override void Enter()
    {
        Machine.Animator.CrossFade("Walk", 0.15f);
    }

    public override void Tick(float deltaTime)
    {
        Vector3 dir = Machine.Target.position - Machine.transform.position;
        dir.y = 0f;

        Machine.FaceTarget(RotateSpeed, deltaTime);

        if (dir.sqrMagnitude > StopRange * StopRange)
        {
            Machine.Controller.SimpleMove(dir.normalized * MoveSpeed);
        }

        if (dir.sqrMagnitude <= StopRange * StopRange && Machine.CanAttack == true)
        {
            Machine.ChangeState(new BossReadyState(Machine));
        }
    }
}
