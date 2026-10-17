using UnityEngine;

public class PlayerGuardState : PlayerState
{
    public PlayerGuardState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float GuardMoveSpeed = 1.5f;
    private const float ParryWindow = 0.2f;
    private bool _windowClosed;

    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0f;
        Debug.Log("패링 창 열림");
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= ParryWindow && _windowClosed == false)
        {
            Debug.Log("가드");
            _windowClosed = true;

        }

        if (_windowClosed == true && Machine.Input.ParryHeld == false)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
            return;
        }

        if (Machine.LockOn.IsLockedOn == true)
        {
            Machine.Locomotion.FaceTowards(Machine.LockOn.GetAttackTarget().position);
        }

        Vector3 dir = Machine.Locomotion.GetCameraRelativeDirection(Machine.Input.MoveInput);
        Machine.Locomotion.MoveInDirection(dir, GuardMoveSpeed, deltaTime);
    }

    public override void Exit()
    {

    }
}