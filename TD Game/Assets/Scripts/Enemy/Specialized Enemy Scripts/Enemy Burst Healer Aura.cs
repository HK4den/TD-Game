using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyBurstHealerAura : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyHealth selfHealth;
    [SerializeField] private EnemyRadiusVisualizer radiusVisualizer;

    [Header("Aura")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private float burstInterval = 2f;

    [Tooltip("Heals this percent of each target's max HP per burst. Example: 0.10 = heal 10% max HP.")]
    [Range(0f, 1f)]
    [SerializeField] private float healPercentOfTargetMaxHealth = 0.10f;

    [Header("Targets")]
    [SerializeField] private bool includeSelf = false;
    [SerializeField] private LayerMask overlapMask = ~0;

    [Header("Radius Visual")]
    [SerializeField] private bool showRadiusOnBurst = true;
    [SerializeField] private float radiusVisibleDuration = 0.35f;

    private readonly Collider[] overlapResults = new Collider[64];
    private readonly HashSet<EnemyHealth> uniqueTargets = new HashSet<EnemyHealth>();

    private float burstTimer;

    private void Awake()
    {
        if (selfHealth == null)
            selfHealth = GetComponent<EnemyHealth>();

        if (radiusVisualizer == null)
            radiusVisualizer = GetComponentInChildren<EnemyRadiusVisualizer>(true);

        if (radiusVisualizer != null)
            radiusVisualizer.SetRadius(radius);
    }

    private void OnEnable()
    {
        burstTimer = Mathf.Max(0.01f, burstInterval);
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (selfHealth == null || selfHealth.IsDead)
            return;

        burstTimer -= Time.deltaTime;
        if (burstTimer > 0f)
            return;

        burstTimer = Mathf.Max(0.01f, burstInterval);
        DoBurstHeal();
    }

    private void DoBurstHeal()
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

            EnemyHealth targetHealth = hit.GetComponentInParent<EnemyHealth>();
            if (targetHealth == null || targetHealth.IsDead)
                continue;

            if (!includeSelf && targetHealth == selfHealth)
                continue;

            uniqueTargets.Add(targetHealth);
        }

        foreach (EnemyHealth target in uniqueTargets)
        {
            if (target == null || target.IsDead)
                continue;

            float amount = target.MaxHealth * Mathf.Clamp01(healPercentOfTargetMaxHealth);
            if (amount <= 0f)
                continue;

            target.Heal(amount);
        }

        if (showRadiusOnBurst && radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(radius);
            radiusVisualizer.ShowForDuration(radiusVisibleDuration);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}