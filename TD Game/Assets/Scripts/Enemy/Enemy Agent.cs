using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    // Global lifecycle events (WaveSpawner listens to these)
    public static event Action<EnemyAgent> OnAnySpawned;
    public static event Action<EnemyAgent> OnAnyRemoved;

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;

    [Header("Base / Goal")]
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private BaseHealth baseHealth;

    [Header("Path")]
    [SerializeField] private float nodeArriveDist = 0.08f;
    [SerializeField] private float yOffset = 0.0f;

    [Header("Movement")]
    [SerializeField] private float speed = 2.5f;

    private List<GridTile> tilePath;
    private int index;
    private int lastSeenPathVersion = -1;

    private bool hasReachedGoal;
    private bool spawnedEventFired;

    public void Init(GridManager g, GridPathfinder pf, Vector2Int goal, float moveSpeed)
    {
        grid = g;
        pathfinder = pf;
        goalCoord = goal;
        speed = moveSpeed;

        if (baseHealth == null) baseHealth = FindFirstObjectByType<BaseHealth>();

        FireSpawnedIfNeeded();
        ForceRepath();
    }

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
        if (baseHealth == null) baseHealth = FindFirstObjectByType<BaseHealth>();
    }

    private void Start()
    {
        // In case something instantiates enemies without calling Init (debug/testing),
        // still count them as spawned.
        FireSpawnedIfNeeded();

        ForceRepath();
    }

    private void Update()
    {
        if (hasReachedGoal) return;

        if (lastSeenPathVersion != PathChangeBroadcaster.Version)
            ForceRepath();

        FollowPath();
    }

    private void FireSpawnedIfNeeded()
    {
        if (spawnedEventFired) return;
        spawnedEventFired = true;
        OnAnySpawned?.Invoke(this);
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

        if (tilePath == null || tilePath.Count == 0) return;

        AdvanceIfClose();
    }

    private void FollowPath()
    {
        if (tilePath == null || tilePath.Count == 0) return;

        if (index >= tilePath.Count)
        {
            ArriveAtGoal();
            return;
        }

        Vector3 target = tilePath[index].transform.position;
        target.y = transform.position.y + yOffset;

        Vector3 to = target - transform.position;
        float dist = to.magnitude;

        if (dist <= nodeArriveDist)
        {
            index++;

            if (index >= tilePath.Count)
                ArriveAtGoal();

            return;
        }

        float step = speed * Time.deltaTime;

        // Clamp so we don't overshoot
        if (dist <= step && dist > 0.0001f)
        {
            transform.position = target;
            index++;

            if (index >= tilePath.Count)
                ArriveAtGoal();

            return;
        }

        if (dist > 0.0001f)
        {
            transform.position += (to / dist) * step;
            transform.forward = new Vector3(to.x, 0f, to.z).normalized;
        }
    }

    private void ArriveAtGoal()
    {
        if (hasReachedGoal) return;
        hasReachedGoal = true;

        if (baseHealth != null)
            baseHealth.TakeDamage(baseDamage);

        Destroy(gameObject);
    }

    private void AdvanceIfClose()
    {
        if (tilePath == null || tilePath.Count == 0) return;

        Vector3 target = tilePath[0].transform.position;
        Vector3 to = target - transform.position;

        if (to.sqrMagnitude <= nodeArriveDist * nodeArriveDist)
            index = 1;
    }

    private void OnDestroy()
    {
        // Only report removal if we had reported spawn.
        // This avoids weird cases during scene shutdown.
        if (spawnedEventFired)
            OnAnyRemoved?.Invoke(this);
    }
}
