using UnityEngine;

public class BossStateMachine : MonoBehaviour
{
    private BossState _currentState;

    public Transform Target { get; private set; }
    public CharacterController Controller { get; private set; }
    public HitboxComponent Hitbox { get; private set; }
    public Renderer Body { get; private set; }
    public Color BaseColor { get; private set; }

    [SerializeField] private bool _canAttack = true;
    public bool CanAttack => _canAttack;

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

    public void FaceTarget(float rotateSpeed, float deltaTime)
    {
        Vector3 dir = Target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRot, rotateSpeed * deltaTime);
    }

    private void Update()
    {
        _currentState?.Tick(Time.deltaTime);
    }
}
