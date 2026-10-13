using UnityEngine;

public class PlayerCombatTarget : MonoBehaviour
{
    private HealthComponent _health;

    public bool IsAlive => !_health.IsDead;



    private void Awake()
    {
        
    }

    private void Update()
    {
        
    }

    public DamageResult TakeDamage(in DamageInfo info)
    {
        return DamageResult.Ignored;
    }
}
