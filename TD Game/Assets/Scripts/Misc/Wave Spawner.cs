using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Active { get; private set; }

    [System.Serializable]
    public class SpawnGroup
    {
        [Header("Enemy Type")]
        public EnemyAgent enemyPrefab;

        [Header("Counts & Timing")]
        public int count = 10;
        public float spawnInterval = 0.6f;
        public float delayAfterGroup = 0.0f;
    }

    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
        public int completionReward = 50;
        public SpawnGroup[] groups;
    }

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;
    [SerializeField] private EconomyManager economy;
    [SerializeField] private BaseHealth baseHealth;

    [Header("Spawn/Goal")]
    [SerializeField] private Vector2Int spawnCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private float spawnYOffset = 0f;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    public event Action<int> OnWaveStarted;
    public event Action<int, int> OnWaveCompleted;

    public int TotalWaves => waves != null ? waves.Length : 0;
    public int NextWaveNumber => Mathf.Clamp(waveIndex + 1, 1, Mathf.Max(1, TotalWaves));
    public bool IsSpawning => running != null;
    public int AliveEnemiesThisWave => aliveThisWave;
    public bool IsWaveInProgress => waveActiveContext && (IsSpawning || aliveThisWave > 0);

    private int waveIndex = 0;
    private Coroutine running;

    private int aliveThisWave = 0;
    private bool spawningFinished = false;
    private bool waveActiveContext = false;

    private readonly HashSet<EnemyAgent> activeWaveMembers = new HashSet<EnemyAgent>();

    private void Awake()
    {
        Active = this;

        if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
        if (baseHealth == null) baseHealth = FindFirstObjectByType<BaseHealth>();
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;
    }

    [ContextMenu("Start Next Wave")]
    public void StartNextWave()
    {
        if (grid != null)
            grid.RebuildLookupFromChildren();

        if (running != null)
            return;

        if (IsWaveInProgress)
            return;

        if (waves == null || waves.Length == 0)
            return;

        if (waveIndex >= waves.Length)
            return;

        aliveThisWave = 0;
        spawningFinished = false;
        waveActiveContext = true;
        activeWaveMembers.Clear();

        int startedWaveNumber = waveIndex + 1;
        OnWaveStarted?.Invoke(startedWaveNumber);

        running = StartCoroutine(SpawnWave(waves[waveIndex], startedWaveNumber));
        waveIndex++;
    }

    public EnemyAgent SpawnEnemyFromPrefab(EnemyAgent prefab, Vector3 worldPosition, bool registerToCurrentWave = true)
    {
        if (prefab == null || grid == null || pathfinder == null)
            return null;

        EnemyAgent enemy = Instantiate(prefab, worldPosition, Quaternion.identity);
        ConfigureSpawnedEnemy(enemy, registerToCurrentWave);
        return enemy;
    }

    public EnemyAgent SpawnChildEnemy(EnemyAgent prefab, Vector3 worldPosition)
    {
        bool registerToCurrentWave = waveActiveContext;
        return SpawnEnemyFromPrefab(prefab, worldPosition, registerToCurrentWave);
    }

    public void RegisterEnemyAsCurrentWaveMember(EnemyAgent enemy)
    {
        if (enemy == null || !waveActiveContext)
            return;

        if (!activeWaveMembers.Add(enemy))
            return;

        aliveThisWave++;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDeathFinalized += HandleEnemyDeathFinalized;

        enemy.OnReachedGoal += HandleEnemyReachedGoal;
    }

    private IEnumerator SpawnWave(Wave wave, int waveNumber)
    {
        if (wave == null || wave.groups == null || wave.groups.Length == 0)
        {
            spawningFinished = true;
            running = null;
            TryCompleteWaveIfDone(waveNumber);
            yield break;
        }

        for (int g = 0; g < wave.groups.Length; g++)
        {
            SpawnGroup group = wave.groups[g];
            if (group == null || group.enemyPrefab == null || group.count <= 0)
                continue;

            for (int i = 0; i < group.count; i++)
            {
                SpawnOne(group.enemyPrefab);

                if (group.spawnInterval > 0f)
                    yield return new WaitForSeconds(group.spawnInterval);
                else
                    yield return null;
            }

            if (group.delayAfterGroup > 0f)
                yield return new WaitForSeconds(group.delayAfterGroup);
        }

        spawningFinished = true;
        running = null;

        TryCompleteWaveIfDone(waveNumber);
    }

    private void SpawnOne(EnemyAgent prefab)
    {
        if (prefab == null || grid == null || pathfinder == null)
            return;

        GridTile spawnTile = grid.GetTile(spawnCoord.x, spawnCoord.y);
        if (spawnTile == null)
            return;

        Vector3 pos = spawnTile.transform.position;
        pos.y += spawnYOffset;

        EnemyAgent enemy = Instantiate(prefab, pos, Quaternion.identity);
        ConfigureSpawnedEnemy(enemy, true);
    }

    private void ConfigureSpawnedEnemy(EnemyAgent enemy, bool registerToCurrentWave)
    {
        if (enemy == null)
            return;

        enemy.SetBaseHealth(baseHealth);

        float moveSpeed = 2.5f;
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        if (stats != null)
            moveSpeed = stats.MoveSpeed;

        enemy.Init(grid, pathfinder, goalCoord, moveSpeed);

        if (registerToCurrentWave)
            RegisterEnemyAsCurrentWaveMember(enemy);
    }

    private void HandleEnemyDeathFinalized(EnemyHealth health)
    {
        if (health == null)
            return;

        EnemyAgent enemy = health.GetComponent<EnemyAgent>();
        if (enemy == null)
            enemy = health.GetComponentInParent<EnemyAgent>();

        MarkEnemyResolved(enemy);
    }

    private void HandleEnemyReachedGoal(EnemyAgent enemy)
    {
        MarkEnemyResolved(enemy);
    }

    private void MarkEnemyResolved(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        if (!activeWaveMembers.Remove(enemy))
            return;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null)
            health.OnDeathFinalized -= HandleEnemyDeathFinalized;

        enemy.OnReachedGoal -= HandleEnemyReachedGoal;

        if (aliveThisWave > 0)
            aliveThisWave--;

        if (aliveThisWave < 0)
            aliveThisWave = 0;

        int currentWaveNumber = waveIndex;
        TryCompleteWaveIfDone(currentWaveNumber);
    }

    private void TryCompleteWaveIfDone(int waveNumber)
    {
        if (!waveActiveContext)
            return;

        if (!spawningFinished)
            return;

        if (aliveThisWave != 0)
            return;

        int idx = waveNumber - 1;
        int reward = 50;

        if (waves != null && idx >= 0 && idx < waves.Length)
            reward = waves[idx].completionReward;

        if (economy != null && reward > 0)
            economy.AddMoney(reward);

        waveActiveContext = false;
        spawningFinished = false;

        OnWaveCompleted?.Invoke(waveNumber, reward);
    }
}