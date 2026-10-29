using System;
using UnityEngine;

public class PlayerInputReader : MonoBehaviour
{
    private PlayerControls _controls;

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool AttackHeld => _controls.Gameplay.Attack.IsPressed();
    public bool ParryHeld => _controls.Gameplay.Parry.IsPressed();

    public event Action OnAttackPressed;
    public event Action OnDodgePressed;
    public event Action OnParryPressed;
    public event Action OnLockOnPressed;
    public event Action OnHealPressed;
    public event Action OnInteractPressed;

    private void Awake()
    {
        _controls = new PlayerControls();

        _controls.Gameplay.Move.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        _controls.Gameplay.Move.canceled  += _   => MoveInput = Vector2.zero;
        _controls.Gameplay.Look.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        _controls.Gameplay.Look.canceled  += _   => LookInput = Vector2.zero;

        _controls.Gameplay.Attack.performed += _ => OnAttackPressed?.Invoke();
        _controls.Gameplay.Dodge.performed += _ => OnDodgePressed?.Invoke();
        _controls.Gameplay.Parry.performed += _ => OnParryPressed?.Invoke();
        _controls.Gameplay.LockOn.performed += _ => OnLockOnPressed?.Invoke();
        _controls.Gameplay.Heal.performed += _ => OnHealPressed?.Invoke();
        _controls.Gameplay.Interact.performed += _ => OnInteractPressed?.Invoke();
    }

    private void OnEnable()
    {
        _controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        _controls.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        _controls.Dispose();
    }

    public void SetGameplayEnabled(bool enabled)
    {
        if (enabled) _controls.Gameplay.Enable();
        else _controls.Gameplay.Disable();
    }
}
