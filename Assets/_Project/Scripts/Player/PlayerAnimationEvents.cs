using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerStateMachine _machine;
    private Animator _animator;

    private void Awake()
    {
        _machine = GetComponentInParent<PlayerStateMachine>();
        _animator = GetComponent<Animator>();
    }

    public void OnAnimEvent(AnimationEvent evt)
    {
        _machine.HandleAnimationEvent(evt.stringParameter, evt.intParameter);
    }

    private void OnAnimatorMove()
    {
        _machine.SetRootMotion(_animator.deltaPosition);
    }
}
