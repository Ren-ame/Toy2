using UnityEngine;

public class PlayerLightAttackState : PlayerState
{
    public PlayerLightAttackState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float HoldThreshold = 0.25f;
    private const float InputBuffer = 0.3f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    private bool _comboBuffer;
    private bool _comboWindowOpen;
    private int _comboCount;
    private float[] _comboDamage = { 12f, 12f, 18f };
    private string[] _comboAnimations = { "Attack1", "Attack2", "Attack3" };

    public const float LightStaminaCost = 15f;

    private bool _holdChecked;

    private Transform _aimTarget;

    private float _elapsed;

    public override void Enter()
    {
        base.Enter();
        _comboCount = 0;
        _comboWindowOpen = false;
        _elapsed = 0f;
        _comboBuffer = false;
        _hitboxState = HitboxState.Waiting;
        _holdChecked = false;
        _aimTarget = Machine.LockOn.GetAttackTarget();

        Machine.Input.OnAttackPressed += HandleComboInput;

        PlayAnimation(_comboAnimations[0]);
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= HoldThreshold && _holdChecked == false)
        {
            _holdChecked = true;

            if (Machine.Input.AttackHeld)
            {
                if (Machine.Stamina.CanConsume(PlayerHeavyAttackState.HeavyStaminaCost))
                {
                    Debug.Log("강공");
                    Debug.Log("스태미나: " + Machine.Stamina.CurrentStamina);
                    Machine.ChangeState(new PlayerHeavyAttackState(Machine));
                    return;
                }
                else
                {
                    Debug.Log("Stamina Not Enough");
                    Machine.ChangeState(new PlayerIdleState(Machine));
                    return;
                }
            }
            else
            {
                Debug.Log("약공");
                Machine.Stamina.TryConsume(LightStaminaCost);
                Debug.Log("콤보: " + (_comboCount + 1) + " / 스태미나: " + Machine.Stamina.CurrentStamina);
            }
        }

        if (_comboWindowOpen == true && _comboBuffer == true)
        {
            if (_comboCount < 2)
            {
                if (Machine.Stamina.CanConsume(LightStaminaCost))
                {
                    if (_hitboxState == HitboxState.Active)
                    {
                        Debug.Log("Light Attack Hitbox Off");
                        Machine.Hitbox.Deactivate();
                    }
                    _comboCount += 1;
                    PlayAnimation(_comboAnimations[_comboCount]);
                    _elapsed = 0f;
                    _comboBuffer = false;
                    _comboWindowOpen = false;
                    _hitboxState = HitboxState.Waiting;
                    _holdChecked = false;
                    _aimTarget = Machine.LockOn.GetAttackTarget();
                }
                else
                {
                    Debug.Log("Stamina Not Enough");
                    Machine.ChangeState(new PlayerIdleState(Machine));
                    return;
                }
            }
        }

        if (_hitboxState == HitboxState.Waiting && _aimTarget != null)
        {
            Machine.Locomotion.FaceTowards(_aimTarget.position);
        }

        Machine.Locomotion.StayInPlace(deltaTime);
    }

    public override void OnAnimationEvent(string eventName, int index)
    {
        if (index != _comboCount) return;

        switch (eventName)
        {
            case "HitboxOn":
                if (_hitboxState == HitboxState.Waiting)
                {
                    Machine.Hitbox.Activate(_comboDamage[_comboCount]);
                    _hitboxState = HitboxState.Active;
                }
                break;

            case "HitboxOff":
                if (_hitboxState == HitboxState.Active)
                {
                    Machine.Hitbox.Deactivate();
                    _hitboxState = HitboxState.Finished;
                }
                _comboWindowOpen = true;
                break;

            case "AttackEnd":
                Machine.ChangeState(new PlayerIdleState(Machine));
                break;
        }
    }

    private void HandleComboInput()
    {
        if (_elapsed >= InputBuffer)
        {
            _comboBuffer = true;
            Debug.Log("예약됨");
        }
    }

    public override void Exit()
    {
        Machine.Input.OnAttackPressed -= HandleComboInput;

        if (_hitboxState == HitboxState.Active)
        {
            Debug.Log("Hitbox Off");
            Machine.Hitbox.Deactivate();
        }
    }
}
