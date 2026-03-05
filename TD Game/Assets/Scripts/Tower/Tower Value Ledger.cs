using UnityEngine;

public class TowerValueLedger : MonoBehaviour
{
    [Header("Selling")]
    [SerializeField] private bool canSell = true;
    public bool CanSell => canSell;

    [SerializeField] private int totalSpent;
    public int TotalSpent => totalSpent;

    public void AddSpend(int amount)
    {
        if (amount <= 0) return;
        totalSpent += amount;
    }

    public void SetTotalSpent(int value)
    {
        totalSpent = Mathf.Max(0, value);
    }

    public int GetRefund(float refundRate)
    {
        refundRate = Mathf.Clamp01(refundRate);
        return Mathf.CeilToInt(totalSpent * refundRate); // round UP
    }
}