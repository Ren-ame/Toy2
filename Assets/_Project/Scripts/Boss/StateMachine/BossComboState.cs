using UnityEngine;

public class BossComboState : BossState
{
    public BossComboState(BossStateMachine machine, BossPattern pattern) : base(machine)
    {
        _pattern = pattern;
    }

    private readonly BossPattern _pattern;
    private int _segmentIndex;
    private BossPattern.Segment CurrentSegment => _pattern.Segments[_segmentIndex];

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    private bool _nextParryable;

    private void PlaySegment(int index)
    {
        _segmentIndex = index;
        _hitboxState = HitboxState.Waiting;
        _nextParryable = false;

        Machine.Animator.CrossFadeInFixedTime(CurrentSegment.StateName, 0.15f, 0, CurrentSegment.StartTime);
    }

    public override void Enter()
    {
        PlaySegment(0);
    }

    public override void Tick(float deltaTime)
    {
        if (_hitboxState == HitboxState.Waiting)
        {
            Machine.FaceTarget(CurrentSegment.TrackSpeed, deltaTime);
        }

        Vector3 delta = Machine.RootMotionDelta;
        delta.y = 0f;
        if (deltaTime > 0)
        {
            Machine.Controller.SimpleMove(delta / deltaTime); 
        }
    }

    public override void OnAnimationEvent(string eventName, int index)
    {
        if (index != _segmentIndex)
            return;

        switch (eventName)
        {
            case "GlowOn":
                Machine.SetEyeGlow(true);
                _nextParryable = true;
                break;
            
            case "SwordHitboxOn":
                Machine.SetEyeGlow(false);
                Machine.SwordHitbox.Activate(CurrentSegment.Damage, _nextParryable);
                _hitboxState = HitboxState.Active;
                break;

            case "SwordHitboxOff":
                Machine.SwordHitbox.Deactivate();
                _hitboxState = HitboxState.Finished;
                break;

            case "FootHitboxOn":
                Machine.FootHitbox.Activate(CurrentSegment.Damage, false);
                _hitboxState = HitboxState.Active;
                break;

            case "FootHitboxOff":
                Machine.FootHitbox.Deactivate();
                _hitboxState = HitboxState.Finished;
                break;

            case "AttackEnd":
                if (_segmentIndex + 1 < _pattern.Segments.Length)
                {
                    PlaySegment(_segmentIndex + 1);
                }
                else Machine.ChangeState(new BossChaseState(Machine));
                break;
        }
    }

    public override void Exit()
    {
        if (_hitboxState == HitboxState.Active)
        {
            Machine.SwordHitbox.Deactivate();
            Machine.FootHitbox.Deactivate();
        }

        Machine.SetEyeGlow(false);
    }
}
