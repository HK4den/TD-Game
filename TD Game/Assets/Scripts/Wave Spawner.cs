using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public int count = 10;
        public float spawnInterval = 0.6f;
        public float enemySpeed = 2.5f;
    }

    [Header("Refs")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GridPathfinder pathfinder;

    [Header("Enemy Prefab")]
    [SerializeField] private EnemyAgent enemyPrefab;

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
        if (running != null) return;
        if (waves == null || waves.Length == 0) return;
        if (waveIndex >= waves.Length) return;

        running = StartCoroutine(SpawnWave(waves[waveIndex]));
        waveIndex++;
    }

    private IEnumerator SpawnWave(Wave wave)
    {
        for (int i = 0; i < wave.count; i++)
        {
            SpawnOne(wave.enemySpeed);
            yield return new WaitForSeconds(wave.spawnInterval);
        }

        running = null;
    }

    private void SpawnOne(float speed)
    {
        if (enemyPrefab == null || grid == null || pathfinder == null) return;

        GridTile spawnTile = grid.GetTile(spawnCoord.x, spawnCoord.y);
        if (spawnTile == null) return;

        Vector3 pos = spawnTile.transform.position;
        pos.y += spawnYOffset;

        EnemyAgent enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
        enemy.Init(grid, pathfinder, goalCoord, speed);
    }
}
