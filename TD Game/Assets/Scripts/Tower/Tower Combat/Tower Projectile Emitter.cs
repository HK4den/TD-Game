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
    [SerializeField] private TowerIdentity towerIdentity;
    [SerializeField] private TowerVisualSquash visualSquash;

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

    [Header("Aim Rules")]
    [SerializeField] private bool flattenAimedShotsToXZPlane = true;
    [SerializeField] private bool flattenFixedForwardShotsToXZPlane = false;

    private Coroutine attackRoutine;
    private readonly List<Transform> reusableFirePoints = new List<Transform>(8);

    public bool IsEmitting => attackRoutine != null;

    private void Awake()
    {
        if (combatStats == null)
            combatStats = GetComponent<TowerCombatStats>();

        if (rotationController == null)
            rotationController = GetComponent<TowerRotationController>();

        if (towerIdentity == null)
            towerIdentity = GetComponent<TowerIdentity>();

        if (towerIdentity == null)
            towerIdentity = GetComponentInChildren<TowerIdentity>();

        if (visualSquash == null)
            visualSquash = GetComponent<TowerVisualSquash>();
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
        if (visualSquash != null)
            visualSquash.TriggerFirePulse();

        if (projectilePrefab == null || combatStats == null)
            return;

        GetValidFirePointsNonAlloc(reusableFirePoints);

        if (reusableFirePoints.Count == 0)
            reusableFirePoints.Add(transform);

        if (useAllFirePointsPerShot)
        {
            for (int i = 0; i < reusableFirePoints.Count; i++)
                SpawnProjectileFromPoint(reusableFirePoints[i], lockedTarget);
        }
        else
        {
            SpawnProjectileFromPoint(reusableFirePoints[0], lockedTarget);
        }
    }

    private void SpawnProjectileFromPoint(Transform firePoint, EnemyAgent lockedTarget)
    {
        if (firePoint == null || projectilePrefab == null)
            return;

        Vector3 spawnPos = firePoint.position;
        Vector3 direction = ResolveShotDirection(firePoint, lockedTarget);

        if (direction.sqrMagnitude <= 0.0001f)
            direction = firePoint.forward;

        string familyKey = towerIdentity != null ? towerIdentity.TowerFamilyKey : string.Empty;
        int sourceInstanceId = transform.root.gameObject.GetInstanceID();

        TowerProjectile projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        projectile.Initialize(
            direction,
            projectileSpeed,
            combatStats.Pierce,
            combatStats.Power,
            effectMode,
            projectileLifetime,
            familyKey,
            sourceInstanceId);
    }

    private Vector3 ResolveShotDirection(Transform firePoint, EnemyAgent lockedTarget)
    {
        switch (fireDirectionMode)
        {
            case FireDirectionMode.FixedFirePointForward:
                {
                    Vector3 dir = firePoint.forward;
                    if (flattenFixedForwardShotsToXZPlane)
                        dir.y = 0f;

                    if (dir.sqrMagnitude <= 0.0001f)
                        dir = Vector3.forward;

                    return dir.normalized;
                }

            case FireDirectionMode.AimAtTargetWithoutVisualRotate:
            case FireDirectionMode.AimAtTargetWithVisualRotate:
                {
                    if (lockedTarget != null)
                    {
                        Vector3 toTarget = lockedTarget.transform.position - firePoint.position;

                        if (flattenAimedShotsToXZPlane)
                            toTarget.y = 0f;

                        if (toTarget.sqrMagnitude > 0.0001f)
                            return toTarget.normalized;
                    }

                    Vector3 fallback = firePoint.forward;
                    if (flattenAimedShotsToXZPlane)
                        fallback.y = 0f;

                    if (fallback.sqrMagnitude <= 0.0001f)
                        fallback = Vector3.forward;

                    return fallback.normalized;
                }

            default:
                return firePoint.forward.normalized;
        }
    }

    private void GetValidFirePointsNonAlloc(List<Transform> results)
    {
        results.Clear();

        for (int i = 0; i < firePoints.Count; i++)
        {
            if (firePoints[i] != null)
                results.Add(firePoints[i]);
        }
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