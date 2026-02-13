using System;
using UnityEngine;

public class BaseHealth : MonoBehaviour
{
    [SerializeField] private int maxHP = 20;
    [SerializeField] private int hp;

    public int HP => hp;
    public int MaxHP => maxHP;

    public bool IsDead => hp <= 0;

    public event Action<int, int> OnHealthChanged; // (hp, maxHP)
    public event Action OnBaseDestroyed;

    private void Awake()
    {
        hp = maxHP;
        OnHealthChanged?.Invoke(hp, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        hp -= Mathf.Abs(amount);
        if (hp < 0) hp = 0;

        OnHealthChanged?.Invoke(hp, maxHP);

        if (hp <= 0)
        {
            Debug.Log("GAME OVER: Base destroyed.");
            OnBaseDestroyed?.Invoke();
        }
    }

    public void ResetToFull()
    {
        hp = maxHP;
        OnHealthChanged?.Invoke(hp, maxHP);
    }
}
