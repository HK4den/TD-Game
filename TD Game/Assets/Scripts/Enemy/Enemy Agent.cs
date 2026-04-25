using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAgent : MonoBehaviour
{
    [Serializable]
    private class MoveSpeedEntry
    {
        public int sourceInstanceId;
        public string familyKey;
        public float multiplier;
        public float expireTime;
    }

    public static event Action<EnemyAgent> OnAnySpawned;
    public static event Action<EnemyAgent> OnAnyRemoved;

    public event Action<EnemyAgent> OnReachedGoal;

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private EnemySlowController slowController;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyAbilities enemyAbilities;

    [Header("Base / Goal")]
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private BaseHealth baseHealth;

    [Header("Path")]
    [SerializeField] private float nodeArriveDist = 0.08f;
    [SerializeField] private float yOffset = 0.0f;

    [Header("Movement")]
    [SerializeField] private float speed = 2.5f;

    private readonly List<GridTile> tilePath = new List<GridTile>();
    private readonly List<MoveSpeedEntry> activeMoveSpeedEntries = new List<MoveSpeedEntry>();
    private readonly Dictionary<string, float> strongestMoveSpeedByFamily = new Dictionary<string, float>();

    private int index;
    private int lastSeenPathVersion = -1;

    private bool hasReachedGoal;
    private bool spawnedEventFired;

    private float hpSpeedMultiplier = 1f;
    private float currentExternalMoveSpeedMultiplier = 1f;

    private GridTile currentTile;
    private TerrainType currentTerrainType = TerrainType.Normal;
    private float terrainSlowPercent = 0f;

    public bool HasReachedGoal => hasReachedGoal;
    public bool HasValidPath => tilePath != null && tilePath.Count > 0;
    public float MoveSpeed => speed;
    public float BaseMoveSpeed => speed;
    public GridTile CurrentTile => currentTile;
    public TerrainType CurrentTerrainType => currentTerrainType;
    public float CurrentExternalMoveSpeedMultiplier => currentExternalMoveSpeedMultiplier;
    public float CurrentHpSpeedMultiplier => hpSpeedMultiplier;
    public float TerrainSlowPercent => terrainSlowPercent;
    public bool IsAlwaysCamo => enemyAbilities != null && enemyAbilities.AlwaysCamo;
    public bool IsOnBeamTerrain => currentTile != null && currentTile.IsBeamTerrain;
    public bool IsOnBrushTerrain => currentTile != null && currentTile.IsBrushTerrain;
    public bool IsOnThickBrushTerrain => currentTile != null && currentTile.IsThickBrushTerrain;
    public bool IsOnRubbleTerrain => currentTile != null && currentTile.IsRubbleTerrain;
    public bool IsBeamProtected => IsOnBeamTerrain;
    public bool IsTerrainCamo => IsOnBrushTerrain || IsOnThickBrushTerrain;
    public bool IsCamoHidden => IsAlwaysCamo || IsTerrainCamo;

    public bool IsTargetable =>
        !hasReachedGoal &&
        !IsBeamProtected &&
        (enemyHealth == null || enemyHealth.IsTargetable);

    public float TerrainMoveSpeedMultiplier => Mathf.Clamp01(1f - terrainSlowPercent);

    public float CurrentTotalMoveSpeedMultiplier
    {
        get
        {
            float slowMultiplier = slowController != null ? slowController.CurrentMoveSpeedMultiplier : 1f;
            return Mathf.Max(0f, slowMultiplier * currentExternalMoveSpeedMultiplier * hpSpeedMultiplier * TerrainMoveSpeedMultiplier);
        }
    }

    public float CurrentFinalMoveSpeed => speed * CurrentTotalMoveSpeedMultiplier;

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
        UpdateCurrentTileAndTerrainState();
    }

    public void SetHpSpeedMultiplier(float multiplier)
    {
        hpSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ClearHpSpeedMultiplier()
    {
        hpSpeedMultiplier = 1f;
    }

    public void ApplyOrRefreshMoveSpeedMultiplier(int sourceInstanceId, string familyKey, float multiplier, float duration)
    {
        if (duration <= 0f)
            return;

        multiplier = Mathf.Max(0f, multiplier);

        string resolvedFamilyKey = ResolveFamilyKey(sourceInstanceId, familyKey);
        float expireTime = Time.time + duration;

        for (int i = 0; i < activeMoveSpeedEntries.Count; i++)
        {
            MoveSpeedEntry entry = activeMoveSpeedEntries[i];
            if (entry.sourceInstanceId == sourceInstanceId && entry.familyKey == resolvedFamilyKey)
            {
                entry.multiplier = multiplier;
                entry.expireTime = expireTime;
                RecalculateExternalMoveSpeedMultiplier();
                return;
            }
        }

        activeMoveSpeedEntries.Add(new MoveSpeedEntry
        {
            sourceInstanceId = sourceInstanceId,
            familyKey = resolvedFamilyKey,
            multiplier = multiplier,
            expireTime = expireTime
        });

        RecalculateExternalMoveSpeedMultiplier();
    }

    public void ClearAllExternalMoveSpeedMultipliers()
    {
        activeMoveSpeedEntries.Clear();
        currentExternalMoveSpeedMultiplier = 1f;
    }

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
        if (slowController == null) slowController = GetComponent<EnemySlowController>();
        if (enemyHealth == null) enemyHealth = GetComponent<EnemyHealth>();
        if (enemyAbilities == null) enemyAbilities = GetComponent<EnemyAbilities>();
    }

    private void Start()
    {
        FireSpawnedIfNeeded();
        ForceRepath();
        UpdateCurrentTileAndTerrainState();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (hasReachedGoal)
            return;

        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        bool removedAny = RemoveExpiredMoveSpeedEntries();
        if (removedAny)
            RecalculateExternalMoveSpeedMultiplier();

        if (lastSeenPathVersion != PathChangeBroadcaster.Version)
            ForceRepath();

        UpdateCurrentTileAndTerrainState();
        FollowPath();
        UpdateCurrentTileAndTerrainState();
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

        List<GridTile> newPath = pathfinder.FindPathAStarAllowStartBlocked(startTile, goalTile);

        tilePath.Clear();

        if (newPath == null || newPath.Count == 0)
        {
            index = 0;
            return;
        }

        tilePath.AddRange(newPath);
        index = FindBestPathIndexAfterRepath();
    }

    private int FindBestPathIndexAfterRepath()
    {
        if (tilePath == null || tilePath.Count == 0)
            return 0;

        Vector3 pos = transform.position;

        int closestIndex = 0;
        float closestSqrDist = float.MaxValue;

        for (int i = 0; i < tilePath.Count; i++)
        {
            Vector3 tilePos = tilePath[i].transform.position;
            tilePos.y = pos.y;

            float sqrDist = (tilePos - pos).sqrMagnitude;
            if (sqrDist < closestSqrDist)
            {
                closestSqrDist = sqrDist;
                closestIndex = i;
            }
        }

        if (tilePath.Count <= 1)
            return 0;

        if (closestIndex >= tilePath.Count - 1)
            return closestIndex;

        return closestIndex + 1;
    }

    private void UpdateCurrentTileAndTerrainState()
    {
        if (grid == null)
        {
            currentTile = null;
            currentTerrainType = TerrainType.Normal;
            terrainSlowPercent = 0f;
            return;
        }

        Vector2Int coord = grid.WorldToGrid(transform.position);
        currentTile = grid.GetTile(coord.x, coord.y);
        currentTerrainType = currentTile != null ? currentTile.Terrain : TerrainType.Normal;

        switch (currentTerrainType)
        {
            case TerrainType.Brush:
                terrainSlowPercent = 0.15f;
                break;

            case TerrainType.ThickBrush:
                terrainSlowPercent = 0.25f;
                break;

            default:
                terrainSlowPercent = 0f;
                break;
        }
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

        float moveSpeed = CurrentFinalMoveSpeed;
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

            Vector3 look = new Vector3(to.x, 0f, to.z);
            if (look.sqrMagnitude > 0.0001f)
                transform.forward = look.normalized;
        }
    }

    private void ArriveAtGoal()
    {
        if (hasReachedGoal)
            return;

        hasReachedGoal = true;

        if (baseHealth != null)
            baseHealth.TakeDamage(baseDamage);
        else
            Debug.LogWarning("[EnemyAgent] No BaseHealth assigned! Did WaveSpawner call SetBaseHealth()?");

        OnReachedGoal?.Invoke(this);
        Destroy(gameObject);
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

    private bool RemoveExpiredMoveSpeedEntries()
    {
        bool removedAny = false;
        float now = Time.time;

        for (int i = activeMoveSpeedEntries.Count - 1; i >= 0; i--)
        {
            if (now >= activeMoveSpeedEntries[i].expireTime)
            {
                activeMoveSpeedEntries.RemoveAt(i);
                removedAny = true;
            }
        }

        return removedAny;
    }

    private void RecalculateExternalMoveSpeedMultiplier()
    {
        strongestMoveSpeedByFamily.Clear();

        for (int i = 0; i < activeMoveSpeedEntries.Count; i++)
        {
            MoveSpeedEntry entry = activeMoveSpeedEntries[i];
            if (entry == null)
                continue;

            float multiplier = Mathf.Max(0f, entry.multiplier);

            if (strongestMoveSpeedByFamily.TryGetValue(entry.familyKey, out float currentBest))
            {
                float currentDistance = Mathf.Abs(currentBest - 1f);
                float incomingDistance = Mathf.Abs(multiplier - 1f);

                if (incomingDistance > currentDistance)
                    strongestMoveSpeedByFamily[entry.familyKey] = multiplier;
            }
            else
            {
                strongestMoveSpeedByFamily.Add(entry.familyKey, multiplier);
            }
        }

        float result = 1f;
        foreach (var pair in strongestMoveSpeedByFamily)
            result *= pair.Value;

        currentExternalMoveSpeedMultiplier = Mathf.Max(0f, result);
    }

    private string ResolveFamilyKey(int sourceInstanceId, string familyKey)
    {
        if (!string.IsNullOrWhiteSpace(familyKey))
            return familyKey.Trim();

        return $"__SOURCE_{sourceInstanceId}";
    }

    private void OnDestroy()
    {
        if (spawnedEventFired)
            OnAnyRemoved?.Invoke(this);
    }
}