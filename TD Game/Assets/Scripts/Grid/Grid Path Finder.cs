using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;

    [Header("Start / Goal Coords")]
    [SerializeField] private Vector2Int start = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goal = new Vector2Int(19, 19);

    [Header("Enemy Test")]
    [SerializeField] private EnemyMover enemyPrefab;
    [SerializeField] private float pathY = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool drawPathGizmos = true;

    private readonly List<GridTile> neighbors = new List<GridTile>(4);
    private readonly PathOpenHeap open = new PathOpenHeap();
    private readonly HashSet<GridTile> openMembership = new HashSet<GridTile>();
    private readonly HashSet<GridTile> closed = new HashSet<GridTile>();
    private readonly Dictionary<GridTile, GridTile> cameFrom = new Dictionary<GridTile, GridTile>();
    private readonly Dictionary<GridTile, int> gScore = new Dictionary<GridTile, int>();
    private readonly Dictionary<GridTile, int> fScore = new Dictionary<GridTile, int>();
    private readonly Dictionary<(GridTile, GridTile, bool), List<GridTile>> cachedPaths = new Dictionary<(GridTile, GridTile, bool), List<GridTile>>();
    private int cachedRevision = -1;
    private int cachedVersion = -1;
    private GridManager cachedGrid;
    private List<GridTile> lastTilePath;
    private List<Vector3> lastWorldPath;

    private void Awake()
    {
        if (grid == null)
            grid = FindFirstObjectByType<GridManager>();

        if (grid != null)
            grid.RebuildLookupFromChildren();
    }

    [ContextMenu("Recompute Path")]
    public void RecomputePath()
    {
        if (grid == null)
            return;

        GridTile s = grid.GetTile(start.x, start.y);
        GridTile g = grid.GetTile(goal.x, goal.y);

        lastTilePath = FindPathAStar(s, g);
        lastWorldPath = lastTilePath == null ? null : ConvertToWorldPath(lastTilePath);

        Debug.Log(lastTilePath == null ? "No path found." : $"Path length: {lastTilePath.Count}");
    }

    [ContextMenu("Spawn Test Enemy")]
    public void SpawnTestEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("GridPathfinder: enemyPrefab not assigned.");
            return;
        }

        if (lastWorldPath == null || lastWorldPath.Count == 0)
        {
            RecomputePath();
            if (lastWorldPath == null || lastWorldPath.Count == 0)
            {
                Debug.LogWarning("GridPathfinder: Can't spawn enemy because path is null.");
                return;
            }
        }

        EnemyMover enemy = Instantiate(enemyPrefab);
        enemy.SetPath(lastWorldPath);
    }

    private List<Vector3> ConvertToWorldPath(List<GridTile> tilePath)
    {
        var pts = new List<Vector3>(tilePath.Count);
        for (int i = 0; i < tilePath.Count; i++)
        {
            Vector3 p = tilePath[i].transform.position;
            p.y += pathY;
            pts.Add(p);
        }

        return pts;
    }

    public List<GridTile> FindPathAStar(GridTile startTile, GridTile goalTile)
    {
        return FindPathAStarInternal(startTile, goalTile, false);
    }

    public List<GridTile> FindPathAStarAllowStartBlocked(GridTile startTile, GridTile goalTile)
    {
        return FindPathAStarInternal(startTile, goalTile, true);
    }

    private List<GridTile> FindPathAStarInternal(GridTile startTile, GridTile goalTile, bool allowBlockedStartTile)
    {
        if (cachedRevision != GridTile.NavigationRevision || cachedVersion != PathChangeBroadcaster.Version || cachedGrid != grid)
        {
            cachedPaths.Clear();
            cachedRevision = GridTile.NavigationRevision;
            cachedVersion = PathChangeBroadcaster.Version;
            cachedGrid = grid;
        }
        var key = (startTile, goalTile, allowBlockedStartTile);
        if (!cachedPaths.TryGetValue(key, out List<GridTile> path))
        {
            path = Search(startTile, goalTile, allowBlockedStartTile);
            if (cachedPaths.Count >= 256) cachedPaths.Clear();
            cachedPaths[key] = path;
        }
        return path == null ? null : new List<GridTile>(path);
    }

    private List<GridTile> Search(GridTile startTile, GridTile goalTile, bool allowBlockedStartTile)
    {
        if (startTile == null || goalTile == null)
            return null;

        if (!allowBlockedStartTile && !startTile.IsPassableForEnemies)
            return null;

        if (!goalTile.IsPassableForEnemies)
            return null;

        open.Clear();
        openMembership.Clear();
        closed.Clear();
        cameFrom.Clear();
        gScore.Clear();
        fScore.Clear();

        open.AddOrUpdate(startTile, Heuristic(startTile, goalTile));
        openMembership.Add(startTile);
        gScore[startTile] = 0;
        fScore[startTile] = Heuristic(startTile, goalTile);

        while (open.Count > 0)
        {
            GridTile current = open.Pop();

            if (current == goalTile)
                return ReconstructPath(cameFrom, current);

            openMembership.Remove(current);
            closed.Add(current);

            grid.GetNeighbors4(current, neighbors);
            for (int i = 0; i < neighbors.Count; i++)
            {
                GridTile n = neighbors[i];
                if (n == null)
                    continue;

                if (closed.Contains(n))
                    continue;

                bool isStartTile = n == startTile;
                if (!isStartTile || !allowBlockedStartTile)
                {
                    if (!n.IsPassableForEnemies)
                        continue;
                }

                int tentativeG = GetScore(gScore, current) + 1;

                bool inOpen = openMembership.Contains(n);
                if (!inOpen || tentativeG < GetScore(gScore, n))
                {
                    cameFrom[n] = current;
                    gScore[n] = tentativeG;
                    fScore[n] = tentativeG + Heuristic(n, goalTile);
                    open.AddOrUpdate(n, fScore[n]);

                    if (!inOpen)
                    {
                        openMembership.Add(n);
                    }
                }
            }
        }

        return null;
    }

    private int Heuristic(GridTile a, GridTile b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Z - b.Z);
    }

    private int GetScore(Dictionary<GridTile, int> dict, GridTile tile)
    {
        return dict.TryGetValue(tile, out int v) ? v : int.MaxValue / 4;
    }

    private List<GridTile> ReconstructPath(Dictionary<GridTile, GridTile> cameFrom, GridTile current)
    {
        var path = new List<GridTile> { current };

        while (cameFrom.TryGetValue(current, out GridTile prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private void OnDrawGizmos()
    {
        if (!drawPathGizmos || lastWorldPath == null || lastWorldPath.Count < 2)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < lastWorldPath.Count - 1; i++)
            Gizmos.DrawLine(lastWorldPath[i], lastWorldPath[i + 1]);
    }
}
