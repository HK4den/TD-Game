using UnityEngine;

public class TowerValueLedger : MonoBehaviour
{
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
        return Mathf.CeilToInt(totalSpent * refundRate);
    }
}