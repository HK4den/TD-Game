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
        if (startTile == null || goalTile == null)
            return null;

        if (!allowBlockedStartTile && !startTile.IsPassableForEnemies)
            return null;

        if (!goalTile.IsPassableForEnemies)
            return null;

        var open = new List<GridTile>();
        var closed = new HashSet<GridTile>();

        var cameFrom = new Dictionary<GridTile, GridTile>();
        var gScore = new Dictionary<GridTile, int>();
        var fScore = new Dictionary<GridTile, int>();

        open.Add(startTile);
        gScore[startTile] = 0;
        fScore[startTile] = Heuristic(startTile, goalTile);

        while (open.Count > 0)
        {
            GridTile current = open[0];
            int bestF = GetScore(fScore, current);

            for (int i = 1; i < open.Count; i++)
            {
                int f = GetScore(fScore, open[i]);
                if (f < bestF)
                {
                    bestF = f;
                    current = open[i];
                }
            }

            if (current == goalTile)
                return ReconstructPath(cameFrom, current);

            open.Remove(current);
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

                bool inOpen = open.Contains(n);
                if (!inOpen || tentativeG < GetScore(gScore, n))
                {
                    cameFrom[n] = current;
                    gScore[n] = tentativeG;
                    fScore[n] = tentativeG + Heuristic(n, goalTile);

                    if (!inOpen)
                        open.Add(n);
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