using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerLocomotion))]
public class PlayerStateMachine : MonoBehaviour
{
    private PlayerState _currentState;

    public PlayerInputReader Input { get; private set; }
    public PlayerLocomotion Locomotion { get; private set; }
    public StaminaComponent Stamina { get; private set; }
    public LockOnController LockOn { get; private set; }
    public HitboxComponent Hitbox { get; private set; }
    public bool IsInvulnerable { get; set; }
    public bool IsParrying { get; set; }
    public bool IsGuarding { get; set; }

    private void Awake()
    {
        Input = GetComponent<PlayerInputReader>();
        Locomotion = GetComponent<PlayerLocomotion>();
        Stamina = GetComponent<StaminaComponent>();
        LockOn = GetComponent<LockOnController>();
        Hitbox = GetComponentInChildren<HitboxComponent>();
    }

    private void Start()
    {
        ChangeState(new PlayerIdleState(this));
    }

    public void ChangeState(PlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void OnDamaged(DamageResult result)
    {
        if (result == DamageResult.Killed)
        {
            ChangeState(new PlayerDeadState(this));
            return;
        }

        if (result == DamageResult.Hit)
        {
            if (_currentState is PlayerHitState hitState)
            {
                hitState.Extend();
            }
            else ChangeState(new PlayerHitState(this));
        }
    }

    private void Update()
    {
        _currentState?.Tick(Time.deltaTime);
    }

}
