using UnityEngine;

public abstract class BossState
{
    protected readonly BossStateMachine Machine;

    protected BossState(BossStateMachine machine)
    {
        Machine = machine;
    }

    public virtual void Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }

    public virtual void OnAnimationEvent(string eventName) { }
}
