using UnityEngine;

[System.Serializable]
public struct EnemyDamageInfo
{
    public float damage;
    public bool ignoreDamageTakenModifiers;
    public bool isRedirectedDamage;
    public bool showDamageNumber;
    public GameObject source;

    public EnemyDamageInfo(
        float damage,
        bool ignoreDamageTakenModifiers = false,
        bool isRedirectedDamage = false,
        bool showDamageNumber = true,
        GameObject source = null)
    {
        this.damage = damage;
        this.ignoreDamageTakenModifiers = ignoreDamageTakenModifiers;
        this.isRedirectedDamage = isRedirectedDamage;
        this.showDamageNumber = showDamageNumber;
        this.source = source;
    }
}

public interface IEnemyIncomingDamageModifier
{
    void ModifyIncomingDamage(EnemyHealth target, ref EnemyDamageInfo damageInfo);
}

public abstract class EnemyDeathBehavior : MonoBehaviour
{
    public virtual float GetRequiredDelay() => 0f;
    public abstract void TriggerDeath(EnemyHealth health);
}