using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDamageReductionAura : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyHealth selfHealth;
    [SerializeField] private EnemyRadiusVisualizer radiusVisualizer;

    [Header("Aura Tick")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float tickInterval = 0.20f;

    [Header("Damage Reduction")]
    [Tooltip("0.25 = targets take 25% less damage.")]
    [Range(0f, 1f)]
    [SerializeField] private float damageReductionPercent = 0.25f;

    [Header("Optional Move Speed Buff")]
    [SerializeField] private bool applyMoveSpeedBonus = false;

    [Tooltip("0.20 = move 20% faster.")]
    [Range(0f, 5f)]
    [SerializeField] private float moveSpeedBonusPercent = 0.20f;

    [Header("Family / Source")]
    [SerializeField] private string familyKeyOverride = string.Empty;

    [Header("Targets")]
    [SerializeField] private bool includeSelf = false;
    [SerializeField] private LayerMask overlapMask = ~0;

    [Header("Radius Visual")]
    [SerializeField] private bool alwaysShowRadius = false;

    private readonly Collider[] overlapResults = new Collider[64];
    private readonly HashSet<EnemyAgent> uniqueTargets = new HashSet<EnemyAgent>();

    private float tickTimer;

    private void Awake()
    {
        if (selfHealth == null)
            selfHealth = GetComponent<EnemyHealth>();

        if (radiusVisualizer == null)
            radiusVisualizer = GetComponentInChildren<EnemyRadiusVisualizer>(true);

        if (radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(radius);
            radiusVisualizer.SetAlwaysVisible(alwaysShowRadius);
        }
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (selfHealth == null || selfHealth.IsDead)
            return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = Mathf.Max(0.01f, tickInterval);
        ApplyAura();
    }

    private void ApplyAura()
    {
        uniqueTargets.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            radius,
            overlapResults,
            overlapMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null)
                continue;

            EnemyAgent enemyAgent = hit.GetComponentInParent<EnemyAgent>();
            if (enemyAgent == null || !enemyAgent.IsTargetable)
                continue;

            EnemyHealth targetHealth = hit.GetComponentInParent<EnemyHealth>();
            if (targetHealth == null || targetHealth.IsDead)
                continue;

            if (!includeSelf && targetHealth == selfHealth)
                continue;

            uniqueTargets.Add(enemyAgent);
        }

        int sourceInstanceId = gameObject.GetInstanceID();
        string resolvedFamilyKey = string.IsNullOrWhiteSpace(familyKeyOverride)
            ? GetType().Name
            : familyKeyOverride.Trim();

        float effectDuration = Mathf.Max(0.05f, tickInterval + 0.05f);
        float signedDamageTakenPercent = -Mathf.Clamp01(damageReductionPercent);

        foreach (EnemyAgent targetAgent in uniqueTargets)
        {
            if (targetAgent == null)
                continue;

            EnemyDamageTakenController damageTaken = targetAgent.GetComponent<EnemyDamageTakenController>();
            if (damageTaken == null)
                damageTaken = targetAgent.GetComponentInParent<EnemyDamageTakenController>();

            if (damageTaken != null)
            {
                damageTaken.ApplyOrRefreshExtraDamageTaken(
                    sourceInstanceId,
                    resolvedFamilyKey,
                    signedDamageTakenPercent,
                    effectDuration);
            }

            if (applyMoveSpeedBonus)
            {
                float speedMultiplier = 1f + Mathf.Max(0f, moveSpeedBonusPercent);

                targetAgent.ApplyOrRefreshMoveSpeedMultiplier(
                    sourceInstanceId,
                    resolvedFamilyKey + "_MoveSpeed",
                    speedMultiplier,
                    effectDuration);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}