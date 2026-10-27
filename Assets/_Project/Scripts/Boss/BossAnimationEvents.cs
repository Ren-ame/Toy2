using UnityEngine;

public class BossAnimationEvents : MonoBehaviour
{
    private BossStateMachine _machine;
    private Animator _animator;

    private void Awake()
    {
        _machine = GetComponentInParent<BossStateMachine>();
        _animator = GetComponent<Animator>();
    }

    public void OnAnimEvent(AnimationEvent evt)
    {
        _machine.HandleAnimationEvent(evt.stringParameter, evt.intParameter);
    }

    public void OnAnimatorMove()
    {
        _machine.SetRootMotion(_animator.deltaPosition);
    }
}


