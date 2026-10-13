using UnityEngine;

public class HitboxComponent : MonoBehaviour
{
    private Collider _collider;

    private float _damage;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        Deactivate();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name);


        if (other.gameObject.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            DamageInfo damageInfo = new DamageInfo(_damage, 0, false, Vector3.zero, Vector3.zero, null);
            damageable.TakeDamage(damageInfo);
        }
    }

    public void Activate(float damage)
    {
        _collider.enabled = true;
        _damage = damage;
    }

    public void Deactivate()
    {
        _collider.enabled = false;
    }
}
