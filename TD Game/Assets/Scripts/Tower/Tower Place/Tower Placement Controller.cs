using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private EconomyManager economy;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private LayerMask tileMask;
    [SerializeField] private LayerMask towerMask;

    [Header("Build Menu / Tower Options")]
    [Tooltip("List of tower prefabs the player can choose from.")]
    [SerializeField] private GameObject[] towerPrefabs;

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

    [Header("UI")]
    [SerializeField] private TowerInspectPanel inspectPanel;

    private PlayerControls controls;

    // Runtime selection
    private GameObject selectedTowerPrefab;
    private int selectedIndex = -1;

    // Ghost
    private GameObject ghost;
    private Renderer[] ghostRenderers;
    private Material validMatRuntime;
    private Material invalidMatRuntime;

    // Hover cache
    private GridTile hoveredTile;
    private GridTile lastTile;
    private int lastSeenPathVersion = -1;
    private bool lastWasValid;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
        if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
        if (inspectPanel == null) inspectPanel = FindFirstObjectByType<TowerInspectPanel>();

        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.PrimaryClick.performed += OnPrimaryClick;
        controls.Player.SecondaryClick.performed += OnSecondaryClick;

        // When equipping placement staff: nothing selected
        ClearSelectionAndHideGhost();
    }

    private void OnDisable()
    {
        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Player.SecondaryClick.performed -= OnSecondaryClick;

        controls.Disable();

        ClearSelectionAndHideGhost();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
        {
            if (ghost != null) ghost.SetActive(false);
            return;
        }

        UpdateHoverTileCenterScreen();
        UpdateGhostVisualsAndPosition();
    }

    // Called by ToolHotbar (or other tool switching)
    public void ClearSelectionAndHideGhost()
    {
        selectedTowerPrefab = null;
        selectedIndex = -1;

        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }

        hoveredTile = null;
        lastTile = null;
    }

    private void OnSecondaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;

        // Placeholder until you make the real right-click menu UI:
        // Right click cycles towers.
        if (towerPrefabs == null || towerPrefabs.Length == 0) return;

        int next = selectedIndex + 1;
        if (next >= towerPrefabs.Length) next = 0;

        SelectTower(next);
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;

        // If aiming at a tower -> open inspect/upgrade panel
        Tower tower = RaycastTowerCenterScreen();
        if (tower != null)
        {
            if (inspectPanel != null)
                inspectPanel.Toggle(tower);
            return;
        }

        // Otherwise try place (only if a tower is selected)
        if (selectedTowerPrefab == null) return;

        TryPlace();
    }

    private void SelectTower(int index)
    {
        if (towerPrefabs == null || index < 0 || index >= towerPrefabs.Length) return;

        selectedIndex = index;
        selectedTowerPrefab = towerPrefabs[index];

        RecreateGhostForSelected();
    }

    private void RecreateGhostForSelected()
    {
        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }

        if (selectedTowerPrefab == null) return;

        ghost = Instantiate(selectedTowerPrefab);
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

    private void UpdateHoverTileCenterScreen()
    {
        hoveredTile = null;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            hoveredTile = hit.collider.GetComponent<GridTile>();
        }
    }

    private void UpdateGhostVisualsAndPosition()
    {
        if (ghost == null) return;

        if (hoveredTile == null)
        {
            ghost.SetActive(false);
            return;
        }

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
        if (selectedTowerPrefab == null) return false;
        if (tile == null) return false;

        // terrain that doesn't allow placement -> invalid
        if (!tile.IsBuildable) return false;
        if (tile.Terrain == TerrainType.Blocked) return false;
        if (!tile.CanPlaceTower) return false;

        // money check
        TowerCost costComp = selectedTowerPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            if (economy.Money < costComp.Cost)
                return false;
        }

        // path rule
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
        if (selectedTowerPrefab == null) return;
        if (hoveredTile.IsOccupied) return;

        if (!IsPlacementValid(hoveredTile))
            return;

        TowerCost costComp = selectedTowerPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            if (!economy.TrySpendMoney(costComp.Cost))
                return;
        }

        Vector3 pos = hoveredTile.transform.position;
        GameObject towerObj = Instantiate(selectedTowerPrefab, pos, Quaternion.identity);

        if (towerObj.GetComponent<Tower>() == null)
            towerObj.AddComponent<Tower>();

        hoveredTile.SetOccupied(true);
        if (towersBlockEnemies)
            hoveredTile.SetBlocksEnemies(true);

        PathChangeBroadcaster.Bump();
    }

    private bool WouldStillHavePathIfPlacedHere(GridTile tile)
    {
        if (grid == null || pathfinder == null) return true;

        grid.RebuildLookupFromChildren();

        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);
        if (startTile == null || goalTile == null) return true;

        if (tile.Terrain == TerrainType.Blocked) return false;

        bool wasPassable = tile.IsPassableForEnemies;

        tile.SetBlocksEnemies(true);
        var path = pathfinder.FindPathAStar(startTile, goalTile);

        tile.SetBlocksEnemies(!wasPassable);

        return path != null && path.Count > 0;
    }

    private Tower RaycastTowerCenterScreen()
    {
        if (cam == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, towerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<Tower>();
        }

        return null;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
