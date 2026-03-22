using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TowerCombatStats))]
public class TowerProjectileEmitter : MonoBehaviour
{
    public enum FireDirectionMode
    {
        FixedFirePointForward = 0,
        AimAtTargetWithoutVisualRotate = 1,
        AimAtTargetWithVisualRotate = 2
    }

    [Header("Refs")]
    [SerializeField] private TowerCombatStats combatStats;
    [SerializeField] private TowerRotationController rotationController;

    [Header("Projectile")]
    [SerializeField] private TowerProjectile projectilePrefab;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifetime = 8f;
    [SerializeField] private TowerProjectile.EffectMode effectMode = TowerProjectile.EffectMode.Damage;

    [Header("Fire Points")]
    [SerializeField] private List<Transform> firePoints = new List<Transform>();
    [SerializeField] private bool useAllFirePointsPerShot = true;

    [Header("Attack Timing")]
    [SerializeField] private float delayBeforeFirstShot = 0f;
    [SerializeField] private int burstCount = 1;
    [SerializeField] private float delayBetweenBurstShots = 0f;

    [Header("Behavior")]
    [SerializeField] private FireDirectionMode fireDirectionMode = FireDirectionMode.FixedFirePointForward;

    private Coroutine attackRoutine;

    public bool IsEmitting => attackRoutine != null;

    private void Awake()
    {
        if (combatStats == null)
            combatStats = GetComponent<TowerCombatStats>();

        if (rotationController == null)
            rotationController = GetComponent<TowerRotationController>();
    }

    public bool TryBeginAttack(EnemyAgent attackTarget)
    {
        if (attackRoutine != null)
            return false;

        attackRoutine = StartCoroutine(AttackRoutine(attackTarget));
        return true;
    }

    private IEnumerator AttackRoutine(EnemyAgent lockedTarget)
    {
        if (fireDirectionMode == FireDirectionMode.AimAtTargetWithVisualRotate &&
            rotationController != null &&
            lockedTarget != null)
        {
            rotationController.SnapAimAtWorldPoint(lockedTarget.transform.position);
        }

        if (delayBeforeFirstShot > 0f)
            yield return WaitForSecondsGameplay(delayBeforeFirstShot);

        int shots = Mathf.Max(1, burstCount);

        for (int burstIndex = 0; burstIndex < shots; burstIndex++)
        {
            FireBurstShot(lockedTarget);

            if (burstIndex < shots - 1 && delayBetweenBurstShots > 0f)
                yield return WaitForSecondsGameplay(delayBetweenBurstShots);
        }

        attackRoutine = null;
    }

    private void FireBurstShot(EnemyAgent lockedTarget)
    {
        if (projectilePrefab == null || combatStats == null)
            return;

        List<Transform> points = GetValidFirePoints();
        if (points.Count == 0)
            points.Add(transform);

        if (useAllFirePointsPerShot)
        {
            for (int i = 0; i < points.Count; i++)
                SpawnProjectileFromPoint(points[i], lockedTarget);
        }
        else
        {
            SpawnProjectileFromPoint(points[0], lockedTarget);
        }
    }

    private void SpawnProjectileFromPoint(Transform firePoint, EnemyAgent lockedTarget)
    {
        if (firePoint == null || projectilePrefab == null)
            return;

        Vector3 spawnPos = firePoint.position;
        Vector3 direction = ResolveShotDirection(firePoint, lockedTarget);

        TowerProjectile projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        projectile.Initialize(
            direction,
            projectileSpeed,
            combatStats.Pierce,
            combatStats.Power,
            effectMode,
            projectileLifetime);
    }

    private Vector3 ResolveShotDirection(Transform firePoint, EnemyAgent lockedTarget)
    {
        switch (fireDirectionMode)
        {
            case FireDirectionMode.FixedFirePointForward:
                return firePoint.forward;

            case FireDirectionMode.AimAtTargetWithoutVisualRotate:
            case FireDirectionMode.AimAtTargetWithVisualRotate:
                {
                    if (lockedTarget != null)
                    {
                        Vector3 toTarget = lockedTarget.transform.position - firePoint.position;
                        if (toTarget.sqrMagnitude > 0.0001f)
                            return toTarget.normalized;
                    }

                    return firePoint.forward;
                }

            default:
                return firePoint.forward;
        }
    }

    private List<Transform> GetValidFirePoints()
    {
        List<Transform> results = new List<Transform>();

        for (int i = 0; i < firePoints.Count; i++)
        {
            if (firePoints[i] != null)
                results.Add(firePoints[i]);
        }

        return results;
    }

    private IEnumerator WaitForSecondsGameplay(float seconds)
    {
        float timer = 0f;

        while (timer < seconds)
        {
            if (!PauseState.IsPaused)
                timer += Time.deltaTime;

            yield return null;
        }
    }
}