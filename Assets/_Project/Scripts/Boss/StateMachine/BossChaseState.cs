using UnityEngine;

public class BossChaseState : BossState
{
    public BossChaseState(BossStateMachine machine) : base(machine)
    {
    }

    private const float MoveSpeed = 3.5f;
    private const float RotateSpeed = 360f;

    private bool _isWalking;

    public override void Enter()
    {
        Machine.Animator.CrossFade("Walk", 0.15f);
        _isWalking = true;
    }

    public override void Tick(float deltaTime)
    {
        Vector3 dir = Machine.Target.position - Machine.transform.position;
        dir.y = 0f;

        Machine.FaceTarget(RotateSpeed, deltaTime);

        if (dir.sqrMagnitude > Machine.AttackRange * Machine.AttackRange)
        {
            Machine.Controller.SimpleMove(dir.normalized * MoveSpeed);
            
            if (_isWalking == false)
            {
                Machine.Animator.CrossFade("Walk", 0.15f);
                _isWalking = true;
            }
        }
        else
        {
            Machine.Controller.SimpleMove(Vector3.zero);

            if (Machine.CanAttack == true)
            {
                Machine.ChangeState(new BossReadyState(Machine));
                return;
            }

            if (_isWalking == true)
            {
                Machine.Animator.CrossFade("Idle", 0.15f);
                _isWalking = false;
            }
        }
    }
}
