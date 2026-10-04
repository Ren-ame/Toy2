using UnityEngine;

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float ComboFinish = 0.6f;
    private const float HoldThreshold = 0.15f;
    private const float UseStamina = 0.2f;
    private const float HitboxOn = 0.25f;
    private const float HitboxOff = 0.5f;
    private const float InputBuffer = 0.3f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    private bool _comboBuffer;
    private int _comboCount;
    private int[] _comboDamage = { 12, 12, 18 };
    public float[] StaminaCost = { 15, 30 };

    private float _elapsed;

    public override void Enter()
    {
        _comboCount = 0;
        _elapsed = 0f;
        _comboBuffer = false;
        _hitboxState = HitboxState.Waiting;

        Machine.Input.OnAttackPressed += Input_OnAttackPressed;
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= HoldThreshold)
        {

        }

        if (_elapsed >= UseStamina)
        {

        }

        if (_elapsed >= HitboxOn && _hitboxState == HitboxState.Waiting)
        {
            Debug.Log("Hitbox On");
            _hitboxState = HitboxState.Active;
        }

        if (_elapsed >= InputBuffer)
        {
            
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

    private void Input_OnAttackPressed()
    {
        _comboBuffer = true;
    }

    public override void Exit()
    {
        Machine.Input.OnAttackPressed -= Input_OnAttackPressed;
    }
}
