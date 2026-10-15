using UnityEngine;

public class BossStateMachine : MonoBehaviour
{
    private BossState _currentState;

    public Transform Target { get; private set; }
    public CharacterController Controller { get; private set; }
    public HitboxComponent Hitbox { get; private set; }
    public Renderer Body { get; private set; }
    public Color BaseColor { get; private set; }

    private void Awake()
    {
        Target = GameObject.FindWithTag("Player").transform;
        Controller = GetComponent<CharacterController>();
        Hitbox = GetComponentInChildren<HitboxComponent>();
        Body = GetComponent<Renderer>();
        BaseColor = Body.material.color;
    }

    private void Start()
    {
        ChangeState(new BossIdleState(this));
    }

    public void ChangeState(BossState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void OnDamaged(DamageResult result)
    {
        if (result == DamageResult.Killed)
        {
            ChangeState(new BossDeadState(this));
        }
    }

    private void Update()
    {
        _currentState?.Tick(Time.deltaTime);
    }
}
