using UnityEngine;

public class BossRecoverState : BossState
{
    public BossRecoverState(BossStateMachine machine, float duration) : base(machine)
    {
        _duration = duration;
    }

    private readonly float _duration;

    private float _elapsed;

    enum MoveMode { Strafe, Back, Forward }
    MoveMode _mode;

    private float _strafeSign;
    private const float StrafeSpeed = 1.5f;
    private const float RotateSpeed = 180f;
    private const float MoveSpeed = 1.2f;
    private const float KeepMin = 3f;
    private const float KeepMax = 6f;
    

    public override void Enter()
    {
        _elapsed = 0f;

        if (Random.value < 0.5f)
        {
            _strafeSign = 1f;
            Machine.Animator.CrossFade("StrafeR", 0.15f);
        }
        else
        {
            Machine.Animator.CrossFade("StrafeL", 0.15f);
            _strafeSign = -1f;
        }
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        Vector3 playerDir = Machine.Target.position - Machine.transform.position;
        playerDir.y = 0f;
        float dist = playerDir.magnitude;
        playerDir = playerDir.normalized;

        Vector3 move;
        if (dist < KeepMin)
        {
            SetMode(MoveMode.Back, "StrafeB");
            move = -playerDir * MoveSpeed;
        }
        else if (dist > KeepMax)
        {
            SetMode(MoveMode.Forward, "StrafeF");
            move = playerDir * MoveSpeed;
        }
        else
        {
            Vector3 side = Vector3.Cross(Vector3.up, playerDir) * _strafeSign;
            SetMode(MoveMode.Strafe, _strafeSign > 0f ? "StrafeR" : "StrafeL");
            move = side * StrafeSpeed;
        }
        Machine.Controller.SimpleMove(move);
        Machine.FaceTarget(RotateSpeed, deltaTime);

        if (_elapsed >= _duration)
        {
            Machine.ChangeState(new BossChaseState(Machine));
            return;
        }
    }

    private void SetMode(MoveMode mode, string anim)
    {
        if (mode != _mode)
        {
            Machine.Animator.CrossFade(anim, 0.15f);
            _mode = mode;
        }
    }

}
