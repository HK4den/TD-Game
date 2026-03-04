using UnityEngine;
using DamageNumbersPro;

public class TowerUpgradeState : MonoBehaviour
{
    [Header("Upgrade Flags")]
    [SerializeField] private bool canUpgrade = true;

    [Tooltip("If true, UI shows two upgrade options (Path A / Path B).")]
    [SerializeField] private bool hasTwoPaths = false;

    [Header("Display Level (manual per prefab)")]
    [SerializeField] private int displayLevel = 1;

    [Header("Upgrade A")]
    [TextArea(2, 6)][SerializeField] private string upgradeADescription;
    [SerializeField] private int upgradeACost = 0;
    [Tooltip("Prefab to swap into when buying Upgrade A.")]
    [SerializeField] private GameObject upgradeAPrefab;

    [Header("Upgrade B (optional)")]
    [TextArea(2, 6)][SerializeField] private string upgradeBDescription;
    [SerializeField] private int upgradeBCost = 0;
    [Tooltip("Prefab to swap into when buying Upgrade B.")]
    [SerializeField] private GameObject upgradeBPrefab;

    [Header("Level Up Popup (Mesh)")]
    [Tooltip("Damage Numbers Pro Mesh prefab (your 'Level Up Text'). Prefix should be 'Level '.")]
    [SerializeField] private DamageNumber levelUpTextPrefab;
    [SerializeField] private float levelUpYOffset = 2.2f;

    public bool CanUpgrade => canUpgrade;
    public bool HasTwoPaths => hasTwoPaths;
    public int DisplayLevel => displayLevel;

    public string UpgradeADescription => upgradeADescription;
    public string UpgradeBDescription => upgradeBDescription;

    public int UpgradeACost => upgradeACost;
    public int UpgradeBCost => upgradeBCost;

    public bool HasUpgradePrefabA => upgradeAPrefab != null;
    public bool HasUpgradePrefabB => upgradeBPrefab != null;

    public int GetCostForPath(int pathIndex) => (pathIndex == 1) ? upgradeBCost : upgradeACost;
    public string GetDescriptionForPath(int pathIndex) => (pathIndex == 1) ? upgradeBDescription : upgradeADescription;
    public GameObject GetPrefabForPath(int pathIndex) => (pathIndex == 1) ? upgradeBPrefab : upgradeAPrefab;

    /// <summary>
    /// Attempts to purchase and apply the selected upgrade by swapping prefabs in-place.
    /// Returns true if upgrade happened.
    /// </summary>
    public bool TryPurchaseUpgrade(
        int pathIndex,
        EconomyManager economy,
        GridTile tileUnderTower,
        out int requiredCost,
        out int newLevel,
        out GameObject newTowerGO)
    {
        newTowerGO = null;
        newLevel = displayLevel;
        requiredCost = 0;

        if (!canUpgrade) return false;
        if (economy == null) return false;
        if (tileUnderTower == null) return false;

        // Validate path existence
        if (pathIndex != 0 && pathIndex != 1) pathIndex = 0;
        if (pathIndex == 1 && !hasTwoPaths) pathIndex = 0;

        GameObject targetPrefab = GetPrefabForPath(pathIndex);
        if (targetPrefab == null) return false;

        requiredCost = GetCostForPath(pathIndex);
        if (requiredCost < 0) requiredCost = 0;

        // Money gate
        if (!economy.TrySpendMoney(requiredCost))
            return false;

        // Capture current ledger total BEFORE swapping
        TowerValueLedger oldLedger = GetComponent<TowerValueLedger>();
        int carrySpent = oldLedger != null ? oldLedger.TotalSpent : 0;

        // Swap prefab in-place WITHOUT clearing tile occupancy.
        Transform oldT = transform;
        Vector3 pos = oldT.position;
        Quaternion rot = oldT.rotation;

        GameObject swapped = Instantiate(targetPrefab, pos, rot);

        // Ensure new tower has ledger, carry over, then add THIS upgrade cost
        TowerValueLedger newLedger = swapped.GetComponent<TowerValueLedger>();
        if (newLedger == null) newLedger = swapped.AddComponent<TowerValueLedger>();

        newLedger.SetTotalSpent(carrySpent);
        newLedger.AddSpend(requiredCost);

        // Keep tile occupied by new tower immediately
        tileUnderTower.SetOccupiedTower(swapped);

        // Destroy old tower last
        Destroy(gameObject);

        // New level comes from new prefab's TowerUpgradeState if present; otherwise +1 fallback.
        TowerUpgradeState newState = swapped.GetComponent<TowerUpgradeState>();
        if (newState != null) newLevel = newState.DisplayLevel;
        else newLevel = displayLevel + 1;

        // Spawn "Level X" popup (Mesh)
        if (levelUpTextPrefab != null)
        {
            levelUpTextPrefab.Spawn(pos + Vector3.up * levelUpYOffset, newLevel);
        }

        newTowerGO = swapped;
        return true;
    }
}