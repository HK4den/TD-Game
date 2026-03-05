using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private InspectPanelUI inspectPanel; // optional (auto-inspect after placing)

    private PlayerControls controls;
    private EconomyManager economy;

    [Header("Raycast (CENTER RAY)")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask tileMask;

    [Header("Tower Prefab")]
    [SerializeField] private GameObject towerPrefab;

    [Header("Placement Rules")]
    [SerializeField] private bool towersBlockEnemies = true;
    [SerializeField] private bool enforcePath = true;

    [Header("Path Check")]
    [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);

    [Header("Ghost Preview")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material invalidGhostMaterial;
    [SerializeField] private float ghostYOffset = 0.05f;

    private GameObject ghost;
    private GridTile hoveredTile;

    private Renderer[] ghostRenderers;
    private Material validMatRuntime;
    private Material invalidMatRuntime;

    private GridTile lastTile;
    private int lastSeenPathVersion = -1;
    private bool lastWasValid;

    private void Awake()
    {
        economy = FindFirstObjectByType<EconomyManager>();

        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
        if (inspectPanel == null) inspectPanel = FindFirstObjectByType<InspectPanelUI>();

        controls = new PlayerControls();

        CreateGhost();
    }

    private void OnEnable()
    {
        if (economy != null)
            economy.OnMoneyChanged += HandleMoneyChanged;

        controls.Enable();
        controls.Player.PrimaryClick.performed += OnPrimaryClick;
    }

    private void OnDisable()
    {
        if (economy != null)
            economy.OnMoneyChanged -= HandleMoneyChanged;

        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Disable();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
        {
            if (ghost != null) ghost.SetActive(false);
            hoveredTile = null;
            return;
        }

        UpdateHoverTileCenterRay();
        UpdateGhostVisualsAndPosition();
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;

        // Place-only: if occupied, do nothing (inspector tool handles inspect/upgrade/sell)
        if (hoveredTile == null) return;
        if (hoveredTile.IsOccupied) return;

        TryPlace();
    }

    private void HandleMoneyChanged(int _)
    {
        if (ghost == null || !ghost.activeSelf) return;
        if (hoveredTile == null) return;

        bool nowValid = IsPlacementValid(hoveredTile);
        if (nowValid != lastWasValid)
        {
            lastWasValid = nowValid;
            ApplyGhostMaterial(lastWasValid);
        }
    }

    public void ClearSelectionAndHideGhost()
    {
        hoveredTile = null;
        lastTile = null;
        lastSeenPathVersion = -1;
        lastWasValid = false;

        if (ghost != null)
            ghost.SetActive(false);
    }

    private void CreateGhost()
    {
        if (towerPrefab == null) return;

        ghost = Instantiate(towerPrefab);
        ghost.name = "TowerGhost";
        SetLayerRecursively(ghost, 2); // Ignore Raycast

        foreach (var c in ghost.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>(true))
            mb.enabled = false;

        ghostRenderers = ghost.GetComponentsInChildren<Renderer>(true);

        validMatRuntime = ghostMaterial;
        if (validMatRuntime == null && ghostRenderers != null && ghostRenderers.Length > 0)
            validMatRuntime = ghostRenderers[0].sharedMaterial;

        if (invalidGhostMaterial != null)
        {
            invalidMatRuntime = invalidGhostMaterial;
        }
        else if (validMatRuntime != null)
        {
            invalidMatRuntime = new Material(validMatRuntime);
            if (invalidMatRuntime.HasProperty("_Color"))
                invalidMatRuntime.color = new Color(1f, 0.2f, 0.2f, 0.75f);
        }

        ghost.SetActive(false);
    }

    private void UpdateHoverTileCenterRay()
    {
        hoveredTile = null;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            hoveredTile = hit.collider.GetComponentInParent<GridTile>();
        }
    }

    private void UpdateGhostVisualsAndPosition()
    {
        if (ghost == null) return;

        if (hoveredTile == null || towerPrefab == null)
        {
            ghost.SetActive(false);
            return;
        }

        // No ghost on occupied tile
        if (hoveredTile.IsOccupied)
        {
            ghost.SetActive(false);
            return;
        }

        ghost.SetActive(true);

        Vector3 pos = hoveredTile.transform.position;
        pos.y += ghostYOffset;
        ghost.transform.position = pos;
        ghost.transform.rotation = Quaternion.identity;

        bool tileChanged = hoveredTile != lastTile;
        bool pathChanged = PathChangeBroadcaster.Version != lastSeenPathVersion;

        if (tileChanged || pathChanged)
        {
            lastTile = hoveredTile;
            lastSeenPathVersion = PathChangeBroadcaster.Version;

            lastWasValid = IsPlacementValid(hoveredTile);
            ApplyGhostMaterial(lastWasValid);
        }
    }

    private bool IsPlacementValid(GridTile tile)
    {
        if (tile == null) return false;

        // money check
        TowerCost costComp = towerPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            if (economy.Money < costComp.Cost)
                return false;
        }

        if (!tile.IsBuildable) return false;
        if (tile.Terrain == TerrainType.Blocked) return false;
        if (!tile.CanPlaceTower) return false;

        if (enforcePath && towersBlockEnemies)
        {
            if (!WouldStillHavePathIfPlacedHere(tile))
                return false;
        }

        return true;
    }

    private void ApplyGhostMaterial(bool valid)
    {
        if (ghostRenderers == null || ghostRenderers.Length == 0) return;

        Material m = valid ? validMatRuntime : invalidMatRuntime;
        if (m == null) m = validMatRuntime;
        if (m == null) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
            ghostRenderers[i].sharedMaterial = m;
    }

    private void TryPlace()
    {
        if (hoveredTile == null) return;
        if (towerPrefab == null) return;

        if (hoveredTile.IsOccupied) return;
        if (!IsPlacementValid(hoveredTile)) return;

        int paidCost = 0;

        TowerCost costComp = towerPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            paidCost = Mathf.Max(0, costComp.Cost);
            if (!economy.TrySpendMoney(paidCost))
                return;
        }

        Vector3 pos = hoveredTile.transform.position;
        GameObject placed = Instantiate(towerPrefab, pos, Quaternion.identity);

        // ledger: record actual money paid (supports discounts/free towers)
        TowerValueLedger ledger = placed.GetComponent<TowerValueLedger>();
        if (ledger == null) ledger = placed.AddComponent<TowerValueLedger>();
        ledger.AddSpend(paidCost);

        hoveredTile.SetOccupiedTower(placed);

        if (towersBlockEnemies)
            hoveredTile.SetBlocksEnemies(true);

        PathChangeBroadcaster.Bump();

        // Optional: auto-inspect after placing (only if you want it)
        if (inspectPanel != null)
        {
            var id = placed.GetComponentInChildren<TowerIdentity>();
            var up = placed.GetComponentInChildren<TowerUpgradeState>();
            if (id != null) inspectPanel.SetSelectedTower(id, up, hoveredTile);
            else inspectPanel.SetSelectedTile(hoveredTile);
        }

        lastTile = null; // force refresh next frame
    }

    private bool WouldStillHavePathIfPlacedHere(GridTile tile)
    {
        if (grid == null || pathfinder == null) return true;

        grid.RebuildLookupFromChildren();

        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);
        if (startTile == null || goalTile == null) return true;

        if (tile.Terrain == TerrainType.Blocked) return false;

        bool originalBlocksEnemies = tile.BlocksEnemies;

        tile.SetBlocksEnemies(true);
        var path = pathfinder.FindPathAStar(startTile, goalTile);

        tile.SetBlocksEnemies(originalBlocksEnemies);

        return path != null && path.Count > 0;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}