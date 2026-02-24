using UnityEngine;
using UnityEngine.UI;

public class InspectPanelUI : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private RectTransform panelRoot;

    [Header("Panel Positions")]
    [SerializeField] private RectTransform panelRect;              // usually same as panelRoot
    [SerializeField] private Vector2 hiddenAnchoredPos;            // where it sits when nothing selected
    [SerializeField] private Vector2 shownAnchoredPos;             // where it sits when something selected
    [SerializeField] private float moveDuration = 0.15f;

    [Header("Terrain UI")]
    [SerializeField] private Text terrainTypeText;
    [SerializeField] private Text terrainDescText;

    [Header("Tower UI")]
    [SerializeField] private GameObject towerSectionRoot;
    [SerializeField] private Text towerNameText;
    [SerializeField] private Image towerIconImage;

    [Header("Upgrade UI (Keyboard-controlled)")]
    [SerializeField] private GameObject upgradeRoot;       // parent for upgrade UI
    [SerializeField] private Text upgradeAIndicatorText;    // e.g. "> Upgrade A" / "  Upgrade A"
    [SerializeField] private Text upgradeBIndicatorText;    // e.g. "> Upgrade B"
    [SerializeField] private GameObject upgradeBRowRoot;    // hides row if not two-path

    [Header("Terrain Descriptions (optional)")]
    [SerializeField] private TerrainDescriptionsSO terrainDescriptions;

    private GridTile selectedTile;
    private TowerIdentity selectedTower;
    private TowerUpgradeState selectedUpgradeState;

    private bool hasSelection;

    private float moveT;
    private Vector2 moveFrom;
    private Vector2 moveTo;

    private int selectedUpgradeIndex = 0; // 0 = A, 1 = B

    private void Awake()
    {
        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        if (panelRect == null) panelRect = panelRoot;

        // Start "hidden"
        hasSelection = false;
        ForceMoveTo(hiddenAnchoredPos);

        ApplyTerrain(null);
        ApplyTower(null, null);
        ApplyUpgradeSelectionVisuals();
    }

    private void Update()
    {
        // Smooth move between hidden/shown positions
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
        hasSelection = false;

        ApplyTerrain(null);
        ApplyTower(null, null);

        StartMove(hiddenAnchoredPos);
    }

    public void SetSelectedTile(GridTile tile)
    {
        selectedTile = tile;
        selectedTower = null;
        selectedUpgradeState = null;
        hasSelection = tile != null;

        ApplyTerrain(tile);
        ApplyTower(null, null);

        StartMove(hasSelection ? shownAnchoredPos : hiddenAnchoredPos);
    }

    public void SetSelectedTower(TowerIdentity tower, TowerUpgradeState upgradeState, GridTile tileUnderTower)
    {
        selectedTower = tower;
        selectedUpgradeState = upgradeState;
        selectedTile = tileUnderTower;
        hasSelection = tower != null || tileUnderTower != null;

        ApplyTerrain(tileUnderTower);
        ApplyTower(tower, upgradeState);

        StartMove(hasSelection ? shownAnchoredPos : hiddenAnchoredPos);
    }

    // Called by your tool input: R toggles selection
    public void ToggleUpgradeSelection()
    {
        // Only if B exists
        bool twoPaths = selectedUpgradeState != null && selectedUpgradeState.HasTwoPaths;
        if (!twoPaths) { selectedUpgradeIndex = 0; ApplyUpgradeSelectionVisuals(); return; }

        selectedUpgradeIndex = 1 - selectedUpgradeIndex;
        ApplyUpgradeSelectionVisuals();
    }

    public int GetSelectedUpgradeIndex() => selectedUpgradeIndex;

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
            else terrainDescText.text = ""; // your request: blank if no SO
        }
    }

    private void ApplyTower(TowerIdentity tower, TowerUpgradeState upgradeState)
    {
        bool hasTower = tower != null;

        if (towerSectionRoot != null)
            towerSectionRoot.SetActive(hasTower);

        if (!hasTower)
        {
            if (upgradeRoot != null) upgradeRoot.SetActive(false);
            return;
        }

        if (towerNameText != null)
            towerNameText.text = tower.DisplayName;

        if (towerIconImage != null)
        {
            towerIconImage.enabled = tower.Icon != null;
            towerIconImage.sprite = tower.Icon;
        }

        bool canUpgrade = upgradeState != null && upgradeState.CanUpgrade;
        bool twoPaths = upgradeState != null && upgradeState.HasTwoPaths;

        if (upgradeRoot != null)
            upgradeRoot.SetActive(canUpgrade);

        if (upgradeBRowRoot != null)
            upgradeBRowRoot.SetActive(canUpgrade && twoPaths);

        if (!canUpgrade) selectedUpgradeIndex = 0;

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