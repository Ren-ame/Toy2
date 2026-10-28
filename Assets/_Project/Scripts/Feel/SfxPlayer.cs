using UnityEngine;
using UnityEngine.Audio;

public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; }

    [SerializeField] private AudioMixerGroup _output;

    private const int PoolSize = 8;
    private AudioSource[] _sources;
    private int _next;

    private void Awake()
    {
        Instance = this;
        _sources = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            _sources[i] = gameObject.AddComponent<AudioSource>();
            _sources[i].outputAudioMixerGroup = _output;
            _sources[i].playOnAwake = false;
            _sources[i].spatialBlend = 0f;
        }
            
    }

    public void Play(AudioClip[] clips, float volume = 1f)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        AudioSource src = _sources[_next];

        src.clip = clip;
        src.volume = volume;
        src.pitch = Random.Range(0.95f, 1.05f);
        src.Play();

        _next = (_next + 1) % PoolSize;
    }
}
