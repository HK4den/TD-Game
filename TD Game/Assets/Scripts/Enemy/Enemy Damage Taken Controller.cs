using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamageTakenController : MonoBehaviour
{
    [Serializable]
    private class DamageAmpEntry
    {
        public int sourceInstanceId;
        public string familyKey;
        public float extraDamageTakenPercent;
        public float expireTime;
    }

    [Header("Debug")]
    [SerializeField] private bool debugLogDamageAmpChanges = false;

    private readonly List<DamageAmpEntry> activeEntries = new List<DamageAmpEntry>();
    private readonly Dictionary<string, float> strongestByFamily = new Dictionary<string, float>();

    private EnemyAbilities abilities;
    private float currentDamageTakenMultiplier = 1f;

    public float CurrentDamageTakenMultiplier => currentDamageTakenMultiplier;

    private void Awake()
    {
        abilities = GetComponent<EnemyAbilities>();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        bool removedAny = RemoveExpiredEntries();
        RecalculateMultiplier();

        if (removedAny && debugLogDamageAmpChanges)
            Debug.Log($"[EnemyDamageTakenController] Expired entries removed on {name}. Current multiplier={currentDamageTakenMultiplier:0.###}");
    }

    public void ApplyOrRefreshExtraDamageTaken(int sourceInstanceId, string familyKey, float extraPercent, float duration)
    {
        if (duration <= 0f)
            return;

        float adjustedPercent = extraPercent;
        if (abilities != null)
            adjustedPercent = abilities.AdjustExtraDamageTakenPercent(adjustedPercent);

        adjustedPercent = Mathf.Max(0f, adjustedPercent);

        if (adjustedPercent <= 0f)
        {
            RecalculateMultiplier();
            return;
        }

        string resolvedFamilyKey = ResolveFamilyKey(sourceInstanceId, familyKey);
        float expireTime = Time.time + duration;

        for (int i = 0; i < activeEntries.Count; i++)
        {
            DamageAmpEntry entry = activeEntries[i];
            if (entry.sourceInstanceId == sourceInstanceId && entry.familyKey == resolvedFamilyKey)
            {
                entry.extraDamageTakenPercent = adjustedPercent;
                entry.expireTime = expireTime;
                RecalculateMultiplier();

                if (debugLogDamageAmpChanges)
                    Debug.Log($"[EnemyDamageTakenController] Refreshed on {name} family={resolvedFamilyKey} extra={adjustedPercent:0.###}");

                return;
            }
        }

        activeEntries.Add(new DamageAmpEntry
        {
            sourceInstanceId = sourceInstanceId,
            familyKey = resolvedFamilyKey,
            extraDamageTakenPercent = adjustedPercent,
            expireTime = expireTime
        });

        RecalculateMultiplier();

        if (debugLogDamageAmpChanges)
            Debug.Log($"[EnemyDamageTakenController] Added on {name} family={resolvedFamilyKey} extra={adjustedPercent:0.###}");
    }

    public float ModifyIncomingDamage(float baseDamage)
    {
        return Mathf.Max(0f, baseDamage) * currentDamageTakenMultiplier;
    }

    public void ClearAll()
    {
        activeEntries.Clear();
        currentDamageTakenMultiplier = 1f;
    }

    private bool RemoveExpiredEntries()
    {
        bool removedAny = false;
        float now = Time.time;

        for (int i = activeEntries.Count - 1; i >= 0; i--)
        {
            if (now >= activeEntries[i].expireTime)
            {
                activeEntries.RemoveAt(i);
                removedAny = true;
            }
        }

        return removedAny;
    }

    private void RecalculateMultiplier()
    {
        strongestByFamily.Clear();

        for (int i = 0; i < activeEntries.Count; i++)
        {
            DamageAmpEntry entry = activeEntries[i];
            if (entry == null)
                continue;

            float extra = Mathf.Max(0f, entry.extraDamageTakenPercent);
            if (extra <= 0f)
                continue;

            if (strongestByFamily.TryGetValue(entry.familyKey, out float currentBest))
            {
                if (extra > currentBest)
                    strongestByFamily[entry.familyKey] = extra;
            }
            else
            {
                strongestByFamily.Add(entry.familyKey, extra);
            }
        }

        float multiplier = 1f;

        foreach (var pair in strongestByFamily)
            multiplier *= (1f + pair.Value);

        currentDamageTakenMultiplier = Mathf.Max(1f, multiplier);
    }

    private string ResolveFamilyKey(int sourceInstanceId, string familyKey)
    {
        if (!string.IsNullOrWhiteSpace(familyKey))
            return familyKey.Trim();

        return $"__SOURCE_{sourceInstanceId}";
    }
}