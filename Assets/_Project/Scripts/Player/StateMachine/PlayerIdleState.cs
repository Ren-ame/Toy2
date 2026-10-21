using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(PlayerStateMachine machine) : base(machine) 
    {
    }

    public override void Enter()
    {
        base.Enter();
        PlayAnimation("Idle");
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        Vector2 input = Machine.Input.MoveInput;

        if (input.sqrMagnitude > 0.1f)
        {
            Machine.ChangeState(new PlayerMoveState(Machine));
            return;
        }

        Machine.Locomotion.StayInPlace(deltaTime);
    }

}
