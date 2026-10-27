using UnityEngine;

[CreateAssetMenu(fileName = "BossPattern", menuName = "Scriptable Objects/BossPattern")]
public class BossPattern : ScriptableObject
{
    [System.Serializable]
    public struct Segment
    {
        public string StateName;
        public float StartTime;
        public float Damage;
        public float TrackSpeed;
    }

    [SerializeField] private Segment[] _segments;
    public Segment[] Segments => _segments;

    [SerializeField] private float _cooldown;
    public float Cooldown => _cooldown;

    [SerializeField] private float _recoveryMin;
    [SerializeField] private float _recoveryMax;
    public float RandomRecovery => Random.Range(_recoveryMin, _recoveryMax);
}
