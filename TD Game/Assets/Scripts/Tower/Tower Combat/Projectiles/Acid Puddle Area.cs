using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AcidPuddleArea : MonoBehaviour
{
    private static int globalSpawnCounter = 0;

    [Header("Damage")]
    [SerializeField] private float puddleDamage = 1f;
    [SerializeField] private float tickInterval = 0.75f;
    [SerializeField] private int maxDamageApplications = 20;

    [Header("Lifetime")]
    [SerializeField] private float activeLifetime = 4f;
    [SerializeField] private float growDuration = 0.15f;
    [Range(0f, 1f)]
    [SerializeField] private float damageStartGrowthPercent = 0.75f;
    [SerializeField] private float fadeDuration = 0.20f;

    [Header("Placement")]
    [SerializeField] private float baseHeightAboveGrid = 0.002f;
    [SerializeField] private float overlapHeightStep = 0.00002f;
    [SerializeField] private int overlapHeightVariants = 24;

    [Header("Scale")]
    [SerializeField] private Vector3 finalScale = Vector3.one;

    [Header("Collider")]
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private float colliderHeight = 2f;
    [SerializeField] private LayerMask overlapMask = ~0;

    [Header("Visual")]
    [SerializeField] private Renderer[] targetRenderers;

    private readonly HashSet<EnemyHealth> enemiesInside = new HashSet<EnemyHealth>();
    private readonly Dictionary<EnemyHealth, float> nextTickTimeByEnemy = new Dictionary<EnemyHealth, float>();
    private readonly Collider[] overlapResults = new Collider[64];

    private GridManager gridManager;
    private MaterialPropertyBlock propertyBlock;

    private int damageApplicationsUsed = 0;
    private float stateTimer = 0f;
    private float activeTimer = 0f;
    private bool isGrowing = true;
    private bool isActive = false;
    private bool isFading = false;
    private bool initialized = false;
    private bool scannedInitialOverlaps = false;

    private GameObject sourceObject;
    private bool sourceCanDetectCamo = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly List<float> originalRendererAlphas = new List<float>();

    private void Awake()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<BoxCollider>();

        if (triggerCollider != null)
            triggerCollider.isTrigger = true;

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        propertyBlock = new MaterialPropertyBlock();
        gridManager = FindFirstObjectByType<GridManager>();

        CacheOriginalRendererAlphas();
        transform.localScale = Vector3.zero;
        SetAllRendererAlphaNormalized(1f);
        UpdateColliderShape();
    }

    public void InitializeFromImpactPosition(Vector3 impactPosition, bool sourceCanDetectCamo, GameObject sourceObject)
    {
        this.sourceCanDetectCamo = sourceCanDetectCamo;
        this.sourceObject = sourceObject;

        float baseY = 0.002f;
        if (gridManager != null)
            baseY = gridManager.transform.position.y + baseHeightAboveGrid;

        int stepIndex = globalSpawnCounter++;
        int variants = Mathf.Max(1, overlapHeightVariants);
        float extraY = (stepIndex % variants) * Mathf.Max(0f, overlapHeightStep);

        Vector3 spawnPos = new Vector3(
            impactPosition.x,
            baseY + extraY,
            impactPosition.z
        );

        transform.position = spawnPos;
        transform.localScale = Vector3.zero;
        UpdateColliderShape();

        initialized = true;
    }

    private bool CanAffect(EnemyHealth health)
    {
        return health != null && health.CanBeAffectedByTower(sourceCanDetectCamo);
    }

    private void Update()
    {
        if (PauseState.IsPaused || !initialized)
            return;

        if (isGrowing)
        {
            RunGrow();
            return;
        }

        if (isActive)
        {
            RunActive();
            return;
        }

        if (isFading)
            RunFade();
    }

    private void RunGrow()
    {
        stateTimer += Time.deltaTime;
        float t = growDuration <= 0f ? 1f : Mathf.Clamp01(stateTimer / growDuration);

        transform.localScale = Vector3.LerpUnclamped(Vector3.zero, finalScale, t);
        UpdateColliderShape();

        if (!isActive && t >= Mathf.Clamp01(damageStartGrowthPercent))
            BeginActiveDamage();

        if (isActive)
            RunActive();

        if (t >= 1f)
        {
            isGrowing = false;
            transform.localScale = finalScale;
            UpdateColliderShape();
        }
    }

    private void BeginActiveDamage()
    {
        isActive = true;
        activeTimer = 0f;

        if (!scannedInitialOverlaps)
        {
            scannedInitialOverlaps = true;
            RefreshCurrentOverlaps();
        }
    }

    private void RunActive()
    {
        activeTimer += Time.deltaTime;

        CleanupDeadEnemies();

        if (activeTimer >= activeLifetime || damageApplicationsUsed >= maxDamageApplications)
        {
            BeginFade();
            return;
        }

        float now = Time.time;

        var toDamage = ListPool<EnemyHealth>.Get();
        foreach (EnemyHealth health in enemiesInside)
        {
            if (health == null || !health.IsAlive)
                continue;

            if (!nextTickTimeByEnemy.TryGetValue(health, out float nextTick))
                continue;

            if (now >= nextTick)
                toDamage.Add(health);
        }

        for (int i = 0; i < toDamage.Count; i++)
        {
            if (damageApplicationsUsed >= maxDamageApplications)
                break;

            EnemyHealth health = toDamage[i];
            if (health == null || !health.IsAlive)
                continue;

            nextTickTimeByEnemy[health] = now + Mathf.Max(0.01f, tickInterval);

            if (!CanAffect(health))
                continue;

            health.TakeDamage(new EnemyDamageInfo(
                puddleDamage,
                source: sourceObject,
                canAffectCamo: sourceCanDetectCamo));

            damageApplicationsUsed++;
        }

        ListPool<EnemyHealth>.Release(toDamage);

        if (damageApplicationsUsed >= maxDamageApplications)
            BeginFade();
    }

    private void RunFade()
    {
        stateTimer += Time.deltaTime;

        float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(stateTimer / fadeDuration);
        SetAllRendererAlphaNormalized(1f - t);

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void BeginFade()
    {
        if (isFading)
            return;

        isGrowing = false;
        isActive = false;
        isFading = true;
        stateTimer = 0f;

        if (triggerCollider != null)
            triggerCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!initialized || !isActive)
            return;

        AddEnemyInside(other, true);
    }

    private void AddEnemyInside(Collider other, bool damageImmediately)
    {
        if (other == null)
            return;

        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null || !health.IsAlive)
            return;

        bool newlyAdded = enemiesInside.Add(health);

        if (!nextTickTimeByEnemy.ContainsKey(health))
            nextTickTimeByEnemy.Add(health, Time.time);

        if (!damageImmediately || !newlyAdded)
            return;

        if (damageApplicationsUsed >= maxDamageApplications)
            return;

        if (!CanAffect(health))
            return;

        health.TakeDamage(new EnemyDamageInfo(
            puddleDamage,
            source: sourceObject,
            canAffectCamo: sourceCanDetectCamo));

        damageApplicationsUsed++;
        nextTickTimeByEnemy[health] = Time.time + Mathf.Max(0.01f, tickInterval);

        if (damageApplicationsUsed >= maxDamageApplications)
            BeginFade();
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null)
            return;

        enemiesInside.Remove(health);
        nextTickTimeByEnemy.Remove(health);
    }

    private void RefreshCurrentOverlaps()
    {
        if (triggerCollider == null)
            return;

        int count = GetCurrentOverlapNonAlloc();
        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapResults[i];
            if (hit == null || hit == triggerCollider)
                continue;

            AddEnemyInside(hit, true);

            if (damageApplicationsUsed >= maxDamageApplications)
                break;
        }
    }

    private int GetCurrentOverlapNonAlloc()
    {
        if (triggerCollider is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;

            return Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                overlapResults,
                box.transform.rotation,
                overlapMask,
                QueryTriggerInteraction.Collide);
        }

        Bounds bounds = triggerCollider.bounds;
        return Physics.OverlapBoxNonAlloc(
            bounds.center,
            bounds.extents,
            overlapResults,
            Quaternion.identity,
            overlapMask,
            QueryTriggerInteraction.Collide);
    }

    private void CleanupDeadEnemies()
    {
        var toRemove = ListPool<EnemyHealth>.Get();

        foreach (EnemyHealth health in enemiesInside)
        {
            if (health == null || !health.IsAlive)
                toRemove.Add(health);
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            enemiesInside.Remove(toRemove[i]);
            nextTickTimeByEnemy.Remove(toRemove[i]);
        }

        ListPool<EnemyHealth>.Release(toRemove);
    }

    private void UpdateColliderShape()
    {
        if (triggerCollider == null)
            return;

        if (triggerCollider is BoxCollider box)
        {
            Vector3 scaledSize = finalScale;
            box.size = new Vector3(
                Mathf.Max(0.01f, scaledSize.x),
                Mathf.Max(0.01f, colliderHeight),
                Mathf.Max(0.01f, scaledSize.z)
            );
            box.center = new Vector3(0f, colliderHeight * 0.5f, 0f);
        }
    }

    private void CacheOriginalRendererAlphas()
    {
        originalRendererAlphas.Clear();

        if (targetRenderers == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            float alpha = 1f;

            if (r != null && r.sharedMaterial != null)
            {
                Material sharedMat = r.sharedMaterial;

                if (sharedMat.HasProperty(BaseColorId))
                    alpha = sharedMat.GetColor(BaseColorId).a;
                else if (sharedMat.HasProperty(ColorId))
                    alpha = sharedMat.GetColor(ColorId).a;
            }

            originalRendererAlphas.Add(Mathf.Clamp01(alpha));
        }
    }

    private void SetAllRendererAlphaNormalized(float normalizedAlpha)
    {
        normalizedAlpha = Mathf.Clamp01(normalizedAlpha);

        if (targetRenderers == null)
            return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer r = targetRenderers[i];
            if (r == null)
                continue;

            float baseAlpha = 1f;
            if (i < originalRendererAlphas.Count)
                baseAlpha = originalRendererAlphas[i];

            float finalAlpha = baseAlpha * normalizedAlpha;

            r.GetPropertyBlock(propertyBlock);

            Material sharedMat = r.sharedMaterial;
            if (sharedMat != null)
            {
                if (sharedMat.HasProperty(BaseColorId))
                {
                    Color c = sharedMat.GetColor(BaseColorId);
                    c.a = finalAlpha;
                    propertyBlock.SetColor(BaseColorId, c);
                }
                else if (sharedMat.HasProperty(ColorId))
                {
                    Color c = sharedMat.GetColor(ColorId);
                    c.a = finalAlpha;
                    propertyBlock.SetColor(ColorId, c);
                }
            }

            r.SetPropertyBlock(propertyBlock);
        }
    }

    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> pool = new Stack<List<T>>();

        public static List<T> Get()
        {
            if (pool.Count > 0)
                return pool.Pop();

            return new List<T>();
        }

        public static void Release(List<T> list)
        {
            list.Clear();
            pool.Push(list);
        }
    }
}
