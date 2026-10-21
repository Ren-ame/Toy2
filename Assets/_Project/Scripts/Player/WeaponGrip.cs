using UnityEngine;

/// 무기 소켓(WeaponSocket_R)에 붙인다.
/// 가드할 때만 소켓을 추가로 돌려서 칼을 손 안에서 고쳐 쥔다(역수 등).
public class WeaponGrip : MonoBehaviour
{
    [Tooltip("가드 중 평소 각도에서 추가로 돌릴 각도 (플레이 중 조정 가능)")]
    [SerializeField] private Vector3 _guardEuler = new Vector3(0f, 0f, 180f);
    [Tooltip("고쳐 쥐는 속도 (클수록 빠름)")]
    [SerializeField] private float _blendSpeed = 12f;

    private Quaternion _normalRotation;
    private bool _isGuard;

    private void Awake()
    {
        _normalRotation = transform.localRotation;
    }

    public void SetGuard(bool on)
    {
        _isGuard = on;
    }

    private void Update()
    {
        Quaternion target = _isGuard ? _normalRotation * Quaternion.Euler(_guardEuler) : _normalRotation;
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, _blendSpeed * Time.deltaTime);
    }
}
