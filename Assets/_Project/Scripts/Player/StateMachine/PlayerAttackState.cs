using UnityEngine;

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(PlayerStateMachine machine) : base(machine)
    {
    }

    private const float CombFinish = 0.6f;
    private const float UseStamina = 0.2f;
    private const float HitboxOn = 0.25f;
    private const float HitboxOff = 0.5f;
    private const float InputBuffer = 0.3f;

    private enum HitboxState { Waiting, Active, Finished }
    private HitboxState _hitboxState;

    private bool ComboBuffer;
    private int ComboCount;
    private int[] ComboDamage = { 12, 12, 18 };
    public float[] StaminaCost = { 15, 30 };

    private float _elapsed;

    public override void Enter()
    {
        ComboCount = 0;
        _elapsed = 0f;
        ComboBuffer = false;
        _hitboxState = HitboxState.Waiting;

        Machine.Input.OnAttackPressed += Input_OnAttackPressed;
    }

    public override void Tick(float deltaTime)
    {
        _elapsed += deltaTime;

        if (_elapsed >= 0.15f)
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

        if (_elapsed >= CombFinish)
        {
            Machine.ChangeState(new PlayerIdleState(Machine));
            return;
        }

        Machine.Locomotion.StayInPlace(deltaTime);
    }

    private void Input_OnAttackPressed()
    {
        ComboBuffer = true;
    }

    public override void Exit()
    {
        Machine.Input.OnAttackPressed -= Input_OnAttackPressed;
    }
}
