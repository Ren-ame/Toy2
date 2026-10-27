using UnityEngine;

public class BossHandIK : MonoBehaviour
{
    [SerializeField] private Transform _rightHandTarget;
    [SerializeField] private Transform _leftHandTarget;

    private const float BlendSpeed = 2f;

    private Animator _animator;
    private BossStateMachine _machine;

    private float _weight;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _machine = GetComponentInParent<BossStateMachine>();
    }

    private void Update()
    {
        float target = _machine.IsGroggy ? 1f : 0f;
        _weight = Mathf.MoveTowards(_weight, target, BlendSpeed * Time.deltaTime);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _weight);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _weight);
        _animator.SetIKPosition(AvatarIKGoal.RightHand, _rightHandTarget.position);
        _animator.SetIKRotation(AvatarIKGoal.RightHand, _rightHandTarget.rotation);
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _weight);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _weight);
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandTarget.position);
        _animator.SetIKRotation(AvatarIKGoal.LeftHand, _leftHandTarget.rotation);
    }
}
