using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wizliens/Waves/Wave Set", fileName = "Wave Set")]
public class WaveSet : ScriptableObject
{
    public enum WaveStepType
    {
        SingleGroup = 0,
        Alternating = 1,
        Sequence = 2,
        RandomPool = 3,
        Burst = 4,
        Delay = 5
    }

    [Serializable]
    public class EnemyRef
    {
        public string enemyId;
        public EnemyAgent prefab;

        public EnemyAgent Resolve(EnemyCatalog catalog)
        {
            if (catalog != null)
                return catalog.ResolvePrefab(enemyId, prefab);

            return prefab;
        }

        public string GetDisplayName(EnemyCatalog catalog)
        {
            if (catalog != null)
            {
                EnemyCatalog.Entry entry = catalog.FindById(enemyId);
                if (entry != null)
                    return entry.DisplayName;
            }

            if (prefab != null)
                return prefab.name;

            if (!string.IsNullOrWhiteSpace(enemyId))
                return enemyId;

            return "Enemy";
        }
    }

    [Serializable]
    public class WeightedEnemyRef
    {
        public EnemyRef enemy = new EnemyRef();
        public int weight = 1;
    }

    [Serializable]
    public class WaveStep
    {
        public string label;
        [HideInInspector]
        public bool editorCollapsed;
        public WaveStepType type = WaveStepType.SingleGroup;
        public EnemyRef enemy = new EnemyRef();
        public List<EnemyRef> enemies = new List<EnemyRef>();
        public List<WeightedEnemyRef> randomPool = new List<WeightedEnemyRef>();
        public int count = 10;
        public int repeatCount = 1;
        public float spawnInterval = 0.6f;
        [HideInInspector]
        public float delayBeforeStep = 0f;
        public float delayAfterStep = 0f;
        public float delayDuration = 1f;
    }

    [Serializable]
    public class WaveDefinition
    {
        public string editorLabel;
        [TextArea(2, 5)]
        public string notes;
        public int completionReward = 50;
        public List<WaveStep> steps = new List<WaveStep>();
    }

    [SerializeField] private EnemyCatalog enemyCatalog;
    [SerializeField] private int defaultCompletionReward = 50;
    [SerializeField] private float defaultSpawnInterval = 0.6f;
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    public EnemyCatalog EnemyCatalog => enemyCatalog;
    public int DefaultCompletionReward => defaultCompletionReward;
    public float DefaultSpawnInterval => defaultSpawnInterval;
    public IReadOnlyList<WaveDefinition> Waves => waves;
    public int WaveCount => waves != null ? waves.Count : 0;

    public WaveDefinition GetWave(int index)
    {
        if (waves == null || index < 0 || index >= waves.Count)
            return null;

        return waves[index];
    }

    public string GetWaveDisplayName(int index)
    {
        string number = $"Wave {index + 1:00}";
        WaveDefinition wave = GetWave(index);
        if (wave == null || string.IsNullOrWhiteSpace(wave.editorLabel))
            return number;

        return $"{number} - {wave.editorLabel.Trim()}";
    }

    public int GetCompletionReward(int index)
    {
        WaveDefinition wave = GetWave(index);
        if (wave == null)
            return defaultCompletionReward;

        return wave.completionReward;
    }

    public int CountEstimatedEnemies(int waveIndex)
    {
        WaveDefinition wave = GetWave(waveIndex);
        if (wave == null || wave.steps == null)
            return 0;

        int total = 0;
        for (int i = 0; i < wave.steps.Count; i++)
            total += CountEstimatedEnemies(wave.steps[i]);

        return total;
    }

    public int CountEstimatedEnemies(WaveStep step)
    {
        if (step == null)
            return 0;

        switch (step.type)
        {
            case WaveStepType.Delay:
                return 0;

            case WaveStepType.Sequence:
                return Mathf.Max(0, step.repeatCount) * (step.enemies != null ? step.enemies.Count : 0);

            default:
                return Mathf.Max(0, step.count);
        }
    }

    public float EstimateSpawnDuration(int waveIndex)
    {
        WaveDefinition wave = GetWave(waveIndex);
        if (wave == null || wave.steps == null)
            return 0f;

        float total = 0f;
        for (int i = 0; i < wave.steps.Count; i++)
        {
            WaveStep step = wave.steps[i];
            if (step == null)
                continue;

            if (step.type == WaveStepType.Delay)
            {
                total += Mathf.Max(0f, step.delayDuration);
            }
            else
            {
                int enemyCount = CountEstimatedEnemies(step);
                if (enemyCount > 1)
                    total += Mathf.Max(0f, step.spawnInterval) * (enemyCount - 1);
            }

            total += Mathf.Max(0f, step.delayAfterStep);
        }

        return total;
    }

    private void OnValidate()
    {
        if (waves == null)
            waves = new List<WaveDefinition>();

        for (int i = 0; i < waves.Count; i++)
        {
            WaveDefinition wave = waves[i];
            if (wave == null)
            {
                waves[i] = new WaveDefinition { completionReward = defaultCompletionReward };
                continue;
            }

            if (wave.steps == null)
                wave.steps = new List<WaveStep>();

            for (int s = 0; s < wave.steps.Count; s++)
            {
                WaveStep step = wave.steps[s];
                if (step == null)
                {
                    wave.steps[s] = new WaveStep { spawnInterval = defaultSpawnInterval };
                    continue;
                }

                step.count = Mathf.Max(0, step.count);
                step.repeatCount = Mathf.Max(0, step.repeatCount);
                step.spawnInterval = Mathf.Max(0f, step.spawnInterval);
                step.delayBeforeStep = 0f;
                step.delayAfterStep = Mathf.Max(0f, step.delayAfterStep);
                step.delayDuration = Mathf.Max(0f, step.delayDuration);
            }
        }
    }
}

