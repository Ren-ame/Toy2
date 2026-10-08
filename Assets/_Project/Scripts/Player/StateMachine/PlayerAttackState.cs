using UnityEngine;

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float HoldThreshold = 0.15f;
    private const float UseStamina = 0.2f;
    private const float HitboxOn = 0.25f;
    private const float InputBuffer = 0.3f;
    private const float ComboWindowOpen = 0.4f;
    private const float HitboxOff = 0.5f;
    private const float ComboFinish = 0.6f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    private bool _comboBuffer;
    private int _comboCount;
    private int[] _comboDamage = { 12, 12, 18 };

    private bool _staminaConsumed;
    public const float LightStaminaCost = 15f;
    public const float HeavyStaminaCost = 30f;

    private float _elapsed;

    public override void Enter()
    {
        _comboCount = 0;
        _elapsed = 0f;
        _comboBuffer = false;
        _hitboxState = HitboxState.Waiting;
        _staminaConsumed = false;

        Machine.Input.OnAttackPressed += HandleComboInput;
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= HoldThreshold)
        {

        }

        if (_elapsed >= UseStamina && _staminaConsumed == false)
        {
            _staminaConsumed = true;
            Machine.Stamina.TryConsume(LightStaminaCost);
            Debug.Log(Machine.Stamina.CurrentStamina);
        }

        if (_elapsed >= HitboxOn && _hitboxState == HitboxState.Waiting)
        {
            Debug.Log("Hitbox On");
            _hitboxState = HitboxState.Active;
        }

        if (_elapsed >= ComboWindowOpen && _comboBuffer == true)
        {
            if (Machine.Stamina.CanConsume(LightStaminaCost))
            {
                if (_hitboxState == HitboxState.Active)
                {
                    Debug.Log("Hitbox Off"); 
                }
                _comboCount += 1;
                _elapsed = 0f;
                _comboBuffer = false;
                _hitboxState = HitboxState.Waiting;
                _staminaConsumed = false;
            }
            else
            {
                Debug.Log("Stamina Not Enough");
                Machine.ChangeState(new PlayerIdleState(Machine));
                return; 
            }
        }

        if (_elapsed >= HitboxOff && _hitboxState == HitboxState.Active)
        {
            Debug.Log("Hitbox Off");
            _hitboxState = HitboxState.Finished;
        }

        if (_elapsed >= ComboFinish)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
            return;
        }

        Machine.Locomotion.StayInPlace(deltaTime);
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
        }
    }
}
