using System;
using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
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
        public int completionReward = 50;   // NEW: per-wave reward
        public SpawnGroup[] groups;
    }

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;

    [Header("Spawn/Goal")]
    [SerializeField] private Vector2Int spawnCoord = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int goalCoord = new Vector2Int(19, 19);
    [SerializeField] private float spawnYOffset = 0f;

    [Header("Waves")]
    [SerializeField] private Wave[] waves;

    public event Action<int> OnWaveStarted;                 // wave # (1-based)
    public event Action<int, int> OnWaveCompleted;          // wave #, reward $

    public int TotalWaves => waves != null ? waves.Length : 0;
    public int NextWaveNumber => Mathf.Clamp(waveIndex + 1, 1, Mathf.Max(1, TotalWaves)); // UI convenience
    public bool IsSpawning => running != null;
    public int AliveEnemiesThisWave => aliveThisWave;

    // "Wave in progress" means either we are still spawning OR enemies are still alive
    public bool IsWaveInProgress => IsSpawning || aliveThisWave > 0;

    private int waveIndex = 0; // next wave to start (0-based)
    private Coroutine running;

    private int aliveThisWave = 0;
    private bool spawningFinished = false;

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
    }

    private void OnEnable()
    {
        EnemyAgent.OnAnyRemoved += HandleEnemyRemoved;
    }

    private void OnDisable()
    {
        EnemyAgent.OnAnyRemoved -= HandleEnemyRemoved;
    }

    [ContextMenu("Start Next Wave")]
    public void StartNextWave()
    {
        if (grid != null) grid.RebuildLookupFromChildren();

        if (running != null) return;
        if (waves == null || waves.Length == 0) return;
        if (waveIndex >= waves.Length) return;

        aliveThisWave = 0;
        spawningFinished = false;

        int startedWaveNumber = waveIndex + 1;
        OnWaveStarted?.Invoke(startedWaveNumber);

        running = StartCoroutine(SpawnWave(waves[waveIndex], startedWaveNumber));
        waveIndex++;
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
        if (prefab == null || grid == null || pathfinder == null) return;

        GridTile spawnTile = grid.GetTile(spawnCoord.x, spawnCoord.y);
        if (spawnTile == null) return;

        Vector3 pos = spawnTile.transform.position;
        pos.y += spawnYOffset;

        EnemyAgent enemy = Instantiate(prefab, pos, Quaternion.identity);

        float speed = 2.5f;
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        if (stats != null) speed = stats.MoveSpeed;

        enemy.Init(grid, pathfinder, goalCoord, speed);

        aliveThisWave++;
    }

    private void HandleEnemyRemoved(EnemyAgent enemy)
    {
        if (aliveThisWave <= 0) return;

        aliveThisWave--;
        if (aliveThisWave < 0) aliveThisWave = 0;

        // waveIndex already advanced, so current wave number is waveIndex (1-based)
        int currentWaveNumber = waveIndex;
        TryCompleteWaveIfDone(currentWaveNumber);
    }

    private void TryCompleteWaveIfDone(int waveNumber)
    {
        if (!spawningFinished) return;
        if (aliveThisWave != 0) return;

        // Reward is tied to the wave that just completed: waveNumber (1-based) -> index waveNumber-1
        int idx = waveNumber - 1;
        int reward = 50;
        if (waves != null && idx >= 0 && idx < waves.Length)
            reward = waves[idx].completionReward;

        OnWaveCompleted?.Invoke(waveNumber, reward);
    }
}
