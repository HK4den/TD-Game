using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySplitOnDeath : EnemyDeathBehavior
{
    [System.Serializable]
    public class SplitSpawnGroup
    {
        [Header("Enemy Type")]
        public EnemyAgent enemyPrefab;

        [Header("Counts & Timing")]
        public int count = 1;
        public float spawnInterval = 0f;
        public float delayBeforeGroup = 0f;
    }

    [Header("Spawn Groups")]
    [SerializeField] private SplitSpawnGroup[] groups;

    [Header("Spawn Position")]
    [SerializeField] private float spawnYOffset = 0f;
    [SerializeField] private bool useTinyHorizontalJitter = false;
    [SerializeField] private float jitterRadius = 0.08f;

    [Header("Optional Visual")]
    [SerializeField] private EnemyRadiusVisualizer radiusVisualizer;
    [SerializeField] private bool showRadiusWhenSpawning = false;
    [SerializeField] private float visualRadius = 0.6f;
    [SerializeField] private float radiusVisibleDuration = 0.35f;

    public override float GetRequiredDelay()
    {
        float total = 0f;

        if (groups == null || groups.Length == 0)
            return 0f;

        for (int g = 0; g < groups.Length; g++)
        {
            SplitSpawnGroup group = groups[g];
            if (group == null || group.enemyPrefab == null || group.count <= 0)
                continue;

            total += Mathf.Max(0f, group.delayBeforeGroup);

            if (group.count > 1)
                total += Mathf.Max(0f, group.spawnInterval) * (group.count - 1);
        }

        return total;
    }

    public override void TriggerDeath(EnemyHealth health)
    {
        if (!enabled || health == null)
            return;

        StartCoroutine(SplitRoutine(health));
    }

    private IEnumerator SplitRoutine(EnemyHealth ownerHealth)
    {
        if (groups == null || groups.Length == 0)
            yield break;

        Vector3 baseSpawnPosition = ownerHealth.transform.position;
        baseSpawnPosition.y += spawnYOffset;

        if (showRadiusWhenSpawning && radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(visualRadius);
            radiusVisualizer.ShowForDuration(radiusVisibleDuration);
        }

        for (int g = 0; g < groups.Length; g++)
        {
            SplitSpawnGroup group = groups[g];
            if (group == null || group.enemyPrefab == null || group.count <= 0)
                continue;

            float delayBeforeGroup = Mathf.Max(0f, group.delayBeforeGroup);
            if (delayBeforeGroup > 0f)
            {
                float delayTimer = delayBeforeGroup;
                while (delayTimer > 0f)
                {
                    if (!PauseState.IsPaused)
                        delayTimer -= Time.deltaTime;

                    yield return null;
                }
            }

            for (int i = 0; i < group.count; i++)
            {
                SpawnChild(group.enemyPrefab, baseSpawnPosition);

                if (i < group.count - 1)
                {
                    float interval = Mathf.Max(0f, group.spawnInterval);
                    if (interval > 0f)
                    {
                        float intervalTimer = interval;
                        while (intervalTimer > 0f)
                        {
                            if (!PauseState.IsPaused)
                                intervalTimer -= Time.deltaTime;

                            yield return null;
                        }
                    }
                    else
                    {
                        yield return null;
                    }
                }
            }
        }
    }

    private void SpawnChild(EnemyAgent prefab, Vector3 baseSpawnPosition)
    {
        if (prefab == null)
            return;

        Vector3 spawnPos = baseSpawnPosition;

        if (useTinyHorizontalJitter && jitterRadius > 0f)
        {
            Vector2 circle = Random.insideUnitCircle * jitterRadius;
            spawnPos.x += circle.x;
            spawnPos.z += circle.y;
        }

        if (WaveSpawner.Active != null)
        {
            WaveSpawner.Active.SpawnChildEnemy(prefab, spawnPos);
            return;
        }

        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}