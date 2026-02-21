using UnityEngine;
using UnityEngine.UI;

public class InspectPanelUI : MonoBehaviour
{
    [Header("Terrain UI")]
    [SerializeField] private Text terrainTypeText;
    [SerializeField] private Text terrainDescText;

    [Header("Tower UI")]
    [SerializeField] private GameObject towerSectionRoot; // parent to hide/show tower UI
    [SerializeField] private Text towerNameText;
    [SerializeField] private Image towerIconImage;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button upgradeButtonA;
    [SerializeField] private Button upgradeButtonB; // optional second path
    [SerializeField] private Text upgradeButtonAText; // optional label
    [SerializeField] private Text upgradeButtonBText; // optional label

    [Header("Terrain Descriptions")]
    [SerializeField] private TerrainDescriptionsSO terrainDescriptions;

    private TowerIdentity selectedTower;
    private TowerUpgradeState selectedUpgradeState;
    private GridTile selectedTile;

    private void Awake()
    {
        // Start hidden/clean
        ApplyTerrain(null);
        ApplyTower(null, null);
    }

    /// <summary>
    /// Select a tile (terrain-only selection).
    /// </summary>
    public void SetSelectedTile(GridTile tile)
    {
        selectedTile = tile;
        selectedTower = null;
        selectedUpgradeState = null;

        ApplyTerrain(tile);
        ApplyTower(null, null);
    }

    /// <summary>
    /// Select a tower (also shows terrain underneath the tower).
    /// </summary>
    public void SetSelectedTower(TowerIdentity tower, TowerUpgradeState upgradeState, GridTile tileUnderTower)
    {
        selectedTower = tower;
        selectedUpgradeState = upgradeState;
        selectedTile = tileUnderTower;

        ApplyTerrain(tileUnderTower);
        ApplyTower(tower, upgradeState);
    }

    private void ApplyTerrain(GridTile tile)
    {
        if (terrainTypeText != null)
        {
            terrainTypeText.text = tile != null ? $"Terrain: {tile.Terrain}" : "Terrain: (none)";
        }

        if (terrainDescText != null)
        {
            if (tile == null)
            {
                terrainDescText.text = "";
            }
            else if (terrainDescriptions != null)
            {
                // You said leave blank for now; you’ll fill in the SO later.
                terrainDescText.text = terrainDescriptions.GetDescription(tile.Terrain);
            }
            else
            {
                // No SO assigned: keep blank (your request)
                terrainDescText.text = "";
            }
        }
    }

    private void ApplyTower(TowerIdentity tower, TowerUpgradeState upgradeState)
    {
        bool hasTower = tower != null;

        if (towerSectionRoot != null)
            towerSectionRoot.SetActive(hasTower);

        if (!hasTower)
            return;

        if (towerNameText != null)
            towerNameText.text = tower.DisplayName;

        if (towerIconImage != null)
        {
            towerIconImage.enabled = tower.Icon != null;
            towerIconImage.sprite = tower.Icon;
        }

        // Upgrade button visibility rules (your spec)
        bool canUpgrade = upgradeState != null && upgradeState.CanUpgrade;
        bool twoPaths = upgradeState != null && upgradeState.HasTwoPaths;

        if (upgradeButtonA != null)
            upgradeButtonA.gameObject.SetActive(canUpgrade);

        if (upgradeButtonB != null)
            upgradeButtonB.gameObject.SetActive(canUpgrade && twoPaths);

        // Optional button labels
        if (upgradeButtonAText != null)
            upgradeButtonAText.text = "Upgrade";

        if (upgradeButtonBText != null)
            upgradeButtonBText.text = "Upgrade (Alt Path)";

        // IMPORTANT: we are NOT implementing upgrade logic yet.
        // Buttons can be wired later to call tower.UpgradePathA / UpgradePathB.
    }
}
