using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyNearbyDamageSiphon : MonoBehaviour
{
    private static readonly List<EnemyNearbyDamageSiphon> ActiveSiphoners = new List<EnemyNearbyDamageSiphon>(64);

    [Header("Refs")]
    [SerializeField] private EnemyHealth selfHealth;
    [SerializeField] private EnemyAgent selfAgent;
    [SerializeField] private EnemyRadiusVisualizer radiusVisualizer;

    [Header("Siphon")]
    [SerializeField] private float siphonRadius = 3f;

    [Tooltip("0.50 = siphon 50% of incoming damage from nearby enemies. 1.00 = siphon 100%.")]
    [Range(0f, 1f)]
    [SerializeField] private float siphonPercent = 0.50f;

    [Header("Radius Visual")]
    [SerializeField] private bool alwaysShowRadius = true;

    public float SiphonRadius => Mathf.Max(0f, siphonRadius);
    public float SiphonPercent => Mathf.Clamp01(siphonPercent);
    public bool CanCurrentlySiphon => enabled && gameObject.activeInHierarchy && selfHealth != null && !selfHealth.IsDead;

    private void Awake()
    {
        if (selfHealth == null)
            selfHealth = GetComponent<EnemyHealth>();

        if (selfAgent == null)
            selfAgent = GetComponent<EnemyAgent>();

        if (radiusVisualizer == null)
            radiusVisualizer = GetComponentInChildren<EnemyRadiusVisualizer>(true);

        if (radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(siphonRadius);
            radiusVisualizer.SetAlwaysVisible(alwaysShowRadius);
        }
    }

    private void OnEnable()
    {
        if (!ActiveSiphoners.Contains(this))
            ActiveSiphoners.Add(this);
    }

    private void OnDisable()
    {
        ActiveSiphoners.Remove(this);
    }

    private void OnDestroy()
    {
        ActiveSiphoners.Remove(this);
    }

    private void LateUpdate()
    {
        if (radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(siphonRadius);
            radiusVisualizer.SetAlwaysVisible(alwaysShowRadius);
        }
    }

    public static void TryRedirectNearbyDamage(EnemyHealth originalTarget, ref EnemyDamageInfo damageInfo)
    {
        if (originalTarget == null)
            return;

        if (damageInfo.damage <= 0f)
            return;

        if (damageInfo.isRedirectedDamage)
            return;

        // Siphoners never siphon from other siphoners.
        if (originalTarget.GetComponent<EnemyNearbyDamageSiphon>() != null ||
            originalTarget.GetComponentInParent<EnemyNearbyDamageSiphon>() != null)
        {
            return;
        }

        EnemyNearbyDamageSiphon bestSiphoner = FindBestSiphonerForTarget(originalTarget);
        if (bestSiphoner == null)
            return;

        EnemyHealth siphonerHealth = bestSiphoner.selfHealth;
        if (siphonerHealth == null || siphonerHealth.IsDead)
            return;

        float percent = Mathf.Clamp01(bestSiphoner.siphonPercent);
        if (percent <= 0f)
            return;

        float redirectedAmount = damageInfo.damage * percent;
        redirectedAmount = Mathf.Max(0f, redirectedAmount);

        if (redirectedAmount <= 0f)
            return;

        damageInfo.damage = Mathf.Max(0f, damageInfo.damage - redirectedAmount);

        EnemyDamageInfo redirectedInfo = new EnemyDamageInfo(
            redirectedAmount,
            damageInfo.ignoreDamageTakenModifiers,
            true,
            damageInfo.showDamageNumber,
            damageInfo.source != null ? damageInfo.source : originalTarget.gameObject);

        siphonerHealth.TakeDamage(redirectedInfo);
    }

    private static EnemyNearbyDamageSiphon FindBestSiphonerForTarget(EnemyHealth originalTarget)
    {
        if (originalTarget == null)
            return null;

        Vector3 targetPos = originalTarget.transform.position;

        EnemyNearbyDamageSiphon best = null;
        float bestPercent = -1f;
        float bestDistanceSqr = float.MaxValue;

        for (int i = ActiveSiphoners.Count - 1; i >= 0; i--)
        {
            EnemyNearbyDamageSiphon candidate = ActiveSiphoners[i];
            if (candidate == null)
                continue;

            if (!candidate.CanCurrentlySiphon)
                continue;

            if (candidate.selfHealth == originalTarget)
                continue;

            float radius = Mathf.Max(0f, candidate.siphonRadius);
            if (radius <= 0f)
                continue;

            Vector3 siphonerPos = candidate.transform.position;
            float distanceSqr = (siphonerPos - targetPos).sqrMagnitude;
            float radiusSqr = radius * radius;

            if (distanceSqr > radiusSqr)
                continue;

            float percent = Mathf.Clamp01(candidate.siphonPercent);

            bool isBetter = false;

            if (percent > bestPercent)
            {
                isBetter = true;
            }
            else if (Mathf.Approximately(percent, bestPercent) && distanceSqr < bestDistanceSqr)
            {
                isBetter = true;
            }

            if (isBetter)
            {
                best = candidate;
                bestPercent = percent;
                bestDistanceSqr = distanceSqr;
            }
        }

        return best;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.8f, 1f);
        Gizmos.DrawWireSphere(transform.position, siphonRadius);
    }
}