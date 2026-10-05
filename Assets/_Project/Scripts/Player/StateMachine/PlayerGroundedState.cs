using UnityEngine;

public abstract class PlayerGroundedState : PlayerState
{
    protected PlayerGroundedState(PlayerStateMachine machine) : base(machine)
    { 
    }

    public override void Enter()
    {
        Machine.Input.OnDodgePressed += HandleDodge;
        Machine.Input.OnAttackPressed += HandleAttack;
    }

    public override void Exit()
    {
        Machine.Input.OnDodgePressed -= HandleDodge;
        Machine.Input.OnAttackPressed -= HandleAttack;
    }

    private void HandleDodge()
    {
        if (Machine.Stamina.TryConsume(PlayerDodgeState.StaminaCost))
        {
            Machine.ChangeState(new PlayerDodgeState(Machine));
        }
    }

    private void HandleAttack()
    {
        if (Machine.Stamina.CanConsume(PlayerAttackState.LightStaminaCost))
        {
            Machine.ChangeState(new PlayerAttackState(Machine));
        }
    }
}
