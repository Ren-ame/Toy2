using UnityEngine;

public abstract class PlayerGroundedState : PlayerState
{
    protected PlayerGroundedState(PlayerStateMachine machine) : base(machine)
    { 
    }

    public override void Enter()
    {
        Machine.Input.OnDodgePressed += HandleDodge;
    }

    public override void Exit()
    {
        Machine.Input.OnDodgePressed -= HandleDodge;
    }

    private void HandleDodge()
    {
        if (Machine.Stamina.TryConsume(PlayerDodgeState.StaminaCost))
        {
            Machine.ChangeState(new PlayerDodgeState(Machine));
        }
    }
}
