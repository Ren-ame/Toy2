using UnityEngine;

public class BossIdleState : BossState
{
    public BossIdleState(BossStateMachine machine) : base(machine)
    {
    }

    private const float DetectRange = 10f;

    public override void Enter()
    {
        Machine.Animator.CrossFade("Idle", 0.15f);
    }

    public override void Tick(float deltaTime)
    {
        Machine.Controller.SimpleMove(Vector3.zero);

        Vector3 dir = Machine.Target.position - Machine.transform.position;

        if (dir.sqrMagnitude <= DetectRange * DetectRange)
        {
            Machine.ChangeState(new BossChaseState(Machine));
        }
    }
}
