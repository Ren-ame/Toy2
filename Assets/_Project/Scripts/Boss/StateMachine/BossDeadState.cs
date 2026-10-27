using UnityEngine;

public class BossDeadState : BossState
{
    public BossDeadState(BossStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Machine.Animator.CrossFade("Dead", 0.15f);
    }
}
