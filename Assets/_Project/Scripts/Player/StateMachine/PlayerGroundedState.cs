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
        Machine.Input.OnParryPressed += HandleParry;
    }

    public override void Exit()
    {
        Machine.Input.OnDodgePressed -= HandleDodge;
        Machine.Input.OnAttackPressed -= HandleAttack;
        Machine.Input.OnParryPressed -= HandleParry;
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
        if (Machine.Stamina.CanConsume(PlayerLightAttackState.LightStaminaCost))
        {
            Machine.ChangeState(new PlayerLightAttackState(Machine));
        }
    }

    private void HandleParry()
    {
        Machine.ChangeState(new PlayerGuardState(Machine));
    }
}
