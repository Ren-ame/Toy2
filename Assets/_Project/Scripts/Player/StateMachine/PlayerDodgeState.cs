using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    public PlayerDodgeState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float Duration = 0.6f;
    private const float InvulnerableTime = 0.3f;
    private const float Speed = 8f;
    public const float StaminaCost = 25f;

    private Vector3 _direction;
    private float _elapsed;

    public override void Enter()
    {
        base.Enter();

        Vector2 input = Machine.Input.MoveInput;
        _direction = Machine.Locomotion.GetCameraRelativeDirection(input);

        if (input.sqrMagnitude <= 0.1f)
        {
            _direction = Machine.transform.forward;
        }

        _direction = _direction.normalized;

        Machine.transform.rotation = Quaternion.LookRotation(_direction);
        Machine.IsInvulnerable = true;
        _elapsed = 0f;

        PlayAnimation("Dodge");
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= InvulnerableTime)
        {
            Machine.IsInvulnerable = false;
        }

        if (_elapsed >= Duration)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
            return;
        }

        Machine.Locomotion.MoveInDirection(_direction, Speed, deltaTime);
    }

    public override void Exit()
    {
        Machine.IsInvulnerable = false;
    }
}
