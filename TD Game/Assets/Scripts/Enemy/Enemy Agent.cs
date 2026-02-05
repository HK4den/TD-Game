using System.Collections.Generic;
using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;

    [Header("Path")]
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private float nodeArriveDist = 0.08f;
    [SerializeField] private float yOffset = 0.0f;

    [Header("Movement")]
    [SerializeField] private float speed = 2.5f;

    private List<GridTile> tilePath;
    private int index;
    private int lastSeenPathVersion = -1;

    public void Init(GridManager g, GridPathfinder pf, Vector2Int goal, float moveSpeed)
    {
        grid = g;
        pathfinder = pf;
        goalCoord = goal;
        speed = moveSpeed;

        ForceRepath();
    }

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
    }

    private void Start()
    {
        ForceRepath();
    }

    private void Update()
    {
        // If anything changed (tower placed, wall painted, etc), repath
        if (lastSeenPathVersion != PathChangeBroadcaster.Version)
            ForceRepath();

        FollowPath();
    }

    private void ForceRepath()
    {
        lastSeenPathVersion = PathChangeBroadcaster.Version;

        if (grid == null || pathfinder == null) return;

        grid.RebuildLookupFromChildren();

        Vector2Int startCoord = grid.WorldToGrid(transform.position);
        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);

        tilePath = pathfinder.FindPathAStar(startTile, goalTile);
        index = 0;

        // If no path, just stop (later you might despawn or damage base)
        if (tilePath == null || tilePath.Count == 0) return;

        // If we're already basically at node 0, advance
        AdvanceIfClose();
    }

    private void FollowPath()
    {
        if (tilePath == null || tilePath.Count == 0) return;
        if (index >= tilePath.Count) return;

        Vector3 target = tilePath[index].transform.position;
        target.y = transform.position.y + yOffset;

        Vector3 to = target - transform.position;
        float step = speed * Time.deltaTime;

        if (to.sqrMagnitude <= nodeArriveDist * nodeArriveDist)
        {
            index++;
            return;
        }

        transform.position += to.normalized * step;

        // Face move direction (optional)
        if (to.sqrMagnitude > 0.0001f)
            transform.forward = new Vector3(to.x, 0f, to.z).normalized;
    }

    private void AdvanceIfClose()
    {
        if (tilePath == null || tilePath.Count == 0) return;

        Vector3 target = tilePath[0].transform.position;
        Vector3 to = target - transform.position;
        if (to.sqrMagnitude <= nodeArriveDist * nodeArriveDist)
            index = 1;
    }
}
