using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerStateMachine machine) : base(machine)
    {
    }

    public override void Tick(float deltaTime)
    {
        Vector2 input = Machine.Input.MoveInput;

        if (input.sqrMagnitude <= 0.1f)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
            return;
        }

        Machine.Locomotion.Move(input, deltaTime);
    }
}
