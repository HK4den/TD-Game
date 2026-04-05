using System;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    [SerializeField] private int startingMoney = 200;

    public int Money { get; private set; }

    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        Money = startingMoney;
        Debug.Log($"[Economy] Awake startingMoney={Money} (manager: {name})");
        OnMoneyChanged?.Invoke(Money);
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        AdjustMoneySigned(amount);
    }

    public void AdjustMoneySigned(int amount)
    {
        if (amount == 0)
        {
            OnMoneyChanged?.Invoke(Money);
            return;
        }

        Money += amount;

        if (amount > 0)
            Debug.Log($"[Economy] +{amount} => {Money} (manager: {name})");
        else
            Debug.Log($"[Economy] {amount} => {Money} (manager: {name})");

        OnMoneyChanged?.Invoke(Money);
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0) return true;

        if (Money < amount)
        {
            Debug.Log($"[Economy] FAILED spend {amount} (have {Money}) (manager: {name})");
            return false;
        }

        Money -= amount;
        Debug.Log($"[Economy] -{amount} => {Money} (manager: {name})");
        OnMoneyChanged?.Invoke(Money);
        return true;
    }
}