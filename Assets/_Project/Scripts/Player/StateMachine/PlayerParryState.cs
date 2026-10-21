public class PlayerParryState : PlayerState
{
    public PlayerParryState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float Duration = 0.35f;
    private const float InvulnerableDuration = 0.15f;

    private float _elapsed;

    public override void Enter()
    {
        base.Enter();
        PlayAnimation("Parry", 0.05f);
        Machine.IsInvulnerable = true;
    }

    public override void Tick(float deltaTime)
    {
        Machine.Locomotion.StayInPlace(deltaTime);

        _elapsed += deltaTime;

        if (_elapsed >= InvulnerableDuration && Machine.IsInvulnerable == true)
        {
            Machine.IsInvulnerable = false;
        }

        if (_elapsed >= Duration)
        {
            if (Machine.Input.ParryHeld == true)
            {
                Machine.ChangeState(new PlayerGuardState(Machine));
                return;
            }
            else
            {
                Machine.ChangeState(new PlayerIdleState(Machine));
                return;
            } 
        }
    }

    public override void Exit()
    {
        Machine.IsInvulnerable = false;
    }
}