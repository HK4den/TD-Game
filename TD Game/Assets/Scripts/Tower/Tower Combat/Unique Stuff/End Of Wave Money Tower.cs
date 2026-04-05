using System;
using UnityEngine;

[RequireComponent(typeof(TowerIdentity))]
public class EndOfWaveMoneyTower : MonoBehaviour
{
    [Serializable]
    public class MoneyOutcome
    {
        [Range(0f, 1f)]
        public float chance = 1f;
        public int moneyChange = 0;
    }

    [Header("Refs")]
    [SerializeField] private TowerIdentity towerIdentity;

    [Header("Weighted Outcomes")]
    [SerializeField]
    private MoneyOutcome[] outcomes = new MoneyOutcome[]
    {
        new MoneyOutcome { chance = 1f, moneyChange = 50 }
    };

    public int LastRolledAmount { get; private set; }
    public string FamilyKey
    {
        get
        {
            if (towerIdentity != null && !string.IsNullOrWhiteSpace(towerIdentity.TowerFamilyKey))
                return towerIdentity.TowerFamilyKey.Trim();

            if (towerIdentity != null && !string.IsNullOrWhiteSpace(towerIdentity.DisplayName))
                return towerIdentity.DisplayName.Trim();

            return "Unknown";
        }
    }

    private void Awake()
    {
        if (towerIdentity == null)
            towerIdentity = GetComponent<TowerIdentity>();

        if (towerIdentity == null)
            towerIdentity = GetComponentInChildren<TowerIdentity>();
    }

    public int RollMoneyChange()
    {
        if (outcomes == null || outcomes.Length == 0)
        {
            LastRolledAmount = 0;
            return LastRolledAmount;
        }

        float totalWeight = 0f;
        for (int i = 0; i < outcomes.Length; i++)
        {
            MoneyOutcome outcome = outcomes[i];
            if (outcome == null) continue;
            totalWeight += Mathf.Max(0f, outcome.chance);
        }

        if (totalWeight <= 0f)
        {
            LastRolledAmount = 0;
            return LastRolledAmount;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < outcomes.Length; i++)
        {
            MoneyOutcome outcome = outcomes[i];
            if (outcome == null) continue;

            cumulative += Mathf.Max(0f, outcome.chance);
            if (roll <= cumulative)
            {
                LastRolledAmount = outcome.moneyChange;
                return LastRolledAmount;
            }
        }

        LastRolledAmount = outcomes[outcomes.Length - 1] != null ? outcomes[outcomes.Length - 1].moneyChange : 0;
        return LastRolledAmount;
    }
}