using UnityEngine;

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
