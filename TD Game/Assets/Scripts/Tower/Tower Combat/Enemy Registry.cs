using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    private static readonly List<EnemyAgent> aliveEnemies = new List<EnemyAgent>();
    private static readonly IReadOnlyList<EnemyAgent> aliveEnemiesReadOnly = aliveEnemies;

    public static IReadOnlyList<EnemyAgent> AliveEnemies => aliveEnemiesReadOnly;

    private void OnEnable()
    {
        EnemyAgent.OnAnySpawned += HandleEnemySpawned;
        EnemyAgent.OnAnyRemoved += HandleEnemyRemoved;
    }

    private void OnDisable()
    {
        EnemyAgent.OnAnySpawned -= HandleEnemySpawned;
        EnemyAgent.OnAnyRemoved -= HandleEnemyRemoved;

        aliveEnemies.Clear();
    }

    private void HandleEnemySpawned(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        if (aliveEnemies.Contains(enemy))
            return;

        aliveEnemies.Add(enemy);
    }

    private void HandleEnemyRemoved(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        aliveEnemies.Remove(enemy);
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
                aliveEnemies.RemoveAt(i);
        }
    }
}