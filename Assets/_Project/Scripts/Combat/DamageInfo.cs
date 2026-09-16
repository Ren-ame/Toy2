using UnityEngine;

public readonly struct DamageInfo
{
    public readonly float Damage;
    public readonly float PoiseDamage;
    public readonly bool IsParryable;
    public readonly Vector3 HitPos;
    public readonly Vector3 HitDir;
    public readonly GameObject Causer;

    public DamageInfo (float damage, float poiseDamage, bool isParryable, Vector3 hitPos, Vector3 hitDir, GameObject causer)
    {
        Damage = damage;
        PoiseDamage = poiseDamage;
        IsParryable = isParryable;
        HitPos = hitPos;
        HitDir = hitDir;
        Causer = causer;
    }
}
