using UnityEngine;
using UnityEngine.UI;
using DamageNumbersPro;

public class InspectPanelUI : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private RectTransform panelRoot;

    [Header("Panel Positions")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Vector2 hiddenAnchoredPos;
    [SerializeField] private Vector2 shownAnchoredPos;
    [SerializeField] private float moveDuration = 0.15f;

    [Header("Terrain UI")]
    [SerializeField] private Text terrainTypeText;
    [SerializeField] private Text terrainDescText;

    [Header("Tower UI")]
    [SerializeField] private GameObject towerSectionRoot;
    [SerializeField] private Text towerNameText;
    [SerializeField] private Image towerIconImage;

    [Header("Tower Level UI (NEW)")]
    [SerializeField] private Text towerLevelText; // e.g. "Level: 1"

    [Header("Upgrade UI (Keyboard-controlled)")]
    [SerializeField] private GameObject upgradeRoot;
    [SerializeField] private Text upgradeAIndicatorText;
    [SerializeField] private Text upgradeBIndicatorText;
    [SerializeField] private GameObject upgradeBRowRoot;

    [Header("Upgrade Descriptions + Costs (NEW)")]
    [SerializeField] private Text upgradeADescText;
    [SerializeField] private Text upgradeBDescText;
    [SerializeField] private Text upgradeACostText;
    [SerializeField] private Text upgradeBCostText;

    [Header("Upgrade Available Icon (NEW)")]
    [SerializeField] private GameObject upgradeAvailableIcon; // show if CanUpgrade

    [Header("Insufficient Funds Popup (GUI DNP)")]
    [Tooltip("Damage Numbers Pro GUI prefab (your 'Insufficient Fund Text'). Should already say 'Insufficient Funds' via Prefix/Text in the prefab.")]
    [SerializeField] private DamageNumber insufficientFundsGuiPrefab;

    [Tooltip("Where the popup should appear (screen-space overlay). Put this on the selected upgrade row/button area.")]
    [SerializeField] private RectTransform insufficientFundsAnchorA;
    [SerializeField] private RectTransform insufficientFundsAnchorB;

    [Header("Terrain Descriptions (optional)")]
    [SerializeField] private TerrainDescriptionsSO terrainDescriptions;

    private GridTile selectedTile;
    private TowerIdentity selectedTower;
    private TowerUpgradeState selectedUpgradeState;

    private float moveT;
    private Vector2 moveFrom;
    private Vector2 moveTo;

    private int selectedUpgradeIndex = 0; // 0 = A, 1 = B

    private void Awake()
    {
        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        if (panelRect == null) panelRect = panelRoot;

        ForceMoveTo(hiddenAnchoredPos);

        ApplyTerrain(null);
        ApplyTower(null, null);
        ApplyUpgradeSelectionVisuals();
    }

    private void Update()
    {
        if (panelRect == null) return;

        if (moveT < 1f)
        {
            moveT += (moveDuration <= 0f) ? 1f : (Time.unscaledDeltaTime / moveDuration);
            float t = Mathf.Clamp01(moveT);
            panelRect.anchoredPosition = Vector2.Lerp(moveFrom, moveTo, t);
        }
    }

    // -------------------------
    // Public API used by tools
    // -------------------------

    public void ClearSelection()
    {
        selectedTile = null;
        selectedTower = null;
        selectedUpgradeState = null;

        ApplyTerrain(null);
        ApplyTower(null, null);

        StartMove(hiddenAnchoredPos);
    }

    public void SetSelectedTile(GridTile tile)
    {
        selectedTile = tile;
        selectedTower = null;
        selectedUpgradeState = null;

        ApplyTerrain(tile);
        ApplyTower(null, null);

        StartMove(tile != null ? shownAnchoredPos : hiddenAnchoredPos);
    }

    public void SetSelectedTower(TowerIdentity tower, TowerUpgradeState upgradeState, GridTile tileUnderTower)
    {
        selectedTower = tower;
        selectedUpgradeState = upgradeState;
        selectedTile = tileUnderTower;

        ApplyTerrain(tileUnderTower);
        ApplyTower(tower, upgradeState);

        StartMove((tower != null || tileUnderTower != null) ? shownAnchoredPos : hiddenAnchoredPos);
    }

    public void ToggleUpgradeSelection()
    {
        bool twoPaths = selectedUpgradeState != null && selectedUpgradeState.HasTwoPaths;
        if (!twoPaths)
        {
            selectedUpgradeIndex = 0;
            ApplyUpgradeSelectionVisuals();
            return;
        }

        selectedUpgradeIndex = 1 - selectedUpgradeIndex;
        ApplyUpgradeSelectionVisuals();
    }

    public int GetSelectedUpgradeIndex() => selectedUpgradeIndex;

    public bool TryGetSelectedTower(out TowerIdentity tower, out TowerUpgradeState upgradeState, out GridTile tile)
    {
        tower = selectedTower;
        upgradeState = selectedUpgradeState;
        tile = selectedTile;
        return tower != null && tile != null;
    }

    public void ShowInsufficientFundsPopup()
    {
        if (insufficientFundsGuiPrefab == null) return;

        RectTransform anchor = (selectedUpgradeIndex == 1) ? insufficientFundsAnchorB : insufficientFundsAnchorA;
        if (anchor == null) anchor = insufficientFundsAnchorA != null ? insufficientFundsAnchorA : insufficientFundsAnchorB;
        if (anchor == null) return;

        // Screen Space - Overlay => use screen pixel position
        Vector3 screen = RectTransformUtility.WorldToScreenPoint(null, anchor.position);
        // GUI prefab should be text-only; we spawn "0" just to trigger it.
        insufficientFundsGuiPrefab.SpawnGUI(
    (selectedUpgradeIndex == 1 ? insufficientFundsAnchorB : insufficientFundsAnchorA),
    Vector2.zero,
    0
);
    }

    // -------------------------
    // Internals
    // -------------------------

    private void ApplyTerrain(GridTile tile)
    {
        if (terrainTypeText != null)
            terrainTypeText.text = tile != null ? $"Terrain: {tile.Terrain}" : "Terrain: (none)";

        if (terrainDescText != null)
        {
            if (tile == null) terrainDescText.text = "";
            else if (terrainDescriptions != null) terrainDescText.text = terrainDescriptions.GetDescription(tile.Terrain);
            else terrainDescText.text = "";
        }
    }

    private void ApplyTower(TowerIdentity tower, TowerUpgradeState upgradeState)
    {
        bool hasTower = tower != null;

        if (towerSectionRoot != null)
            towerSectionRoot.SetActive(hasTower);

        if (!hasTower)
        {
            if (towerLevelText != null) towerLevelText.text = "";
            if (upgradeRoot != null) upgradeRoot.SetActive(false);
            if (upgradeAvailableIcon != null) upgradeAvailableIcon.SetActive(false);
            return;
        }

        if (towerNameText != null)
            towerNameText.text = tower.DisplayName;

        if (towerIconImage != null)
        {
            towerIconImage.enabled = tower.Icon != null;
            towerIconImage.sprite = tower.Icon;
        }

        if (towerLevelText != null)
        {
            int lvl = (upgradeState != null) ? upgradeState.DisplayLevel : 1;
            towerLevelText.text = $"Level: {lvl}";
        }

        bool canUpgrade = upgradeState != null && upgradeState.CanUpgrade;
        bool twoPaths = upgradeState != null && upgradeState.HasTwoPaths;

        if (upgradeRoot != null)
            upgradeRoot.SetActive(canUpgrade);

        if (upgradeAvailableIcon != null)
            upgradeAvailableIcon.SetActive(canUpgrade);

        if (upgradeBRowRoot != null)
            upgradeBRowRoot.SetActive(canUpgrade && twoPaths);

        if (!canUpgrade) selectedUpgradeIndex = 0;

        // Descriptions + costs
        if (upgradeADescText != null)
            upgradeADescText.text = canUpgrade && upgradeState != null ? upgradeState.UpgradeADescription : "";

        if (upgradeBDescText != null)
        {
            if (canUpgrade && twoPaths && upgradeState != null)
                upgradeBDescText.text = upgradeState.UpgradeBDescription;
            else
                upgradeBDescText.text = "";
        }

        if (upgradeACostText != null)
            upgradeACostText.text = canUpgrade && upgradeState != null ? $"Cost: {upgradeState.UpgradeACost}" : "";

        if (upgradeBCostText != null)
        {
            if (canUpgrade && twoPaths && upgradeState != null)
                upgradeBCostText.text = $"Cost: {upgradeState.UpgradeBCost}";
            else
                upgradeBCostText.text = "";
        }

        ApplyUpgradeSelectionVisuals();
    }

    private void ApplyUpgradeSelectionVisuals()
    {
        if (upgradeAIndicatorText != null)
            upgradeAIndicatorText.text = (selectedUpgradeIndex == 0) ? "> Upgrade A" : "  Upgrade A";

        if (upgradeBIndicatorText != null)
            upgradeBIndicatorText.text = (selectedUpgradeIndex == 1) ? "> Upgrade B" : "  Upgrade B";
    }

    private void StartMove(Vector2 target)
    {
        if (panelRect == null) return;

        moveFrom = panelRect.anchoredPosition;
        moveTo = target;
        moveT = 0f;
    }

    private void ForceMoveTo(Vector2 pos)
    {
        if (panelRect == null) return;

        panelRect.anchoredPosition = pos;
        moveFrom = pos;
        moveTo = pos;
        moveT = 1f;
    }
}