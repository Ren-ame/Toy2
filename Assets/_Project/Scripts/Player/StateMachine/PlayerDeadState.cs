using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerStateMachine machine) : base(machine)
    {
    }

    public override void Enter()
    {
        Debug.Log("[Player] Dead");
    }

    public override void Tick(float deltaTime)
    {
        Machine.Locomotion.StayInPlace(deltaTime);
    }

    public override void Exit()
    {
        
    }
}
