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

    [Header("Sell Settings")]
    [SerializeField] private float sellRefundRate = 0.75f;

    private PlayerControls controls;
    private EconomyManager economy;

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

        // Hook input actions
        controls.Player.PrimaryClick.performed += OnPrimaryClick;
        controls.Player.Upgrade.performed += OnUpgrade;
        controls.Player.SwapUpgrade.performed += OnSwapUpgrade;
        controls.Player.Sell.performed += OnSell;
    }

    private void OnDisable()
    {
        // Unhook input actions
        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Player.Upgrade.performed -= OnUpgrade;
        controls.Player.SwapUpgrade.performed -= OnSwapUpgrade;
        controls.Player.Sell.performed -= OnSell;

        controls.Disable();
    }

    private void Update()
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null || cam == null) return;
        if (!inspectPanel.HasSelection) return;

        Vector3 p = inspectPanel.GetSelectionWorldPos();
        float d = Vector3.Distance(cam.transform.position, p);

        if (d > deselectDistance)
            inspectPanel.ClearSelection();
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (cam == null || inspectPanel == null) return;

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

        if (tile.OccupiedTower != null)
        {
            TowerIdentity id = tile.OccupiedTower.GetComponent<TowerIdentity>();
            if (id == null) id = tile.OccupiedTower.GetComponentInChildren<TowerIdentity>();

            TowerUpgradeState up = tile.OccupiedTower.GetComponent<TowerUpgradeState>();
            if (up == null) up = tile.OccupiedTower.GetComponentInChildren<TowerUpgradeState>();

            if (id != null) inspectPanel.SetSelectedTower(id, up, tile);
            else inspectPanel.SetSelectedTile(tile);
        }
        else
        {
            inspectPanel.SetSelectedTile(tile);
        }
    }

    private void OnSwapUpgrade(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null) return;

        inspectPanel.ToggleUpgradeSelection();
    }

    private void OnUpgrade(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null) return;

        if (!inspectPanel.TryGetSelectedTower(out TowerIdentity tower, out TowerUpgradeState upgradeState, out GridTile tile))
            return;

        if (upgradeState == null || !upgradeState.CanUpgrade)
            return;

        int selectedIndex = inspectPanel.GetSelectedUpgradeIndex();

        bool upgraded = upgradeState.TryPurchaseUpgrade(
            selectedIndex,
            economy,
            tile,
            out int requiredCost,
            out int newLevel,
            out GameObject newTowerGO);

        if (!upgraded)
        {
            if (requiredCost > 0 && economy != null && economy.Money < requiredCost)
                inspectPanel.ShowInsufficientFundsPopup();

            return;
        }

        // Keep selection on the new tower immediately
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

    private void OnSell(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (inspectPanel == null) return;

        if (!inspectPanel.TryGetSelectedTower(out TowerIdentity tower, out TowerUpgradeState upgradeState, out GridTile tile))
            return;

        if (tile == null || tile.OccupiedTower == null) return;

        GameObject towerGO = tile.OccupiedTower;

        // Compute refund from ledger (tracks actual money spent)
        TowerValueLedger ledger = towerGO.GetComponent<TowerValueLedger>();
        int refund = (ledger != null) ? ledger.GetRefund(sellRefundRate) : 0;

        if (economy != null && refund > 0)
            economy.AddMoney(refund);

        // Clear tile occupancy + unblock enemies
        tile.ClearOccupiedTower();
        if (tile.BlocksEnemies) tile.SetBlocksEnemies(false);

        // If selling changes pathing, bump the version
        PathChangeBroadcaster.Bump();

        // Clear UI selection and destroy tower
        inspectPanel.ClearSelection();
        Destroy(towerGO);
    }
}