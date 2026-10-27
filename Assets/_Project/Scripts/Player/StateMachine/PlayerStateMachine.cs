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
    public Animator Animator { get; private set; }
    public WeaponGrip Grip { get; private set; }
    public Vector3 RootMotionDelta { get; private set; }
    public bool IsInvulnerable { get; set; }
    public bool IsParrying { get; set; }
    public bool IsGuarding { get; set; }

    [SerializeField] private float _atk = 10f;
    public float Atk => _atk;

    private void Awake()
    {
        Input = GetComponent<PlayerInputReader>();
        Locomotion = GetComponent<PlayerLocomotion>();
        Stamina = GetComponent<StaminaComponent>();
        LockOn = GetComponent<LockOnController>();
        Hitbox = GetComponentInChildren<HitboxComponent>();
        Animator = GetComponentInChildren<Animator>();
        Grip = GetComponentInChildren<WeaponGrip>();
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

        if (result == DamageResult.Parried)
        {
            ChangeState(new PlayerParryState(this));
        }
    }

    public void HandleAnimationEvent(string eventName, int index)
    {
        _currentState?.OnAnimationEvent(eventName, index);
    }

    public void SetRootMotion(Vector3 delta)
    {
        RootMotionDelta = delta;
    }

    private void Update()
    {
        _currentState?.Tick(Time.deltaTime);
    }

}
