using System.Collections.Generic;
using UnityEngine;

public class BossStateMachine : MonoBehaviour
{
    private BossState _currentState;

    public Transform Target { get; private set; }
    public CharacterController Controller { get; private set; }
    public Animator Animator { get; private set; }
    public Vector3 RootMotionDelta { get; private set; }
    public PoiseComponent Poise { get; private set; }
    public bool IsGroggy { get; set; }

    [SerializeField] private HitboxComponent _swordHitbox;
    public HitboxComponent SwordHitbox => _swordHitbox;
    [SerializeField] private HitboxComponent _footHitbox;
    public HitboxComponent FootHitbox => _footHitbox;

    [SerializeField] private Renderer _headRenderer;
    private Material _eyeMaterial;

    [SerializeField] private bool _canAttack = true;
    public bool CanAttack => _canAttack;

    [SerializeField] private Light _eyeLight;

    [SerializeField] private BossPattern[] _patterns;
    public BossPattern[] Patterns => _patterns;

    [SerializeField] private float[] _readyTime;

    [SerializeField] private float _attackRange = 5f;
    public float AttackRange => _attackRange;

    [SerializeField] private float _parryPoiseDamage = 30f;

    [SerializeField] private TrailRenderer _swordTrail;

    private void Awake()
    {
        Target = GameObject.FindWithTag("Player").transform;
        Controller = GetComponent<CharacterController>();
        Animator = GetComponentInChildren<Animator>();
        _eyeMaterial = _headRenderer.material;
        _readyTime = new float[_patterns.Length];
        Poise = GetComponent<PoiseComponent>();
        SetEyeGlow(false);
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

    private void OnEnable()
    {
        SwordHitbox.OnHitResult += HandleHitResult;
        Poise.OnBroken += HandlePoiseBroken;
    }

    private void OnDisable()
    {
        SwordHitbox.OnHitResult -= HandleHitResult;
        Poise.OnBroken -= HandlePoiseBroken;
    }

    private void HandleHitResult(DamageResult result)
    {
        if (result == DamageResult.Parried)
        {
            Poise.Add(_parryPoiseDamage);
            if (Poise.IsBroken)
                return;
            ChangeState(new BossStaggerState(this));
        }
    }

    private void HandlePoiseBroken()
    {
        ChangeState(new BossGroggyState(this));
    }

    public void HandleAnimationEvent(string eventName, int index)
    {
        _currentState?.OnAnimationEvent(eventName, index);
    }

    public void SetEyeGlow(bool on)
    {
        _eyeMaterial.SetFloat("_Emission", on ? 1f : 0f);
        _eyeLight.enabled = on;
    }

    public void SetRootMotion(Vector3 delta)
    {
        RootMotionDelta = delta;
    }

    public BossPattern PickPattern()
    {
        List<int> candidates = new List<int>();

        for (int i = 0; i < _patterns.Length; i++)
        {
            if (Time.time >= _readyTime[i])
            {
                candidates.Add(i);
            }
        }

        int pick = candidates[Random.Range(0, candidates.Count)];
        _readyTime[pick] = Time.time + _patterns[pick].Cooldown;
        return _patterns[pick];
    }

    public void SetSwordTrail(bool on)
    {
        _swordTrail.emitting = on;
    }
}
