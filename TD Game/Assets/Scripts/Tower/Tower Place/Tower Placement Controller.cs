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
    [SerializeField] private bool towersBlockEnemies = true;   // usually true in TD
    [SerializeField] private bool enforcePath = true;

    [Header("Path Check")]
    [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private GridPathfinder pathfinder;

    [Header("Ghost Preview")]
    [SerializeField] private Material ghostMaterial; // optional
    [SerializeField] private float ghostYOffset = 0.05f;

    private GameObject ghost;
    private GridTile hoveredTile;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();

        CreateGhost();
    }

    private void Update()
    {
        UpdateHoverTile();
        UpdateGhost();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TryPlace();
    }

    private void CreateGhost()
    {
        if (towerPrefab == null) return;

        ghost = Instantiate(towerPrefab);
        ghost.name = "TowerGhost";
        SetLayerRecursively(ghost, 2); // Ignore Raycast

        // Disable colliders on ghost
        foreach (var c in ghost.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // Optional material override
        if (ghostMaterial != null)
        {
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = ghostMaterial;
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

    private void UpdateGhost()
    {
        if (ghost == null) return;

        if (hoveredTile == null || towerPrefab == null)
        {
            ghost.SetActive(false);
            return;
        }

        ghost.SetActive(true);

        Vector3 pos = hoveredTile.transform.position;
        pos.y += ghostYOffset;
        ghost.transform.position = pos;
        ghost.transform.rotation = Quaternion.identity;
    }

    private void TryPlace()
    {
        if (hoveredTile == null) return;
        if (towerPrefab == null) return;

        // Basic placement rules
        if (!hoveredTile.CanPlaceTower)
            return;

        // Optional: path enforcement
        if (enforcePath && towersBlockEnemies)
        {
            if (!WouldStillHavePathIfPlacedHere(hoveredTile))
            {
                Debug.Log("Denied: placing here would block the only path.");
                return;
            }
        }

        // Place real tower
        Vector3 pos = hoveredTile.transform.position;
        GameObject tower = Instantiate(towerPrefab, pos, Quaternion.identity);

        // Mark tile occupied + blocking
        hoveredTile.SetOccupied(true);
        if (towersBlockEnemies)
            hoveredTile.SetBlocksEnemies(true);

        // If you want grid lookup to update (not strictly needed since tiles are same)
        if (grid != null) grid.RebuildLookupFromChildren();
    }

    private bool WouldStillHavePathIfPlacedHere(GridTile tile)
    {
        if (grid == null || pathfinder == null) return true;

        grid.RebuildLookupFromChildren();

        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);
        if (startTile == null || goalTile == null) return true;

        // Temporarily mark as blocked-for-enemies
        bool oldOccupied = true; // doesn't matter, we only need blocksEnemies
        // We'll save/restore blocksEnemies by flipping terrain or blocksEnemies flag.
        // Prefer blocksEnemies so terrain stays independent.
        // BUT blocksEnemies is private; we added SetBlocksEnemies so we can safely flip it.
        // We also need its previous value; easiest: infer from IsPassableForEnemies.
        bool oldBlocks = !tile.IsPassableForEnemies || tile.Terrain == TerrainType.Blocked;

        // If tile is already blocked by terrain, it shouldn't be placeable anyway.
        // We'll just check path without changing anything.
        if (tile.Terrain == TerrainType.Blocked) return false;

        // Temporarily block
        tile.SetBlocksEnemies(true);

        var path = pathfinder.FindPathAStar(startTile, goalTile);

        // Restore (only restore to "unblocked" if it was passable originally)
        if (!oldBlocks)
            tile.SetBlocksEnemies(false);

        return path != null && path.Count > 0;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
