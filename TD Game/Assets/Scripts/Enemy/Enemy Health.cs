using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHP = 20f;
    [SerializeField] private int rewardMoney = 10;

    private float hp;
    private bool died;

    private EconomyManager economy;

    public float CurrentHP => hp;
    public float MaxHP => maxHP;
    public bool IsAlive => !died && hp > 0f;
    public float HealthPercent => maxHP <= 0f ? 0f : hp / maxHP;

    private void Awake()
    {
        hp = Mathf.Max(0.01f, maxHP);
        economy = FindFirstObjectByType<EconomyManager>();
    }

    public void TakeDamage(float amount)
    {
        EnemyDamageTakenController damageTakenController = GetComponent<EnemyDamageTakenController>();
        if (damageTakenController == null)
            damageTakenController = GetComponentInParent<EnemyDamageTakenController>();

        if (damageTakenController != null)
            amount = damageTakenController.ModifyIncomingDamage(amount);

        if (died)
            return;

        if (amount <= 0f)
            return;

        hp -= amount;
        hp = Mathf.Max(0f, hp);

        if (hp <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (died)
            return;

        if (amount <= 0f)
            return;

        hp += amount;
        hp = Mathf.Min(hp, maxHP);
    }

    private void Die()
    {
        if (died)
            return;

        died = true;

        if (economy != null)
            economy.AddMoney(rewardMoney);

        Destroy(gameObject);
    }
}