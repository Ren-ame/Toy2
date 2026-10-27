using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float Damage;
    public readonly float PoiseDamage;
    public readonly bool IsParryable;
    public readonly bool IsHeavyAttack;
    public readonly Vector3 HitPos;
    public readonly Vector3 HitDir;
    public readonly GameObject Causer;

    public DamageInfo (float damage, float poiseDamage, bool isParryable, bool isHeavyAttack, Vector3 hitPos, Vector3 hitDir, GameObject causer)
    {
        Damage = damage;
        PoiseDamage = poiseDamage;
        IsParryable = isParryable;
        IsHeavyAttack = isHeavyAttack;
        HitPos = hitPos;
        HitDir = hitDir;
        Causer = causer;

    }
}
