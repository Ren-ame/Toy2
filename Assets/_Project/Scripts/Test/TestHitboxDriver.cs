using UnityEngine;

// 테스트 전용: 씬에 둔 칼의 히트박스를 주기적으로 켰다 꺼서 플레이어를 반복해서 때린다.
// 보스가 생기면 삭제.
public class TestHitboxDriver : MonoBehaviour
{
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _activeTime = 0.2f;   // 켜져 있는 시간
    [SerializeField] private float _interval = 1f;       // 꺼져 있는 시간

    private HitboxComponent _hitbox;
    private bool _isActive;
    private float _timer;

    private void Awake()
    {
        _hitbox = GetComponentInChildren<HitboxComponent>();
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (!_isActive && _timer >= _interval)
        {
            _hitbox.Activate(_damage);
            _isActive = true;
            _timer = 0f;
        }
        else if (_isActive && _timer >= _activeTime)
        {
            _hitbox.Deactivate();
            _isActive = false;
            _timer = 0f;
        }
    }
}
