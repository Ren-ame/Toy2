using UnityEngine;

public class BossIdleState : BossState
{
    public BossIdleState(BossStateMachine machine) : base(machine)
    {
    }

    private const float DetectRange = 10f;

    public override void Enter()
    {
        Debug.Log("[Boss] Idle");
    }

    public override void Tick(float deltaTime)
    {
        Vector3 dir = Machine.Target.position - Machine.transform.position;

        if (dir.sqrMagnitude <= DetectRange * DetectRange)
        {
            Machine.ChangeState(new BossChaseState(Machine));
        }
    }
}
