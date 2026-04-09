using UnityEngine;
using UnityEngine.InputSystem;

public class TowerInspectorTool : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float deselectDistance = 8f;

    [Header("Raycast")]
    [SerializeField] private LayerMask tileMask;

    [Header("UI")]
    [SerializeField] private InspectPanelUI inspectPanel;

    [Header("Selection Rules")]
    [SerializeField] private bool allowTowerSelection = true;
    [SerializeField] private bool allowEmptyTileSelection = true;

    [Header("Sell Settings")]
    [SerializeField] private float sellRefundRate = 0.75f;
    [SerializeField] private float sellHoldDuration = 0.35f;

    private PlayerControls controls;
    private EconomyManager economy;

    private bool isHoldingSell;
    private float sellHoldTimer;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (inspectPanel == null) inspectPanel = FindFirstObjectByType<InspectPanelUI>();

        economy = FindFirstObjectByType<EconomyManager>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.PrimaryClick.performed += OnPrimaryClick;
        controls.Player.Upgrade.performed += OnUpgrade1;
        controls.Player.SwapUpgrade.performed += OnUpgrade2;
        controls.Player.Sell.started += OnSellStarted;
        controls.Player.Sell.canceled += OnSellCanceled;
    }

    private void OnDisable()
    {
        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Player.Upgrade.performed -= OnUpgrade1;
        controls.Player.SwapUpgrade.performed -= OnUpgrade2;
        controls.Player.Sell.started -= OnSellStarted;
        controls.Player.Sell.canceled -= OnSellCanceled;

        controls.Disable();
    }

    public void SetSelectionPermissions(bool allowTowers, bool allowEmptyTiles)
    {
        allowTowerSelection = allowTowers;
        allowEmptyTileSelection = allowEmptyTiles;
    }

    private void Update()
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null || cam == null) return;

        // Auto-deselect if too far
        if (inspectPanel.HasSelection)
        {
            Vector3 p = inspectPanel.GetSelectionWorldPos();
            float d = Vector3.Distance(cam.transform.position, p);
            if (d > deselectDistance)
            {
                StopSellHoldUI();
                inspectPanel.ClearSelection();
                return;
            }
        }

        // Hold-to-sell fill
        if (isHoldingSell)
        {
            if (!inspectPanel.TryGetSelectedTower(out _, out _, out GridTile tile) || tile == null || tile.OccupiedTower == null)
            {
                StopSellHoldUI();
                return;
            }

            TowerValueLedger ledger = tile.OccupiedTower.GetComponent<TowerValueLedger>();
            if (ledger == null || !ledger.CanSell)
            {
                StopSellHoldUI();
                return;
            }

            sellHoldTimer += Time.unscaledDeltaTime;

            float t = (sellHoldDuration <= 0f) ? 1f : Mathf.Clamp01(sellHoldTimer / sellHoldDuration);
            inspectPanel.SetSellHoldFill(t);

            if (sellHoldTimer >= sellHoldDuration)
            {
                PerformSell(tile, ledger);
                StopSellHoldUI();
            }
        }
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (cam == null || inspectPanel == null) return;

        StopSellHoldUI();

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            inspectPanel.ClearSelection();
            return;
        }

        GridTile tile = hit.collider.GetComponentInParent<GridTile>();
        if (tile == null)
        {
            inspectPanel.ClearSelection();
            return;
        }

        // Tower selection
        if (tile.OccupiedTower != null)
        {
            if (!allowTowerSelection)
                return;

            TowerIdentity id = tile.OccupiedTower.GetComponent<TowerIdentity>();
            if (id == null) id = tile.OccupiedTower.GetComponentInChildren<TowerIdentity>();

            TowerUpgradeState up = tile.OccupiedTower.GetComponent<TowerUpgradeState>();
            if (up == null) up = tile.OccupiedTower.GetComponentInChildren<TowerUpgradeState>();

            if (id != null) inspectPanel.SetSelectedTower(id, up, tile);
            else inspectPanel.SetSelectedTile(tile);

            return;
        }

        // Empty tile selection
        if (allowEmptyTileSelection)
        {
            inspectPanel.SetSelectedTile(tile);
        }
        else
        {
            inspectPanel.ClearSelection();
        }
    }

    private void OnUpgrade1(InputAction.CallbackContext ctx)
    {
        TryUpgrade(0);
    }

    private void OnUpgrade2(InputAction.CallbackContext ctx)
    {
        TryUpgrade(1);
    }

    private void TryUpgrade(int upgradeIndex)
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null) return;

        StopSellHoldUI();

        if (!inspectPanel.TryGetSelectedTower(out TowerIdentity tower, out TowerUpgradeState upgradeState, out GridTile tile))
            return;

        if (tower == null || tile == null || upgradeState == null || !upgradeState.CanUpgrade)
            return;

        bool upgraded = upgradeState.TryPurchaseUpgrade(
            upgradeIndex,
            economy,
            tile,
            out int requiredCost,
            out int newLevel,
            out GameObject newTowerGO);

        if (!upgraded)
        {
            bool notEnoughMoney = requiredCost > 0 && economy != null && economy.Money < requiredCost;
            if (notEnoughMoney)
            {
                if (upgradeIndex == 0)
                    inspectPanel.ShowInsufficientFundsPopupForUpgrade1();
                else
                    inspectPanel.ShowInsufficientFundsPopupForUpgrade2();
            }

            return;
        }

        inspectPanel.PlayUpgradeSuccessSfx();

        if (newTowerGO != null)
        {
            TowerIdentity newId = newTowerGO.GetComponent<TowerIdentity>();
            if (newId == null) newId = newTowerGO.GetComponentInChildren<TowerIdentity>();

            TowerUpgradeState newUp = newTowerGO.GetComponent<TowerUpgradeState>();
            if (newUp == null) newUp = newTowerGO.GetComponentInChildren<TowerUpgradeState>();

            if (newId != null) inspectPanel.SetSelectedTower(newId, newUp, tile);
            else inspectPanel.SetSelectedTile(tile);
        }
    }

    private void OnSellStarted(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null) return;

        if (!inspectPanel.TryGetSelectedTower(out _, out _, out GridTile tile))
            return;

        if (tile == null || tile.OccupiedTower == null)
            return;

        TowerValueLedger ledger = tile.OccupiedTower.GetComponent<TowerValueLedger>();
        if (ledger == null || !ledger.CanSell)
            return;

        isHoldingSell = true;
        sellHoldTimer = 0f;
        inspectPanel.SetSellHoldFill(0f);
    }

    private void OnSellCanceled(InputAction.CallbackContext ctx)
    {
        StopSellHoldUI();
    }

    private void StopSellHoldUI()
    {
        if (!isHoldingSell) return;

        isHoldingSell = false;
        sellHoldTimer = 0f;

        if (inspectPanel != null)
        {
            inspectPanel.ClearSellHoldFill();
            inspectPanel.RestoreSellText();
        }
    }

    private void PerformSell(GridTile tile, TowerValueLedger ledger)
    {
        if (tile == null || tile.OccupiedTower == null) return;
        if (ledger == null || !ledger.CanSell) return;

        GameObject towerGO = tile.OccupiedTower;

        int refund = ledger.GetRefund(sellRefundRate);
        if (economy != null && refund > 0)
            economy.AddMoney(refund);

        if (inspectPanel != null)
            inspectPanel.PlaySellSfx();

        tile.ClearOccupiedTower();

        if (tile.BlocksEnemies)
            tile.SetBlocksEnemies(false);

        PathChangeBroadcaster.Bump();

        if (inspectPanel != null)
        {
            inspectPanel.ClearSellHoldFill();
            inspectPanel.ClearSelection();
        }

        Destroy(towerGO);
    }
}