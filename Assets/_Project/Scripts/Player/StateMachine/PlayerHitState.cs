using UnityEngine;

public class PlayerHitState : PlayerState
{
    public PlayerHitState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float Duration = 0.5f;
    private const float DodgeCancelTime = 0.3f;
    private const float FirstExtend = 0.2f;

    private float _elapsed;
    private float _duration;
    private float _nextExtend;

    public override void Enter()
    {
        _duration = Duration;
        _nextExtend = FirstExtend;
        Machine.Input.OnDodgePressed += HandleDodge;
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        Machine.Locomotion.StayInPlace(deltaTime);

        if (_elapsed >= _duration)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
        }
    }

    public void Extend()
    {
        _duration += _nextExtend;
        _nextExtend /= 2f;
    }

    private void HandleDodge()
    {
        if (_elapsed >= DodgeCancelTime && Machine.Stamina.TryConsume(PlayerDodgeState.StaminaCost))
        {
            Machine.ChangeState(new PlayerDodgeState(Machine));
        }
    }

    public override void Exit()
    {
        Machine.Input.OnDodgePressed -= HandleDodge;
    }
}
