using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    private static readonly List<EnemyAgent> aliveEnemies = new List<EnemyAgent>();
    private static readonly HashSet<EnemyAgent> registeredEnemies = new HashSet<EnemyAgent>();
    private static readonly IReadOnlyList<EnemyAgent> aliveEnemiesReadOnly = aliveEnemies;

    private const float BucketSize = 4f;
    private static readonly Dictionary<Vector2Int, HashSet<EnemyAgent>> buckets = new Dictionary<Vector2Int, HashSet<EnemyAgent>>();
    private static readonly Dictionary<EnemyAgent, Vector2Int> positions = new Dictionary<EnemyAgent, Vector2Int>();
    private static readonly Dictionary<EnemyAgent, long> order = new Dictionary<EnemyAgent, long>();
    private static long nextOrder;
    private static int synchronizedFrame = -1;
    private static readonly System.Comparison<EnemyAgent> compareOrder = (left, right) => order[left].CompareTo(order[right]);

    public static void UpdatePosition(EnemyAgent enemy)
    {
        if (enemy == null || !registeredEnemies.Contains(enemy)) return;
        Vector3 position = enemy.transform.position;
        Vector2Int key = new Vector2Int(Mathf.FloorToInt(position.x / BucketSize), Mathf.FloorToInt(position.z / BucketSize));
        if (positions.TryGetValue(enemy, out Vector2Int previous))
        {
            if (previous == key) return;
            buckets[previous].Remove(enemy);
            if (buckets[previous].Count == 0) buckets.Remove(previous);
        }
        if (!buckets.TryGetValue(key, out HashSet<EnemyAgent> bucket))
        {
            bucket = new HashSet<EnemyAgent>();
            buckets.Add(key, bucket);
        }
        bucket.Add(enemy);
        positions[enemy] = key;
    }

    public static void GetNearby(Vector3 center, float radius, List<EnemyAgent> results)
    {
        results.Clear();
        if (synchronizedFrame != Time.frameCount)
        {
            synchronizedFrame = Time.frameCount;
            foreach (EnemyAgent enemy in aliveEnemies) UpdatePosition(enemy);
        }
        radius = Mathf.Max(0f, radius);
        int minX = Mathf.FloorToInt((center.x - radius) / BucketSize);
        int maxX = Mathf.FloorToInt((center.x + radius) / BucketSize);
        int minZ = Mathf.FloorToInt((center.z - radius) / BucketSize);
        int maxZ = Mathf.FloorToInt((center.z + radius) / BucketSize);
        if ((long)(maxX - minX + 1) * (maxZ - minZ + 1) > buckets.Count * 4L + 64)
        {
            results.AddRange(aliveEnemies);
            return;
        }
        for (int column = minX; column <= maxX; column++)
            for (int row = minZ; row <= maxZ; row++)
                if (buckets.TryGetValue(new Vector2Int(column, row), out HashSet<EnemyAgent> bucket))
                    foreach (EnemyAgent enemy in bucket)
                        if (enemy != null) results.Add(enemy);
        results.Sort(compareOrder);
    }

    public static IReadOnlyList<EnemyAgent> AliveEnemies => aliveEnemiesReadOnly;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        aliveEnemies.Clear();
        registeredEnemies.Clear();
        buckets.Clear();
        positions.Clear();
        order.Clear();
        nextOrder = 0;
        synchronizedFrame = -1;
    }

    public static void Register(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        if (!registeredEnemies.Add(enemy))
            return;

        aliveEnemies.Add(enemy);
        order[enemy] = nextOrder++;
        UpdatePosition(enemy);
    }

    public static void Unregister(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        if (registeredEnemies.Remove(enemy))
        {
            aliveEnemies.Remove(enemy);
            if (positions.TryGetValue(enemy, out Vector2Int key))
            {
                buckets[key].Remove(enemy);
                if (buckets[key].Count == 0) buckets.Remove(key);
            }
            positions.Remove(enemy);
            order.Remove(enemy);
        }
    }

    public static void GetAliveEnemiesNonAlloc(List<EnemyAgent> results)
    {
        if (results == null)
            return;

        results.Clear();

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyAgent enemy = aliveEnemies[i];
            if (enemy == null)
                continue;

            results.Add(enemy);
        }
    }

    public static bool HasAnyAliveEnemy()
    {
        CleanupNulls();
        return aliveEnemies.Count > 0;
    }

    private static void CleanupNulls()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] == null)
            {
                registeredEnemies.Remove(aliveEnemies[i]);
                aliveEnemies.RemoveAt(i);
            }
        }
    }
}
