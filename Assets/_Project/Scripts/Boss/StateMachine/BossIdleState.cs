using UnityEngine;

public class BossIdleState : BossState
{
    public BossIdleState(BossStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("[Boss] Idle");
    }
}
