using UnityEngine;

public class TowerUpgradeState : MonoBehaviour
{
    [Header("Upgrade Flags")]
    [SerializeField] private bool canUpgrade = true;

    [Tooltip("If true, UI shows two upgrade buttons (Path A / Path B).")]
    [SerializeField] private bool hasTwoPaths = false;

    public bool CanUpgrade => canUpgrade;
    public bool HasTwoPaths => hasTwoPaths;

    public void RequestUpgrade(int pathIndex) { /* stub for now */ }

    // Later you can add: current level, max level, costs, etc.
}
