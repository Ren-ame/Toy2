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
        Machine.Body.material.color = Color.yellow;

        Debug.Log("경직됨");
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

    public override void Exit()
    {
        Machine.Body.material.color = Machine.BaseColor;
    }
}