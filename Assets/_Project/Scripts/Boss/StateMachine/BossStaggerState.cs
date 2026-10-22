using UnityEngine;

public class BossStaggerState : BossState
{
    public BossStaggerState(BossStateMachine machine) : base(machine)
    {
    }

    private const float Duration = 0.8f;

    private float _elapsed;

    public override void Enter()
    {
        _elapsed = 0f;

        Debug.Log("경직됨");
        Machine.Animator.CrossFade("Stagger", 0.15f);
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        Machine.Controller.SimpleMove(Vector3.zero);

        if (_elapsed >= Duration)
        {
            Machine.ChangeState(new BossChaseState(Machine));
            return;
        }
    }
}