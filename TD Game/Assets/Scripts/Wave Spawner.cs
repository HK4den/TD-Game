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

    private int waveIndex = 0;
    private Coroutine running;

    private void Awake()
    {
        if (grid == null) grid = FindFirstObjectByType<GridManager>();
        if (pathfinder == null) pathfinder = FindFirstObjectByType<GridPathfinder>();
    }

    [ContextMenu("Start Next Wave")]
    public void StartNextWave()
    {
        if (grid != null) grid.RebuildLookupFromChildren();

        if (running != null) return;
        if (waves == null || waves.Length == 0) return;
        if (waveIndex >= waves.Length) return;

        running = StartCoroutine(SpawnWave(waves[waveIndex]));
        waveIndex++;
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        if (wave == null || wave.groups == null || wave.groups.Length == 0)
        {
            running = null;
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
                    yield return null; // allow a frame if interval is 0
            }

            if (group.delayAfterGroup > 0f)
                yield return new WaitForSeconds(group.delayAfterGroup);
        }

        running = null;
    }

    private void SpawnOne(EnemyAgent prefab)
    {
        if (prefab == null || grid == null || pathfinder == null) return;

        GridTile spawnTile = grid.GetTile(spawnCoord.x, spawnCoord.y);
        if (spawnTile == null) return;

        Vector3 pos = spawnTile.transform.position;
        pos.y += spawnYOffset;

        EnemyAgent enemy = Instantiate(prefab, pos, Quaternion.identity);

        // Speed comes from the enemy type (prefab)
        float speed = 2.5f;
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        if (stats != null) speed = stats.MoveSpeed;

        enemy.Init(grid, pathfinder, goalCoord, speed);
    }
}
