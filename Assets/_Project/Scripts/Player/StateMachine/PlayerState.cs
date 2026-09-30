using UnityEngine;

public abstract class PlayerState
{
    protected readonly PlayerStateMachine Machine;
    protected PlayerState(PlayerStateMachine machine)
    {
        Machine = machine;
    }

    public virtual void Enter() { }

    public virtual void Tick(float deltaTime) { }

    public virtual void Exit() { }
}
