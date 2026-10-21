using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    private PlayerStateMachine _machine;

    private void Awake()
    {
        _machine = GetComponentInParent<PlayerStateMachine>();
    }

    public void OnAnimEvent(AnimationEvent evt)
    {
        _machine.HandleAnimationEvent(evt.stringParameter, evt.intParameter);
    }
}
