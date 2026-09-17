using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyCamoAura : MonoBehaviour
{
    [Header("Aura")]
    [Min(0f)] [SerializeField] private float radius = 2.5f;
    [Range(1, 3)] [SerializeField] private int camoLevel = 1;
    [Min(0.01f)] [SerializeField] private float refreshInterval = 0.2f;
    [SerializeField] private bool includeSelf = false;

    [Header("Radius Visual")]
    [SerializeField] private EnemyRadiusVisualizer radiusVisualizer;
    [SerializeField] private bool alwaysShowRadius = true;

    private EnemyHealth selfHealth;
    private float timer;
    private readonly HashSet<EnemyAgent> grantedTargets = new HashSet<EnemyAgent>();
    private readonly HashSet<EnemyAgent> currentTargets = new HashSet<EnemyAgent>();

    private void Awake()
    {
        selfHealth = GetComponent<EnemyHealth>();
        if (radiusVisualizer == null)
            radiusVisualizer = GetComponentInChildren<EnemyRadiusVisualizer>(true);
        selfHealth.OnDied += OnDied;
    }

    private void OnEnable()
    {
        timer = 0f;
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;
        if (selfHealth == null || !selfHealth.IsAlive)
        {
            ClearGrants();
            return;
        }

        if (radiusVisualizer != null)
        {
            radiusVisualizer.SetRadius(radius);
            radiusVisualizer.SetAlwaysVisible(alwaysShowRadius);
        }
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;
        timer = Mathf.Max(0.01f, refreshInterval);
        currentTargets.Clear();
        float radiusSquared = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        foreach (EnemyAgent target in FindObjectsByType<EnemyAgent>(FindObjectsSortMode.None))
        {
            if (!target.isActiveAndEnabled || target.HasReachedGoal)
                continue;
            EnemyHealth health = target.GetComponent<EnemyHealth>();
            if (health == null || !health.IsAlive || (!includeSelf && health == selfHealth))
                continue;
            Vector3 offset = target.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > radiusSquared)
                continue;
            currentTargets.Add(target);
            target.SetCamoAuraSource(GetInstanceID(), camoLevel);
        }

        foreach (EnemyAgent previous in grantedTargets)
            if (previous != null && !currentTargets.Contains(previous))
                previous.RemoveCamoAuraSource(GetInstanceID());
        grantedTargets.Clear();
        grantedTargets.UnionWith(currentTargets);
    }

    private void OnDied(EnemyHealth health)
    {
        ClearGrants();
    }

    private void ClearGrants()
    {
        foreach (EnemyAgent target in grantedTargets)
            if (target != null)
                target.RemoveCamoAuraSource(GetInstanceID());
        grantedTargets.Clear();
    }

    private void OnDisable()
    {
        ClearGrants();
    }

    private void OnDestroy()
    {
        ClearGrants();
        if (selfHealth != null)
            selfHealth.OnDied -= OnDied;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 previous = transform.position + Vector3.right * radius;
        for (int segment = 1; segment <= 64; segment++)
        {
            float angle = segment * Mathf.PI * 2f / 64f;
            Vector3 next = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
