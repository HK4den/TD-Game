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

        [Tooltip("Delay between spawns inside this group (seconds).")]
        public float spawnInterval = 0.6f;

        [Tooltip("Extra delay AFTER this group finishes (seconds).")]
        public float delayAfterGroup = 0.0f;
    }

    [System.Serializable]
    public class Wave
    {
        public string name = "Wave";
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

    // --- Wave tracking ---
    public event Action<int> OnWaveStarted;   // 1-based wave #
    public event Action<int> OnWaveCompleted; // 1-based wave #

    public int AliveEnemiesThisWave => aliveThisWave;
    public bool IsSpawning => running != null;

    private int waveIndex = 0; // next wave to start
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
        // Helpful diagnostics so "nothing happens" never wastes your time again
        if (grid == null) Debug.LogWarning("WaveSpawner: grid reference is NULL.");
        if (pathfinder == null) Debug.LogWarning("WaveSpawner: pathfinder reference is NULL.");

        if (grid != null) grid.RebuildLookupFromChildren();

        if (running != null)
        {
            Debug.Log("WaveSpawner: denied StartNextWave because a wave is already spawning.");
            return;
        }

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("WaveSpawner: waves array is empty (Inspector data missing).");
            return;
        }

        if (waveIndex >= waves.Length)
        {
            Debug.Log("WaveSpawner: no more waves to start.");
            return;
        }

        // Reset tracking for this wave
        aliveThisWave = 0;
        spawningFinished = false;

        int startedWaveNumber = waveIndex + 1;
        Debug.Log($"WaveSpawner: starting wave {startedWaveNumber} ({waves[waveIndex].name})");

        OnWaveStarted?.Invoke(startedWaveNumber);

        running = StartCoroutine(SpawnWave(waves[waveIndex], startedWaveNumber));
        waveIndex++;
    }

    private IEnumerator SpawnWave(Wave wave, int waveNumber)
    {
        if (wave == null || wave.groups == null || wave.groups.Length == 0)
        {
            Debug.LogWarning($"WaveSpawner: wave {waveNumber} has no groups.");
            spawningFinished = true;
            running = null;
            TryCompleteWaveIfDone(waveNumber);
            yield break;
        }

        for (int g = 0; g < wave.groups.Length; g++)
        {
            SpawnGroup group = wave.groups[g];
            if (group == null)
                continue;

            if (group.enemyPrefab == null)
            {
                Debug.LogWarning($"WaveSpawner: wave {waveNumber} group {g} has no enemyPrefab assigned.");
                continue;
            }

            if (group.count <= 0)
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

        Debug.Log($"WaveSpawner: finished spawning wave {waveNumber}. Alive now: {aliveThisWave}");
        TryCompleteWaveIfDone(waveNumber);
    }

    private void SpawnOne(EnemyAgent prefab)
    {
        if (prefab == null || grid == null || pathfinder == null) return;

        GridTile spawnTile = grid.GetTile(spawnCoord.x, spawnCoord.y);
        if (spawnTile == null)
        {
            Debug.LogWarning("WaveSpawner: spawnTile is null. Check spawnCoord and that the grid exists at runtime.");
            return;
        }

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

        int currentWaveNumber = waveIndex; // because waveIndex already advanced when we started it
        TryCompleteWaveIfDone(currentWaveNumber);
    }

    private void TryCompleteWaveIfDone(int waveNumber)
    {
        if (!spawningFinished) return;
        if (aliveThisWave != 0) return;

        Debug.Log($"WaveSpawner: WAVE {waveNumber} COMPLETE");
        OnWaveCompleted?.Invoke(waveNumber);
    }
}
