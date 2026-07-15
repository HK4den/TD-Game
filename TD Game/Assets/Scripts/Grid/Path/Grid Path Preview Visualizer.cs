using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPathPreviewVisualizer : MonoBehaviour
{
    private enum MarkerKind
    {
        Current,
        Removed,
        Future
    }

    private class Marker
    {
        public GridTile tile;
        public GameObject obj;
        public Vector3 targetScale;
        public Coroutine routine;
        public MarkerKind kind;
        public Vector3 basePosition;
    }

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private TowerPlacementController placement;

    [Header("Path Coords")]
    [SerializeField] private Vector2Int startCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);

    [Header("Prefabs")]
    [SerializeField] private GameObject currentPrefab;
    [SerializeField] private GameObject removedPrefab;
    [SerializeField] private GameObject futurePrefab;

    [Header("Settings")]
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private bool alwaysShowCurrentPath = true;
    [SerializeField] private bool onlyPreviewWhenHoveringCurrentPath = true;

    [Header("Animation")]
    [SerializeField] private float growDuration = 0.15f;
    [SerializeField] private float shrinkDuration = 0.15f;

    [Header("Rotation")]
    [SerializeField] private bool rotateMarkersAlongPath = true;
    [SerializeField] private float markerYawOffset = 0f;

    [Header("Bobbing")]
    [SerializeField] private bool enableBobbing = true;
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 2f;

    private readonly Dictionary<GridTile, Marker> activeMarkers = new Dictionary<GridTile, Marker>();

    private GridTile lastTile;
    private int lastPathVersion = -999;

    private void Awake()
    {
        if (grid == null)
            grid = FindFirstObjectByType<GridManager>();

        if (pathfinder == null)
            pathfinder = FindFirstObjectByType<GridPathfinder>();

        if (placement == null)
            placement = FindFirstObjectByType<TowerPlacementController>();
    }

    private void Update()
    {
        UpdateBobbing();

        if (PauseState.IsPaused)
        {
            return;
        }

        GridTile hovered = GetHoveredTile();
        int currentVersion = PathChangeBroadcaster.Version;

        bool needsRefresh = hovered != lastTile || currentVersion != lastPathVersion || ShouldRetryMissingPathDisplay();

        if (!needsRefresh)
            return;

        lastTile = hovered;
        lastPathVersion = currentVersion;

        RebuildAndShow(hovered);
    }

    private bool ShouldRetryMissingPathDisplay()
    {
        return alwaysShowCurrentPath && activeMarkers.Count == 0;
    }

    private GridTile GetHoveredTile()
    {
        if (placement == null)
            return null;

        if (!placement.IsPlacementPreviewActive)
            return null;

        return placement.CurrentHoveredTile;
    }

    private void RebuildAndShow(GridTile hovered)
    {
        if (grid == null || pathfinder == null)
        {
            HideAllMarkersAnimated();
            return;
        }

        grid.RebuildLookupFromChildren();

        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);

        if (startTile == null || goalTile == null)
        {
            HideAllMarkersAnimated();
            return;
        }

        List<GridTile> currentPath = pathfinder.FindPathAStar(startTile, goalTile);
        if (currentPath == null || currentPath.Count == 0)
        {
            HideAllMarkersAnimated();
            return;
        }

        HashSet<GridTile> currentSet = new HashSet<GridTile>(currentPath);

        if (hovered == null)
        {
            if (alwaysShowCurrentPath)
                ShowCurrentPathOnly(currentPath, null);
            else
                HideAllMarkersAnimated();

            return;
        }

        if (onlyPreviewWhenHoveringCurrentPath && !currentSet.Contains(hovered))
        {
            if (alwaysShowCurrentPath)
                ShowCurrentPathOnly(currentPath, hovered);
            else
                HideAllMarkersAnimated();

            return;
        }

        bool originalBlocksEnemies = hovered.BlocksEnemies;

        hovered.SetBlocksEnemies(true);
        List<GridTile> previewPath = pathfinder.FindPathAStar(startTile, goalTile);
        hovered.SetBlocksEnemies(originalBlocksEnemies);

        if (previewPath == null || previewPath.Count == 0)
        {
            ShowCurrentAsRemoved(currentPath, hovered);
            return;
        }

        ShowPathDiff(currentPath, previewPath, hovered);
    }

    private void ShowCurrentPathOnly(List<GridTile> currentPath, GridTile hovered)
    {
        HashSet<GridTile> wantedTiles = new HashSet<GridTile>();

        for (int i = 0; i < currentPath.Count; i++)
        {
            GridTile tile = currentPath[i];

            if (ShouldSkipPathEndpoint(currentPath, i))
                continue;

            if (tile == hovered)
                continue;

            wantedTiles.Add(tile);
            EnsureMarker(tile, currentPrefab, MarkerKind.Current, GetMarkerRotationForPath(currentPath, i, currentPrefab));
        }

        RemoveUnwantedMarkers(wantedTiles);
    }

    private void ShowCurrentAsRemoved(List<GridTile> currentPath, GridTile hovered)
    {
        HashSet<GridTile> wantedTiles = new HashSet<GridTile>();

        for (int i = 0; i < currentPath.Count; i++)
        {
            GridTile tile = currentPath[i];

            if (ShouldSkipPathEndpoint(currentPath, i))
                continue;

            if (tile == hovered)
                continue;

            wantedTiles.Add(tile);
            EnsureMarker(tile, removedPrefab, MarkerKind.Removed, GetMarkerRotationForPath(currentPath, i, removedPrefab));
        }

        RemoveUnwantedMarkers(wantedTiles);
    }

    private void ShowPathDiff(List<GridTile> currentPath, List<GridTile> previewPath, GridTile hovered)
    {
        HashSet<GridTile> wantedTiles = new HashSet<GridTile>();
        HashSet<GridTile> currentSet = new HashSet<GridTile>(currentPath);
        HashSet<GridTile> previewSet = new HashSet<GridTile>(previewPath);
        Dictionary<GridTile, int> previewIndices = BuildPathIndexLookup(previewPath);

        for (int i = 0; i < currentPath.Count; i++)
        {
            GridTile tile = currentPath[i];

            if (ShouldSkipPathEndpoint(currentPath, i))
                continue;

            if (tile == hovered)
                continue;

            wantedTiles.Add(tile);

            if (previewSet.Contains(tile))
            {
                int previewIndex = previewIndices[tile];
                EnsureMarker(tile, currentPrefab, MarkerKind.Current, GetMarkerRotationForPath(previewPath, previewIndex, currentPrefab));
            }
            else
            {
                EnsureMarker(tile, removedPrefab, MarkerKind.Removed, GetMarkerRotationForPath(currentPath, i, removedPrefab));
            }
        }

        for (int i = 0; i < previewPath.Count; i++)
        {
            GridTile tile = previewPath[i];

            if (ShouldSkipPathEndpoint(previewPath, i))
                continue;

            if (tile == hovered)
                continue;

            if (currentSet.Contains(tile))
                continue;

            wantedTiles.Add(tile);
            EnsureMarker(tile, futurePrefab, MarkerKind.Future, GetMarkerRotationForPath(previewPath, i, futurePrefab));
        }

        RemoveUnwantedMarkers(wantedTiles);
    }

    private bool ShouldSkipPathEndpoint(List<GridTile> path, int index)
    {
        return index <= 0 || index >= path.Count - 1;
    }

    private Dictionary<GridTile, int> BuildPathIndexLookup(List<GridTile> path)
    {
        Dictionary<GridTile, int> lookup = new Dictionary<GridTile, int>();

        if (path == null)
            return lookup;

        for (int i = 0; i < path.Count; i++)
        {
            GridTile tile = path[i];

            if (tile != null && !lookup.ContainsKey(tile))
                lookup.Add(tile, i);
        }

        return lookup;
    }

    private Quaternion GetMarkerRotationForPath(List<GridTile> path, int index, GameObject prefab)
    {
        Quaternion fallback = prefab != null ? prefab.transform.rotation : Quaternion.identity;

        if (!rotateMarkersAlongPath || path == null || index < 0 || index >= path.Count)
            return fallback;

        GridTile current = path[index];
        GridTile next = index + 1 < path.Count ? path[index + 1] : null;
        GridTile previous = index - 1 >= 0 ? path[index - 1] : null;

        Vector3 direction = Vector3.zero;

        if (current != null && next != null)
            direction = next.transform.position - current.transform.position;
        else if (current != null && previous != null)
            direction = current.transform.position - previous.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return fallback;

        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Vector3 prefabEuler = fallback.eulerAngles;

        return Quaternion.Euler(prefabEuler.x, yaw + markerYawOffset, prefabEuler.z);
    }

    private void EnsureMarker(GridTile tile, GameObject prefab, MarkerKind kind, Quaternion rotation)
    {
        if (tile == null || prefab == null)
            return;

        Vector3 basePos = GetMarkerBasePosition(tile);

        if (activeMarkers.TryGetValue(tile, out Marker existing))
        {
            if (existing.kind == kind && existing.obj != null)
            {
                existing.basePosition = basePos;
                existing.obj.transform.rotation = rotation;
                return;
            }

            RemoveMarkerAnimated(tile);
        }

        GameObject obj = Instantiate(prefab, basePos, rotation, transform);

        Marker marker = new Marker
        {
            tile = tile,
            obj = obj,
            targetScale = prefab.transform.localScale,
            kind = kind,
            basePosition = basePos
        };

        obj.transform.localScale = Vector3.zero;

        activeMarkers[tile] = marker;
        marker.routine = StartCoroutine(ScaleMarker(marker, Vector3.zero, marker.targetScale, growDuration, false));
    }

    private Vector3 GetMarkerBasePosition(GridTile tile)
    {
        Vector3 pos = tile.transform.position;
        pos.y += yOffset;
        return pos;
    }

    private void UpdateBobbing()
    {
        if (!enableBobbing)
            return;

        float safeBobHeight = Mathf.Max(0f, bobHeight);
        float safeBobSpeed = Mathf.Max(0f, bobSpeed);

        foreach (var pair in activeMarkers)
        {
            Marker marker = pair.Value;
            if (marker == null || marker.obj == null || marker.tile == null)
                continue;

            bool checkerUpFirst = ((marker.tile.X + marker.tile.Z) % 2) == 0;
            float phase = checkerUpFirst ? 0f : Mathf.PI;

            float wave = Mathf.Sin((Time.time * safeBobSpeed) + phase);

            // Converts -1..1 into 0..1, so it never goes below the base position.
            float normalizedHeight = (wave + 1f) * 0.5f;

            Vector3 pos = marker.basePosition;
            pos.y += normalizedHeight * safeBobHeight;

            marker.obj.transform.position = pos;
        }
    }

    private void RemoveUnwantedMarkers(HashSet<GridTile> wantedTiles)
    {
        List<GridTile> toRemove = new List<GridTile>();

        foreach (var pair in activeMarkers)
        {
            if (!wantedTiles.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        for (int i = 0; i < toRemove.Count; i++)
            RemoveMarkerAnimated(toRemove[i]);
    }

    private void HideAllMarkersAnimated()
    {
        List<GridTile> toRemove = new List<GridTile>();

        foreach (var pair in activeMarkers)
            toRemove.Add(pair.Key);

        for (int i = 0; i < toRemove.Count; i++)
            RemoveMarkerAnimated(toRemove[i]);
    }

    private void RemoveMarkerAnimated(GridTile tile)
    {
        if (!activeMarkers.TryGetValue(tile, out Marker marker))
            return;

        activeMarkers.Remove(tile);

        if (marker == null || marker.obj == null)
            return;

        if (marker.routine != null)
            StopCoroutine(marker.routine);

        marker.routine = StartCoroutine(ScaleMarker(marker, marker.obj.transform.localScale, Vector3.zero, shrinkDuration, true));
    }

    private IEnumerator ScaleMarker(Marker marker, Vector3 from, Vector3 to, float duration, bool destroyAfter)
    {
        if (marker == null || marker.obj == null)
            yield break;

        if (duration <= 0f)
        {
            marker.obj.transform.localScale = to;

            if (destroyAfter && marker.obj != null)
                Destroy(marker.obj);

            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            if (!PauseState.IsPaused)
                timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);

            if (marker.obj != null)
                marker.obj.transform.localScale = Vector3.LerpUnclamped(from, to, t);

            yield return null;
        }

        if (marker.obj != null)
            marker.obj.transform.localScale = to;

        if (destroyAfter && marker.obj != null)
            Destroy(marker.obj);
    }
}
