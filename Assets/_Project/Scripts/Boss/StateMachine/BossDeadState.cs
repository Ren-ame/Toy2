using UnityEngine;

public class BossDeadState : BossState
{
    public BossDeadState(BossStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("[Boss] Dead");
    }
}
