using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHP = 20f;
    private float hp;

    private void Awake()
    {
        hp = maxHP;
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        if (hp <= 0f)
            Die();
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
