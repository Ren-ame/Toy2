using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public class LockOnController : MonoBehaviour
{
    private Transform _target;
    [SerializeField] private Transform _lockOnPivot;
    [SerializeField] private float _lockOnRange = 20f;
    [SerializeField] private LayerMask _targetLayer;

    private Camera _mainCamera;
    [SerializeField] private CinemachineCamera _lockOnCamera;

    private PlayerInputReader _input;
    
    public bool IsLockedOn { get; private set; }

    private void Awake()
    {
        _input = GetComponent<PlayerInputReader>();
        _mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        _input.OnLockOnPressed += ToggleLockOn;
    }

    private void OnDisable()
    {
        _input.OnLockOnPressed -= ToggleLockOn;
    }

    private void ToggleLockOn()
    {
        if (IsLockedOn)
        {
            Unlock();
        }
        else
        {
            TryLockOn();
        }
    }

    private void TryLockOn()
    {
        Transform found = FindNearestTarget();

        if (found == null)
            return;
        _target = found;

        _lockOnCamera.LookAt = _target;

        IsLockedOn = true;

        _lockOnCamera.gameObject.SetActive(true);
    }

    private void Unlock()
    {
        IsLockedOn = false;

        _target = null;

        _lockOnCamera.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (IsLockedOn == false || _target == null)
            return;

        Vector3 toTarget = _target.position - _lockOnPivot.position;
        toTarget.y = 0;
        _lockOnPivot.rotation = Quaternion.LookRotation(toTarget);
    }

    private Transform FindNearestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _lockOnRange, _targetLayer);
        Transform nearest = null;
        float minSqrDistance = float.MaxValue;

        foreach (Collider col in hits)
        {
            if (IsOnScreen(col.transform.position) == false)
                continue;

            float sqrDistance = (col.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance < minSqrDistance)
            {
                minSqrDistance = sqrDistance;
                nearest = col.transform;
            }
        }
        return nearest;
    }

    private bool IsOnScreen(Vector3 worldPosition)
    {
        Vector3 trgPos = _mainCamera.WorldToViewportPoint(worldPosition);

        if (trgPos.z > 0 && trgPos.x >= 0.1f && trgPos.x <= 0.9f && trgPos.y >= 0.1f && trgPos.y <= 0.9f)
            return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, _lockOnRange);
    }
}
