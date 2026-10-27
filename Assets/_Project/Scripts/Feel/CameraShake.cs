using UnityEngine;
using Unity.Cinemachine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    [SerializeField] private CinemachineImpulseSource _source;

    private void Awake()
    {
        Instance = this;
    }

    public void Play(float force)
    {
        _source.GenerateImpulseWithForce(force);
    }
}
