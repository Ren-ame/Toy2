using System.Collections.Generic;
using UnityEngine;

public class HitboxComponent : MonoBehaviour
{
    private Collider _collider;

    private float _damage;

    private HashSet<IDamageable> _hitList = new HashSet<IDamageable>();

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        Deactivate();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);

        if (other.transform.root == transform.root) return;

        if (other.gameObject.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            if (_hitList.Contains(damageable) == false)
            {
                _hitList.Add(damageable); 
                DamageInfo damageInfo = new DamageInfo(_damage, 0, false, Vector3.zero, Vector3.zero, null);
                damageable.TakeDamage(damageInfo);
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
