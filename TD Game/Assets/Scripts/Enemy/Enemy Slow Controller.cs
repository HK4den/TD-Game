using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySlowController : MonoBehaviour
{
    [Serializable]
    private class SlowEntry
    {
        public int sourceInstanceId;
        public string familyKey;
        public float slowPercent;
        public float expireTime;
    }

    [Header("Debug")]
    [SerializeField] private bool debugLogSlowChanges = false;

    private readonly List<SlowEntry> activeSlows = new List<SlowEntry>();
    private readonly Dictionary<string, float> strongestByFamily = new Dictionary<string, float>();

    private EnemyAbilities abilities;
    private float currentMoveSpeedMultiplier = 1f;

    public float CurrentMoveSpeedMultiplier => currentMoveSpeedMultiplier;

    private void Awake()
    {
        abilities = GetComponent<EnemyAbilities>();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        bool removedAny = RemoveExpiredSlows();
        RecalculateMultiplier();

        if (removedAny && debugLogSlowChanges)
            Debug.Log($"[EnemySlowController] Expired slows removed on {name}. Current multiplier={currentMoveSpeedMultiplier:0.###}");
    }

    public void ApplyOrRefreshSlow(int sourceInstanceId, string familyKey, float slowPercent, float duration)
    {
        if (duration <= 0f)
            return;

        float adjustedSlow = slowPercent;
        if (abilities != null)
            adjustedSlow = abilities.AdjustSlowPercent(adjustedSlow);

        adjustedSlow = Mathf.Clamp01(adjustedSlow);

        // If this enemy is immune or resistance reduced this to 0, do not store a useless slow.
        if (adjustedSlow <= 0f)
        {
            RecalculateMultiplier();
            return;
        }

        string resolvedFamilyKey = ResolveFamilyKey(sourceInstanceId, familyKey);
        float expireTime = Time.time + duration;

        for (int i = 0; i < activeSlows.Count; i++)
        {
            SlowEntry entry = activeSlows[i];
            if (entry.sourceInstanceId == sourceInstanceId && entry.familyKey == resolvedFamilyKey)
            {
                entry.slowPercent = adjustedSlow;
                entry.expireTime = expireTime;
                RecalculateMultiplier();

                if (debugLogSlowChanges)
                    Debug.Log($"[EnemySlowController] Refreshed slow on {name} family={resolvedFamilyKey} slow={adjustedSlow:0.###}");

                return;
            }
        }

        activeSlows.Add(new SlowEntry
        {
            sourceInstanceId = sourceInstanceId,
            familyKey = resolvedFamilyKey,
            slowPercent = adjustedSlow,
            expireTime = expireTime
        });

        RecalculateMultiplier();

        if (debugLogSlowChanges)
            Debug.Log($"[EnemySlowController] Added slow on {name} family={resolvedFamilyKey} slow={adjustedSlow:0.###}");
    }

    public void ClearAllSlows()
    {
        activeSlows.Clear();
        currentMoveSpeedMultiplier = 1f;
    }

    public bool HasAnyActiveSlow()
    {
        RemoveExpiredSlows();
        return activeSlows.Count > 0;
    }

    private bool RemoveExpiredSlows()
    {
        bool removedAny = false;
        float now = Time.time;

        for (int i = activeSlows.Count - 1; i >= 0; i--)
        {
            if (now >= activeSlows[i].expireTime)
            {
                activeSlows.RemoveAt(i);
                removedAny = true;
            }
        }

        return removedAny;
    }

    private void RecalculateMultiplier()
    {
        strongestByFamily.Clear();

        for (int i = 0; i < activeSlows.Count; i++)
        {
            SlowEntry entry = activeSlows[i];
            if (entry == null)
                continue;

            float clampedSlow = Mathf.Clamp01(entry.slowPercent);
            if (clampedSlow <= 0f)
                continue;

            if (strongestByFamily.TryGetValue(entry.familyKey, out float currentBest))
            {
                if (clampedSlow > currentBest)
                    strongestByFamily[entry.familyKey] = clampedSlow;
            }
            else
            {
                strongestByFamily.Add(entry.familyKey, clampedSlow);
            }
        }

        float multiplier = 1f;

        foreach (var pair in strongestByFamily)
            multiplier *= (1f - pair.Value);

        currentMoveSpeedMultiplier = Mathf.Clamp(multiplier, 0f, 1f);
    }

    private string ResolveFamilyKey(int sourceInstanceId, string familyKey)
    {
        if (!string.IsNullOrWhiteSpace(familyKey))
            return familyKey.Trim();

        // Fallback: if no family key is set, treat this source as its own unique family.
        return $"__SOURCE_{sourceInstanceId}";
    }
}