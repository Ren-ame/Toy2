using UnityEngine;

[RequireComponent(typeof(PlayerInputReader), typeof(PlayerLocomotion))]
public class PlayerStateMachine : MonoBehaviour
{
    private PlayerState _currentState;

    public PlayerInputReader Input { get; private set; }
    public PlayerLocomotion Locomotion { get; private set; }
    public StaminaComponent Stamina { get; private set; }
    public LockOnController LockOn { get; private set; }

    private void Awake()
    {
        Input = GetComponent<PlayerInputReader>();
        Locomotion = GetComponent<PlayerLocomotion>();
        Stamina = GetComponent<StaminaComponent>();
        LockOn = GetComponent<LockOnController>();
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

    private void Update()
    {
        _currentState?.Tick(Time.deltaTime);
    }

}
