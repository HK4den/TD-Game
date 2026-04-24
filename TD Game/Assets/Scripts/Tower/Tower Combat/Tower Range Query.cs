using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TowerCombatStats))]
[RequireComponent(typeof(TowerRangeProfile))]
public class TowerRangeQuery : MonoBehaviour
{
    [SerializeField] private TowerCombatStats combatStats;
    [SerializeField] private TowerRangeProfile rangeProfile;

    private readonly List<EnemyAgent> reusableResults = new List<EnemyAgent>(32);

    public TowerCombatStats CombatStats => combatStats;
    public TowerRangeProfile RangeProfile => rangeProfile;

    private void Awake()
    {
        if (combatStats == null)
            combatStats = GetComponent<TowerCombatStats>();

        if (rangeProfile == null)
            rangeProfile = GetComponent<TowerRangeProfile>();
    }

    public List<EnemyAgent> GetEnemiesInRange()
    {
        reusableResults.Clear();

        IReadOnlyList<EnemyAgent> enemies = EnemyRegistry.AliveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyAgent enemy = enemies[i];
            if (enemy == null)
                continue;

            if (IsEnemyInRange(enemy))
                reusableResults.Add(enemy);
        }

        return reusableResults;
    }

    public bool HasAnyEnemyInRange()
    {
        IReadOnlyList<EnemyAgent> enemies = EnemyRegistry.AliveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyAgent enemy = enemies[i];
            if (enemy == null)
                continue;

            if (IsEnemyInRange(enemy))
                return true;
        }

        return false;
    }

    public bool IsEnemyInRange(EnemyAgent enemy)
    {
        if (enemy == null || combatStats == null || rangeProfile == null)
            return false;

        if (!CanTargetEnemy(enemy))
            return false;

        Vector3 enemyPos = enemy.transform.position;
        float effectiveRange = combatStats.Range;
        float baseRange = combatStats.BaseRange;

        switch (rangeProfile.Shape)
        {
            case TowerRangeProfile.RangeShape.Sphere:
                return IsInsideSphere(enemyPos, effectiveRange);

            case TowerRangeProfile.RangeShape.SingleBox:
                return IsInsideSingleBox(enemyPos, effectiveRange, baseRange);

            case TowerRangeProfile.RangeShape.MultiBox:
                return IsInsideAnyMultiBox(enemyPos, effectiveRange, baseRange);
        }

        return false;
    }

    private bool CanTargetEnemy(EnemyAgent enemy)
    {
        if (enemy == null)
            return false;

        if (!enemy.IsTargetable)
            return false;

        if (enemy.IsBeamProtected)
            return false;

        if (enemy.IsCamoHidden && !combatStats.CanDetectCamo)
            return false;

        return true;
    }

    private bool IsInsideSphere(Vector3 enemyPos, float radius)
    {
        Vector3 center = transform.position;
        center.y = enemyPos.y;

        float sqrDistance = (enemyPos - center).sqrMagnitude;
        return sqrDistance <= radius * radius;
    }

    private bool IsInsideSingleBox(Vector3 enemyPos, float effectiveRange, float baseRange)
    {
        Vector3 localCenter = rangeProfile.GetExtendedSingleBoxCenter(effectiveRange, baseRange);
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        Vector3 worldHalfExtents = rangeProfile.GetExtendedSingleBoxSize(effectiveRange, baseRange) * 0.5f;
        Quaternion rotation = transform.rotation;

        return IsPointInsideOrientedBox(enemyPos, worldCenter, worldHalfExtents, rotation);
    }

    private bool IsInsideAnyMultiBox(Vector3 enemyPos, float effectiveRange, float baseRange)
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

            if (IsPointInsideOrientedBox(enemyPos, worldCenter, halfExtents, rotation))
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
}