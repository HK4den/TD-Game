using UnityEngine;
using UnityEngine.InputSystem;

public class TowerPlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private GridManager grid;

    [Header("Raycast")]
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
    [SerializeField] private GridPathfinder pathfinder;

    [Header("Ghost Preview")]
    [SerializeField] private Material ghostMaterial;         // valid material (optional)
    [SerializeField] private Material invalidGhostMaterial;  // invalid material (red) (optional)
    [SerializeField] private float ghostYOffset = 0.05f;

    private GameObject ghost;
    private GridTile hoveredTile;

    private Renderer[] ghostRenderers;
    private Material validMatRuntime;
    private Material invalidMatRuntime;

    private GridTile lastTile;
    private int lastSeenPathVersion = -1;
    private bool lastWasValid;

    private EconomyManager economy;


    private void Awake()
    {
        economy = FindFirstObjectByType<EconomyManager>();

        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();

        CreateGhost();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
        {
            if (ghost != null) ghost.SetActive(false);
            return;
        }

        UpdateHoverTile();
        UpdateGhostVisualsAndPosition();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlace();
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

        // Decide valid material
        validMatRuntime = ghostMaterial;

        // If no valid material provided, use the first renderer's current material as baseline
        if (validMatRuntime == null && ghostRenderers != null && ghostRenderers.Length > 0)
            validMatRuntime = ghostRenderers[0].sharedMaterial;

        // Decide invalid material (prefer user-provided)
        if (invalidGhostMaterial != null)
        {
            invalidMatRuntime = invalidGhostMaterial;
        }
        else
        {
            // Create a red-tinted copy of the valid material if possible
            if (validMatRuntime != null)
            {
                invalidMatRuntime = new Material(validMatRuntime);
                if (invalidMatRuntime.HasProperty("_Color"))
                    invalidMatRuntime.color = new Color(1f, 0.2f, 0.2f, 0.75f);
            }
        }

        ghost.SetActive(false);
    }

    private void UpdateHoverTile()
    {
        hoveredTile = null;

        if (Mouse.current == null || cam == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            hoveredTile = hit.collider.GetComponent<GridTile>();
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

        // IMPORTANT: no ghost on occupied tiles (prevents overlap confusion)
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

        // Recompute validity only when needed:
        // - hovered tile changed
        // - path version changed (tower placements)
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
        TowerCost costComp = towerPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            if (economy.Money < costComp.Cost)
                return false;
        }

        if (tile == null) return false;

        // non-buildable or blocked terrain is invalid (but we still show red ghost)
        if (!tile.IsBuildable) return false;
        if (tile.Terrain == TerrainType.Blocked) return false;

        // if CanPlaceTower is false here, it would only be occupancy (we already hid ghost on occupied),
        // but keep the check anyway
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

        // If invalidMatRuntime couldn't be created, fall back to valid material
        if (m == null) m = validMatRuntime;

        if (m == null) return;

        for (int i = 0; i < ghostRenderers.Length; i++)
            ghostRenderers[i].sharedMaterial = m;
    }

    private void TryPlace()
    {
        if (hoveredTile == null) return;
        if (towerPrefab == null) return;

        // if occupied, no placement
        if (hoveredTile.IsOccupied) return;

        // must be valid (includes money check via IsPlacementValid)
        if (!IsPlacementValid(hoveredTile))
            return;

        // ONLY spend after we know placement will succeed
        TowerCost costComp = towerPrefab.GetComponent<TowerCost>();
        if (costComp != null && economy != null)
        {
            if (!economy.TrySpendMoney(costComp.Cost))
                return;
        }

        Vector3 pos = hoveredTile.transform.position;
        Instantiate(towerPrefab, pos, Quaternion.identity);

        hoveredTile.SetOccupied(true);
        if (towersBlockEnemies)
            hoveredTile.SetBlocksEnemies(true);

        PathChangeBroadcaster.Bump();

        ghost.SetActive(false);
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

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
