using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    public static event Action<EnemyAgent> OnAnySpawned;
    public static event Action<EnemyAgent> OnAnyRemoved;

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private EnemySlowController slowController;

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

    public bool HasReachedGoal => hasReachedGoal;
    public bool HasValidPath => tilePath != null && tilePath.Count > 0;
    public float MoveSpeed => speed;

    public float RemainingPathDistance
    {
        get
        {
            if (hasReachedGoal)
                return 0f;

            return CalculateRemainingPathDistance();
        }
    }

    public float DistanceTravelled
    {
        get
        {
            if (!HasValidPath)
                return 0f;

            float totalPath = CalculateTotalPathDistance();
            return Mathf.Max(0f, totalPath - RemainingPathDistance);
        }
    }

    public void SetBaseHealth(BaseHealth bh)
    {
        baseHealth = bh;
    }

    public void Init(GridManager g, GridPathfinder pf, Vector2Int goal, float moveSpeed)
    {
        grid = g;
        pathfinder = pf;
        goalCoord = goal;
        speed = moveSpeed;

        FireSpawnedIfNeeded();
        ForceRepath();
    }

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();

        if (slowController == null)
            slowController = GetComponent<EnemySlowController>();

        Debug.Log($"[EnemyAgent] Awake baseHealth={(baseHealth ? baseHealth.name : "NULL")}");
    }

    private void Start()
    {
        FireSpawnedIfNeeded();
        ForceRepath();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (hasReachedGoal)
            return;

        if (lastSeenPathVersion != PathChangeBroadcaster.Version)
            ForceRepath();

        FollowPath();
    }

    private void FireSpawnedIfNeeded()
    {
        if (spawnedEventFired)
            return;

        spawnedEventFired = true;
        OnAnySpawned?.Invoke(this);
    }

    private void ForceRepath()
    {
        lastSeenPathVersion = PathChangeBroadcaster.Version;

        if (grid == null || pathfinder == null)
            return;

        grid.RebuildLookupFromChildren();

        Vector2Int startCoord = grid.WorldToGrid(transform.position);
        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);

        tilePath = pathfinder.FindPathAStar(startTile, goalTile);
        index = 0;

        if (tilePath == null || tilePath.Count == 0)
            return;

        AdvanceIfClose();
    }

    private void FollowPath()
    {
        if (tilePath == null || tilePath.Count == 0)
            return;

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

        float moveSpeed = speed;
        if (slowController != null)
            moveSpeed *= slowController.CurrentMoveSpeedMultiplier;

        float step = moveSpeed * Time.deltaTime;

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
        if (hasReachedGoal)
            return;

        hasReachedGoal = true;

        Debug.Log($"[EnemyAgent] ArriveAtGoal baseHealth={(baseHealth ? baseHealth.name : "NULL")} dmg={baseDamage}");

        if (baseHealth != null)
            baseHealth.TakeDamage(baseDamage);
        else
            Debug.LogWarning("[EnemyAgent] No BaseHealth assigned! Did WaveSpawner call SetBaseHealth()?");

        Destroy(gameObject);
    }

    private void AdvanceIfClose()
    {
        if (tilePath == null || tilePath.Count == 0)
            return;

        Vector3 target = tilePath[0].transform.position;
        Vector3 to = target - transform.position;

        if (to.sqrMagnitude <= nodeArriveDist * nodeArriveDist)
            index = 1;
    }

    private float CalculateRemainingPathDistance()
    {
        if (tilePath == null || tilePath.Count == 0)
            return float.MaxValue;

        if (index >= tilePath.Count)
            return 0f;

        float total = 0f;

        Vector3 currentPos = transform.position;
        Vector3 currentTarget = tilePath[index].transform.position;
        currentTarget.y = currentPos.y + yOffset;

        total += Vector3.Distance(currentPos, currentTarget);

        for (int i = index; i < tilePath.Count - 1; i++)
        {
            Vector3 a = tilePath[i].transform.position;
            Vector3 b = tilePath[i + 1].transform.position;
            total += Vector3.Distance(a, b);
        }

        return total;
    }

    private float CalculateTotalPathDistance()
    {
        if (tilePath == null || tilePath.Count <= 1)
            return 0f;

        float total = 0f;
        for (int i = 0; i < tilePath.Count - 1; i++)
        {
            Vector3 a = tilePath[i].transform.position;
            Vector3 b = tilePath[i + 1].transform.position;
            total += Vector3.Distance(a, b);
        }

        return total;
    }

    private void OnDestroy()
    {
        if (spawnedEventFired)
            OnAnyRemoved?.Invoke(this);
    }
}