using UnityEngine;

public class BossBarVisibility : MonoBehaviour
{
    [SerializeField] private Transform _boss;
    [SerializeField] private LockOnController _lockOn;

    private CanvasGroup _group;
    private float _appearSpeed = 2f;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _group.alpha = 0;
    }

    private void Update()
    {
        Vector3 dir = _boss.position - _lockOn.transform.position;

        if (dir.sqrMagnitude <= _lockOn.LockOnRange * _lockOn.LockOnRange)
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, 1, _appearSpeed * Time.deltaTime);
        }
        else
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, 0, _appearSpeed * Time.deltaTime);
        }
    }
}

