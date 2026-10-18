using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class HitboxComponent : MonoBehaviour
{
    private Collider _collider;

    private float _damage;

    private HashSet<IDamageable> _hitList = new HashSet<IDamageable>();

    private GameObject _owner;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _owner = transform.root.gameObject;
        Deactivate();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);

        if (other.transform.root == transform.root) return;

        if (other.gameObject.TryGetComponent<IDamageable>(out IDamageable target))
        {
            if (_hitList.Contains(target) == false)
            {
                _hitList.Add(target); 
                DamageInfo damageInfo = new DamageInfo(_damage, 0, false, Vector3.zero, Vector3.zero, transform.root.gameObject);
                target.TakeDamage(damageInfo);
            }
        }
    }

    public void Activate(float damage)
    {
        _hitList.Clear();
        _collider.enabled = true;
        _damage = damage;
    }

    public void Deactivate()
    {
        _collider.enabled = false;
    }
}
