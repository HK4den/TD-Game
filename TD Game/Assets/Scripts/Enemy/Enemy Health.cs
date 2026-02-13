using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHP = 20f;
    [SerializeField] private int rewardMoney = 10;

    private float hp;
    private bool died;

    private EconomyManager economy;

    private void Awake()
    {
        hp = maxHP;
        economy = FindFirstObjectByType<EconomyManager>();
    }

    public void TakeDamage(float amount)
    {
        if (died) return;

        hp -= amount;
        if (hp <= 0f)
            Die();
    }

    private void Die()
    {
        died = true;

        if (economy != null)
            economy.AddMoney(rewardMoney);

        Destroy(gameObject);
    }
}
