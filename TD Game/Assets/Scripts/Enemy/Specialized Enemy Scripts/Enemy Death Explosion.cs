using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeathExplosion : EnemyDeathBehavior
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2.5f;
    [SerializeField] private float explosionDelay = 0f;
    [SerializeField] private float damageAmount = 5f;

    [Header("Targets")]
    [SerializeField] private bool canHitOtherEnemies = true;
    [SerializeField] private bool canHitSelf = false;
    [SerializeField] private LayerMask overlapMask = ~0;

    [Header("Damage")]
    [SerializeField] private bool redirectedDamage = false;
    [SerializeField] private bool ignoreDamageTakenModifiers = false;
    [SerializeField] private bool showDamageNumbers = true;

    [Header("Visual + Audio")]
    [SerializeField] private GameObject explosionVisualPrefab;
    [SerializeField] private GameObject explosionSfxPrefab;
    [SerializeField] private Vector3 visualAndSfxOffset = Vector3.zero;
    [SerializeField] private float explosionVisualSize = 1f;

    [Header("Radius Visual")]
    [SerializeField] private EnemyRadiusVisualizer radiusVisualizer;
    [SerializeField] private bool showRadiusOnExplosion = true;
    [SerializeField] private float radiusVisibleDuration = 0.35f;

    private readonly Collider[] overlapResults = new Collider[64];
    private readonly HashSet<EnemyHealth> uniqueTargets = new HashSet<EnemyHealth>();

    public override float GetRequiredDelay()
    {
        return Mathf.Max(0f, explosionDelay);
    }

    public override void TriggerDeath(EnemyHealth health)
    {
        if (!enabled || health == null)
            return;

        StartCoroutine(ExplosionRoutine(health));
    }

    private IEnumerator ExplosionRoutine(EnemyHealth ownerHealth)
    {
        if (explosionDelay > 0f)
        {
            float timer = explosionDelay;
            while (timer > 0f)
            {
                if (!PauseState.IsPaused)
                    timer -= Time.deltaTime;

                yield return null;
            }
        }

        if (ownerHealth == null)
            yield break;

        if (showRadiusOnExplosion && radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(explosionRadius);
            radiusVisualizer.ShowForDuration(radiusVisibleDuration);
        }

        Vector3 explosionPosition = ownerHealth.transform.position + visualAndSfxOffset;
        SpawnExplosionVisual(explosionPosition);
        SpawnExplosionSfx(explosionPosition);

        uniqueTargets.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            ownerHealth.transform.position,
            explosionRadius,
            overlapResults,
            overlapMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null)
                continue;

            EnemyHealth targetHealth = hit.GetComponentInParent<EnemyHealth>();
            if (targetHealth == null)
                continue;

            if (targetHealth.IsDead)
                continue;

            if (!canHitSelf && targetHealth == ownerHealth)
                continue;

            if (!canHitOtherEnemies && targetHealth != ownerHealth)
                continue;

            uniqueTargets.Add(targetHealth);
        }

        EnemyDamageInfo damageInfo = new EnemyDamageInfo(
            damageAmount,
            ignoreDamageTakenModifiers,
            redirectedDamage,
            showDamageNumbers,
            ownerHealth.gameObject);

        foreach (EnemyHealth target in uniqueTargets)
        {
            if (target == null || target.IsDead)
                continue;

            target.TakeDamage(damageInfo);
        }
    }

    private void SpawnExplosionVisual(Vector3 position)
    {
        if (explosionVisualPrefab == null)
            return;

        GameObject visualInstance = Instantiate(explosionVisualPrefab, position, Quaternion.identity);
        float safeSize = Mathf.Max(0f, explosionVisualSize);

        EnemyExplosionSpriteAnimation animation = visualInstance.GetComponent<EnemyExplosionSpriteAnimation>();
        if (animation == null)
            animation = visualInstance.GetComponentInChildren<EnemyExplosionSpriteAnimation>();

        if (animation != null)
            animation.SetScaleMultiplier(safeSize);
        else
            visualInstance.transform.localScale *= safeSize;
    }

    private void SpawnExplosionSfx(Vector3 position)
    {
        if (explosionSfxPrefab == null)
            return;

        Instantiate(explosionSfxPrefab, position, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
