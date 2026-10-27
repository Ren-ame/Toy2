using System;
using System.Collections.Generic;
using UnityEngine;

public class HitboxComponent : MonoBehaviour
{
    private Collider _collider;

    private float _damage;
    private float _poiseDamage;
    private bool _isParryable;
    private bool _isHeavyAttack;

    private HashSet<IDamageable> _hitList = new HashSet<IDamageable>();

    public event Action<DamageResult> OnHitResult;

    private GameObject _owner;


    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _owner = transform.root.gameObject;
        Deactivate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.gameObject == _owner) return;

        if (other.gameObject.TryGetComponent<IDamageable>(out IDamageable target))
        {
            if (_hitList.Contains(target) == false)
            {
                _hitList.Add(target); 
                DamageInfo damageInfo = new DamageInfo(_damage, _poiseDamage, _isParryable, _isHeavyAttack, Vector3.zero, Vector3.zero, _owner);
                DamageResult getDamage = target.TakeDamage(damageInfo);
                OnHitResult?.Invoke(getDamage);

                float stop = 0f;
                if (getDamage == DamageResult.Parried) stop = 0.12f;
                else if (getDamage == DamageResult.Hit || getDamage == DamageResult.Killed)
                    stop = _isHeavyAttack ? 0.1f : 0.06f;

                if (stop > 0f) HitStop.Instance.Play(stop);
            }
        }
    }

    public void Activate(float damage, bool isParryable = true, float poiseDamage = 0f, bool isHeavyAttack = false)
    {
        _hitList.Clear();
        _collider.enabled = true;
        _damage = damage;
        _poiseDamage = poiseDamage;
        _isParryable = isParryable;
        _isHeavyAttack = isHeavyAttack;
    }

    public void Deactivate()
    {
        _collider.enabled = false;
    }

}
