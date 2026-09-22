using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TowerCombatStats))]
[RequireComponent(typeof(TowerRangeProfile))]
public class TowerAuraGrantCamoDetection : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TowerCombatStats sourceCombatStats;
    [SerializeField] private TowerRangeProfile rangeProfile;

    [Header("Tick")]
    [SerializeField] private float refreshInterval = 0.20f;

    [Header("Rules")]
    [SerializeField] private bool includeSelf = false;
    [Min(0)] [SerializeField] private int grantedDetectionLevels = 1;

    private readonly HashSet<TowerCombatStats> grantedTargets = new HashSet<TowerCombatStats>();
    private readonly HashSet<TowerCombatStats> currentTargets = new HashSet<TowerCombatStats>();

    private float tickTimer;
    private int sourceId;

    private void Awake()
    {
        if (sourceCombatStats == null)
            sourceCombatStats = GetComponent<TowerCombatStats>();

        if (rangeProfile == null)
            rangeProfile = GetComponent<TowerRangeProfile>();

        sourceId = GetInstanceID();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = Mathf.Max(0.01f, refreshInterval);
        RefreshAura();
    }

    private void RefreshAura()
    {
        currentTargets.Clear();

        IReadOnlyList<TowerCombatStats> allTowers = TowerCombatStats.ActiveTowers;
        for (int i = 0; i < allTowers.Count; i++)
        {
            TowerCombatStats target = allTowers[i];
            if (target == null)
                continue;

            if (!includeSelf && target == sourceCombatStats)
                continue;

            if (IsTowerInAuraRange(target.transform.position))
                currentTargets.Add(target);
        }

        foreach (TowerCombatStats target in grantedTargets)
        {
            if (target == null || !currentTargets.Contains(target))
            {
                if (target != null)
                    target.RemoveGrantedCamoSource(sourceId);

            }
        }

        grantedTargets.Clear();
        foreach (TowerCombatStats target in currentTargets)
        {
            if (target == null)
                continue;

            target.AddGrantedCamoSource(sourceId, grantedDetectionLevels);

            grantedTargets.Add(target);
        }
    }

    private bool IsTowerInAuraRange(Vector3 targetPosition)
    {
        float effectiveRange = sourceCombatStats.Range;
        float baseRange = sourceCombatStats.BaseRange;

        switch (rangeProfile.Shape)
        {
            case TowerRangeProfile.RangeShape.Sphere:
                return IsInsideSphere(targetPosition, effectiveRange);

            case TowerRangeProfile.RangeShape.SingleBox:
                return IsInsideSingleBox(targetPosition, effectiveRange, baseRange);

            case TowerRangeProfile.RangeShape.MultiBox:
                return IsInsideAnyMultiBox(targetPosition, effectiveRange, baseRange);
        }

        return false;
    }

    private bool IsInsideSphere(Vector3 targetPos, float radius)
    {
        Vector3 center = transform.position;
        center.y = targetPos.y;

        float sqrDistance = (targetPos - center).sqrMagnitude;
        return sqrDistance <= radius * radius;
    }

    private bool IsInsideSingleBox(Vector3 targetPos, float effectiveRange, float baseRange)
    {
        Vector3 localCenter = rangeProfile.GetExtendedSingleBoxCenter(effectiveRange, baseRange);
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Vector3 worldHalfExtents = rangeProfile.GetExtendedSingleBoxSize(effectiveRange, baseRange) * 0.5f;
        Quaternion rotation = transform.rotation;

        return IsPointInsideOrientedBox(targetPos, worldCenter, worldHalfExtents, rotation);
    }

    private bool IsInsideAnyMultiBox(Vector3 targetPos, float effectiveRange, float baseRange)
    {
        IReadOnlyList<TowerRangeProfile.BoxRangeDefinition> defs = rangeProfile.MultiBoxDefinitions;
        for (int i = 0; i < defs.Count; i++)
        {
            TowerRangeProfile.BoxRangeDefinition def = defs[i];
            if (def == null)
                continue;

            Vector3 localCenter = rangeProfile.GetExtendedMultiBoxCenter(def, effectiveRange, baseRange);
            Vector3 size = rangeProfile.GetExtendedMultiBoxSize(def, effectiveRange, baseRange);

            Vector3 worldCenter = transform.TransformPoint(localCenter);
            Vector3 halfExtents = size * 0.5f;
            Quaternion rotation = transform.rotation;

            if (IsPointInsideOrientedBox(targetPos, worldCenter, halfExtents, rotation))
                return true;
        }

        return false;
    }

    private bool IsPointInsideOrientedBox(Vector3 point, Vector3 boxCenter, Vector3 halfExtents, Quaternion boxRotation)
    {
        Vector3 local = Quaternion.Inverse(boxRotation) * (point - boxCenter);

        return Mathf.Abs(local.x) <= halfExtents.x
            && Mathf.Abs(local.y) <= halfExtents.y
            && Mathf.Abs(local.z) <= halfExtents.z;
    }

    private void OnDisable()
    {
        foreach (TowerCombatStats target in grantedTargets)
        {
            if (target != null)
                target.RemoveGrantedCamoSource(sourceId);
        }

        grantedTargets.Clear();
        currentTargets.Clear();
    }

    private void OnDestroy()
    {
        OnDisable();
    }
}
