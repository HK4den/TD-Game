using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TowerRangeQuery))]
public class TowerTargetingController : MonoBehaviour
{
    [Header("Mode Rules")]
    [SerializeField] private bool useTargetingModes = true;
    [SerializeField] private TowerTargetingMode defaultTargetingMode = TowerTargetingMode.First;

    [Header("Refs")]
    [SerializeField] private TowerRangeQuery rangeQuery;

    private readonly List<EnemyAgent> reusableCandidates = new List<EnemyAgent>(32);

    public bool UsesTargetingModes => useTargetingModes;
    public bool IsNoneTower => !useTargetingModes;

    public TowerTargetingMode CurrentMode { get; private set; }

    private void Awake()
    {
        if (rangeQuery == null)
            rangeQuery = GetComponent<TowerRangeQuery>();

        if (!useTargetingModes)
        {
            CurrentMode = TowerTargetingMode.None;
        }
        else
        {
            if (defaultTargetingMode == TowerTargetingMode.None)
                defaultTargetingMode = TowerTargetingMode.First;

            CurrentMode = defaultTargetingMode;
        }
    }

    public bool CanCycleTargetingMode()
    {
        return useTargetingModes;
    }

    public TowerTargetingMode CycleForward()
    {
        if (!useTargetingModes)
            return TowerTargetingMode.None;

        CurrentMode = GetNextSelectableMode(CurrentMode);
        return CurrentMode;
    }

    public EnemyAgent GetCurrentTarget()
    {
        if (!useTargetingModes)
            return null;

        List<EnemyAgent> inRange = rangeQuery.GetEnemiesInRange();
        if (inRange == null || inRange.Count == 0)
            return null;

        reusableCandidates.Clear();
        for (int i = 0; i < inRange.Count; i++)
        {
            EnemyAgent enemy = inRange[i];
            if (enemy == null)
                continue;

            reusableCandidates.Add(enemy);
        }

        if (reusableCandidates.Count == 0)
            return null;

        switch (CurrentMode)
        {
            case TowerTargetingMode.First:
                return SelectFirst(reusableCandidates);

            case TowerTargetingMode.Last:
                return SelectLast(reusableCandidates);

            case TowerTargetingMode.Close:
                return SelectClosest(reusableCandidates);

            case TowerTargetingMode.Strong:
                return SelectStrongest(reusableCandidates);

            case TowerTargetingMode.Weak:
                return SelectWeakest(reusableCandidates);

            case TowerTargetingMode.None:
            default:
                return null;
        }
    }

    public bool HasAnyEnemyInRange()
    {
        if (rangeQuery == null)
            return false;

        return rangeQuery.HasAnyEnemyInRange();
    }

    public List<EnemyAgent> GetEnemiesInRange()
    {
        if (rangeQuery == null)
        {
            reusableCandidates.Clear();
            return reusableCandidates;
        }

        return rangeQuery.GetEnemiesInRange();
    }

    private TowerTargetingMode GetNextSelectableMode(TowerTargetingMode current)
    {
        switch (current)
        {
            case TowerTargetingMode.First:
                return TowerTargetingMode.Last;

            case TowerTargetingMode.Last:
                return TowerTargetingMode.Close;

            case TowerTargetingMode.Close:
                return TowerTargetingMode.Strong;

            case TowerTargetingMode.Strong:
                return TowerTargetingMode.Weak;

            case TowerTargetingMode.Weak:
            default:
                return TowerTargetingMode.First;
        }
    }

    private EnemyAgent SelectFirst(List<EnemyAgent> candidates)
    {
        EnemyAgent best = null;
        float bestRemaining = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyAgent enemy = candidates[i];
            if (enemy == null)
                continue;

            float remaining = enemy.RemainingPathDistance;
            if (remaining < bestRemaining)
            {
                bestRemaining = remaining;
                best = enemy;
            }
        }

        return best;
    }

    private EnemyAgent SelectLast(List<EnemyAgent> candidates)
    {
        EnemyAgent best = null;
        float bestRemaining = float.MinValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyAgent enemy = candidates[i];
            if (enemy == null)
                continue;

            float remaining = enemy.RemainingPathDistance;
            if (remaining > bestRemaining)
            {
                bestRemaining = remaining;
                best = enemy;
            }
        }

        return best;
    }

    private EnemyAgent SelectClosest(List<EnemyAgent> candidates)
    {
        EnemyAgent best = null;
        float bestSqrDistance = float.MaxValue;
        Vector3 towerPos = transform.position;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyAgent enemy = candidates[i];
            if (enemy == null)
                continue;

            float sqrDistance = (enemy.transform.position - towerPos).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = enemy;
            }
        }

        return best;
    }

    private EnemyAgent SelectStrongest(List<EnemyAgent> candidates)
    {
        EnemyAgent best = null;
        float bestHp = float.MinValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyAgent enemy = candidates[i];
            if (enemy == null)
                continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null || !health.IsAlive)
                continue;

            if (health.CurrentHP > bestHp)
            {
                bestHp = health.CurrentHP;
                best = enemy;
            }
        }

        return best;
    }

    private EnemyAgent SelectWeakest(List<EnemyAgent> candidates)
    {
        EnemyAgent best = null;
        float bestHp = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            EnemyAgent enemy = candidates[i];
            if (enemy == null)
                continue;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health == null || !health.IsAlive)
                continue;

            if (health.CurrentHP < bestHp)
            {
                bestHp = health.CurrentHP;
                best = enemy;
            }
        }

        return best;
    }
}