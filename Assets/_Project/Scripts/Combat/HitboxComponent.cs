using System;
using System.Collections.Generic;
using UnityEngine;

public class HitboxComponent : MonoBehaviour
{
    private Collider _collider;

    private float _damage;

    private HashSet<IDamageable> _hitList = new HashSet<IDamageable>();

    public event Action<DamageResult> OnHitResult;

    private GameObject _owner;

    private bool _isParryable;

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
                DamageInfo damageInfo = new DamageInfo(_damage, 0, _isParryable, Vector3.zero, Vector3.zero, _owner);
                DamageResult getDamage = target.TakeDamage(damageInfo);
                OnHitResult?.Invoke(getDamage);
            }
        }
    }

    public void Activate(float damage, bool isParryable = true)
    {
        _hitList.Clear();
        _collider.enabled = true;
        _damage = damage;
        _isParryable = isParryable;
    }

    public void Deactivate()
    {
        _collider.enabled = false;
    }

}
