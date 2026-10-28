using UnityEngine;

public class HitboxSfx : MonoBehaviour
{
    [SerializeField] private AudioClip[] _swing;
    [SerializeField] private AudioClip[] _hit;
    [SerializeField] private AudioClip[] _parried;
    [SerializeField] private AudioClip[] _blocked;
    [SerializeField] private AudioSource _source;

    private HitboxComponent _hitbox;

    private void Awake()
    {
        _hitbox = GetComponent<HitboxComponent>();
    }

    private void OnEnable()
    {
        _hitbox.OnActivated += HandleActivated;
        _hitbox.OnHitResult += HandleHitResult;
    }

    private void OnDisable()
    {
        _hitbox.OnActivated -= HandleActivated;
        _hitbox.OnHitResult -= HandleHitResult;
    }

    private void HandleActivated()
    {
        Play(_swing);
    }

    private void HandleHitResult(DamageResult result)
    {
        switch (result)
        {
            case DamageResult.Hit:
            case DamageResult.Killed:
                Play(_hit);
                break;
         
            case DamageResult.Parried:
                Play(_parried);
                break;
            
            case DamageResult.Blocked:
                Play(_blocked);
                break;
        }
    }

    private void Play(AudioClip[] clips)
    {
        if (_source == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        
        _source.pitch = Random.Range(0.95f, 1.05f);
        _source.PlayOneShot(clip);
    }
}

