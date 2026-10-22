using UnityEngine;

public class BossAnimationEvents : MonoBehaviour
{
    private BossStateMachine _machine;

    private void Awake()
    {
        _machine = GetComponentInParent<BossStateMachine>();    
    }

    public void OnAnimEvent(AnimationEvent evt)
    {
        _machine.HandleAnimationEvent(evt.stringParameter);
    }
}


