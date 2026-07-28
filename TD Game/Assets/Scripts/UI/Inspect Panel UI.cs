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

    [Header("Tower Level UI")]
    [SerializeField] private Text towerLevelText; // e.g. "Level: 1"

    [Header("Upgrade UI")]
    [SerializeField] private GameObject upgradeRoot;
    [SerializeField] private Text upgradeAIndicatorText;
    [SerializeField] private Text upgradeBIndicatorText;
    [SerializeField] private GameObject upgradeBRowRoot;

    [Header("Upgrade Descriptions + Costs")]
    [SerializeField] private Text upgradeADescText;
    [SerializeField] private Text upgradeBDescText;
    [SerializeField] private Text upgradeACostText;
    [SerializeField] private Text upgradeBCostText;

    [Header("Upgrade Available Icon")]
    [SerializeField] private GameObject upgradeAvailableIcon; // show if CanUpgrade

    [Header("Insufficient Funds Popup (GUI DNP)")]
    [Tooltip("Damage Numbers Pro GUI prefab (your 'Insufficient Fund Text'). Should already say 'Insufficient Funds' via Prefix/Text in the prefab.")]
    [SerializeField] private DamageNumber insufficientFundsGuiPrefab;

    [Tooltip("Where the popup should appear (screen-space overlay). Put this on the selected upgrade row/button area.")]
    [SerializeField] private RectTransform insufficientFundsAnchorA;
    [SerializeField] private RectTransform insufficientFundsAnchorB;

    [Header("Audio Prefabs")]
    [Tooltip("Prefab to spawn when an upgrade succeeds.")]
    [SerializeField] private GameObject upgradeSuccessSfxPrefab;

    [Tooltip("Prefab to spawn when trying to upgrade without enough money.")]
    [SerializeField] private GameObject upgradeFailSfxPrefab;

    [Tooltip("Prefab to spawn when selling a tower.")]
    [SerializeField] private GameObject sellSfxPrefab;

    [Header("Sell UI")]
    [SerializeField] private GameObject sellRoot;   // optional container
    [SerializeField] private Text sellPriceText;    // e.g. "Sell: $123 (Q)" or "Sell: $123 (Down)"
    [SerializeField] private float sellRefundRate = 0.75f;
    [SerializeField] private RectTransform sellFillRect; // your "copied sell button" rect
    [SerializeField] private float sellFillFullWidth = 384.02f; // full width when filled

    [Header("Selection Highlight")]
    [SerializeField] private SelectionHighlightManager selectionHighlightManager;

    [Header("Terrain Descriptions (optional)")]
    [SerializeField] private TerrainDescriptionsSO terrainDescriptions;

    private GridTile selectedTile;
    private TowerIdentity selectedTower;
    private TowerUpgradeState selectedUpgradeState;

    private float moveT;
    private Vector2 moveFrom;
    private Vector2 moveTo;

    private string sellBaseText = "";

    private void Awake()
    {
        if (selectionHighlightManager == null)
            selectionHighlightManager = FindFirstObjectByType<SelectionHighlightManager>();

        if (panelRoot != null) panelRoot.gameObject.SetActive(true);
        if (panelRect == null) panelRect = panelRoot;

        ForceMoveTo(hiddenAnchoredPos);

        ApplyTerrain(null);
        ApplyTower(null, null);
        ApplyUpgradeLabels();
    }

    private void OnEnable()
    {
        GameplayInputPromptModeTracker.EnsureInitialized();
        GameplayInputPromptModeTracker.OnModeChanged += HandlePromptModeChanged;
        RefreshControlPromptTexts();
    }

    private void OnDisable()
    {
        GameplayInputPromptModeTracker.OnModeChanged -= HandlePromptModeChanged;
    }

    private void HandlePromptModeChanged(GameplayInputPromptMode mode)
    {
        RefreshControlPromptTexts();
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

        if (selectionHighlightManager != null)
            selectionHighlightManager.ClearSelection();

        StartMove(hiddenAnchoredPos);
    }

    public bool HasSelection => selectedTile != null || selectedTower != null;

    public Vector3 GetSelectionWorldPos()
    {
        if (selectedTile != null) return selectedTile.transform.position;
        if (selectedTower != null) return selectedTower.transform.position;
        return Vector3.zero;
    }

    public void SetSelectedTile(GridTile tile)
    {
        selectedTile = tile;
        selectedTower = null;
        selectedUpgradeState = null;

        ApplyTerrain(tile);
        ApplyTower(null, null);

        if (selectionHighlightManager != null)
            selectionHighlightManager.SetSelection(null, tile != null ? tile.gameObject : null);

        StartMove(tile != null ? shownAnchoredPos : hiddenAnchoredPos);
    }

    public void SetSelectedTower(TowerIdentity tower, TowerUpgradeState upgradeState, GridTile tileUnderTower)
    {
        selectedTower = tower;
        selectedUpgradeState = upgradeState;
        selectedTile = tileUnderTower;

        ApplyTerrain(tileUnderTower);
        ApplyTower(tower, upgradeState);

        if (selectionHighlightManager != null)
        {
            GameObject towerRoot = tower != null ? tower.gameObject : null;
            GameObject tileRoot = tileUnderTower != null ? tileUnderTower.gameObject : null;
            selectionHighlightManager.SetSelection(towerRoot, tileRoot);
        }

        StartMove((tower != null || tileUnderTower != null) ? shownAnchoredPos : hiddenAnchoredPos);
    }

    public bool TryGetSelectedTower(out TowerIdentity tower, out TowerUpgradeState upgradeState, out GridTile tile)
    {
        tower = selectedTower;
        upgradeState = selectedUpgradeState;
        tile = selectedTile;
        return tower != null && tile != null;
    }

    public void ShowInsufficientFundsPopupForUpgrade1()
    {
        ShowInsufficientFundsPopupAtAnchor(insufficientFundsAnchorA);
        PlayUpgradeFailSfx();
    }

    public void ShowInsufficientFundsPopupForUpgrade2()
    {
        ShowInsufficientFundsPopupAtAnchor(insufficientFundsAnchorB != null ? insufficientFundsAnchorB : insufficientFundsAnchorA);
        PlayUpgradeFailSfx();
    }

    public void ShowInsufficientFundsPopup()
    {
        ShowInsufficientFundsPopupForUpgrade1();
    }

    public void PlayUpgradeSuccessSfx()
    {
        SpawnAudioPrefab(upgradeSuccessSfxPrefab);
    }

    public void PlayUpgradeFailSfx()
    {
        SpawnAudioPrefab(upgradeFailSfxPrefab);
    }

    public void PlaySellSfx()
    {
        SpawnAudioPrefab(sellSfxPrefab);
    }

    public void SetSellHoldFill(float normalized01)
    {
        if (sellFillRect == null) return;

        float t = Mathf.Clamp01(normalized01);

        float w = sellFillFullWidth * t;
        float x = w * 0.5f;

        Vector2 size = sellFillRect.sizeDelta;
        size.x = w;
        sellFillRect.sizeDelta = size;

        Vector2 pos = sellFillRect.anchoredPosition;
        pos.x = x;
        sellFillRect.anchoredPosition = pos;
    }

    public void ClearSellHoldFill()
    {
        SetSellHoldFill(0f);
    }

    public void RestoreSellText()
    {
        if (sellPriceText == null) return;
        sellPriceText.text = sellBaseText;
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

            if (sellRoot != null) sellRoot.SetActive(false);
            if (sellPriceText != null) sellPriceText.text = "";
            sellBaseText = "";

            ClearSellHoldFill();
            ApplyUpgradeLabels();
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

        // ---- Sell UI (show only if sellable) ----
        TowerValueLedger ledger = tower.GetComponentInParent<TowerValueLedger>();
        if (ledger == null) ledger = tower.GetComponentInChildren<TowerValueLedger>();

        bool canSell = ledger != null && ledger.CanSell;

        if (sellRoot != null) sellRoot.SetActive(canSell);

        if (sellPriceText != null)
        {
            if (canSell)
            {
                int refund = ledger.GetRefund(sellRefundRate);
                sellBaseText = BuildSellText(refund);
                sellPriceText.text = sellBaseText;
                ClearSellHoldFill();
            }
            else
            {
                sellBaseText = "";
                sellPriceText.text = "";
                ClearSellHoldFill();
            }
        }

        // ---- Upgrades ----
        bool canUpgrade = upgradeState != null && upgradeState.CanUpgrade;
        bool twoPaths = upgradeState != null && upgradeState.HasTwoPaths;

        if (upgradeRoot != null)
            upgradeRoot.SetActive(canUpgrade);

        if (upgradeAvailableIcon != null)
            upgradeAvailableIcon.SetActive(canUpgrade);

        if (upgradeBRowRoot != null)
            upgradeBRowRoot.SetActive(canUpgrade && twoPaths);

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

        ApplyUpgradeLabels();
    }

    private void ApplyUpgradeLabels()
    {
        if (upgradeAIndicatorText != null)
            upgradeAIndicatorText.text = $"({GetUpgradeAPrompt()}) Upgrade 1";

        if (upgradeBIndicatorText != null)
            upgradeBIndicatorText.text = $"({GetUpgradeBPrompt()}) Upgrade 2";
    }

    private void RefreshControlPromptTexts()
    {
        ApplyUpgradeLabels();
        RefreshSellPromptText();
    }

    private void RefreshSellPromptText()
    {
        if (sellPriceText == null)
            return;

        if (selectedTower == null)
            return;

        TowerValueLedger ledger = selectedTower.GetComponentInParent<TowerValueLedger>();
        if (ledger == null) ledger = selectedTower.GetComponentInChildren<TowerValueLedger>();

        if (ledger == null || !ledger.CanSell)
            return;

        int refund = ledger.GetRefund(sellRefundRate);
        sellBaseText = BuildSellText(refund);
        sellPriceText.text = sellBaseText;
    }

    private string BuildSellText(int refund)
    {
        return $"Sell: ${refund} ({GetSellPrompt()})";
    }

    private string GetUpgradeAPrompt()
    {
        return GameplayInputPromptModeTracker.IsController ? "Up" : "E";
    }

    private string GetUpgradeBPrompt()
    {
        return GameplayInputPromptModeTracker.IsController ? "Right" : "R";
    }

    private string GetSellPrompt()
    {
        return GameplayInputPromptModeTracker.IsController ? "Down" : "Q";
    }

    private void ShowInsufficientFundsPopupAtAnchor(RectTransform anchor)
    {
        if (insufficientFundsGuiPrefab == null) return;
        if (anchor == null) return;

        DamageNumber dn = insufficientFundsGuiPrefab.Spawn(Vector3.zero);
        dn.SetAnchoredPosition(anchor, Vector2.zero);
    }

    private void SpawnAudioPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        Instantiate(prefab, Vector3.zero, Quaternion.identity);
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
