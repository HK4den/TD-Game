using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TowerProjectile : MonoBehaviour
{
    public enum EffectMode
    {
        Damage = 0,
        Heal = 1
    }

    [Header("Runtime")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float effectAmount = 1f;
    [SerializeField] private int remainingPierce = 1;
    [SerializeField] private float maxLifetime = 8f;
    [SerializeField] private EffectMode effectMode = EffectMode.Damage;

    [Header("Optional Slow On Hit")]
    [SerializeField] private bool applySlowOnHit = false;
    [Range(0f, 1f)]
    [SerializeField] private float slowPercent = 0.10f;
    [SerializeField] private float slowDuration = 1f;

    [Header("Optional Acid Puddle On Hit")]
    [SerializeField] private bool spawnAcidPuddleOnHit = false;
    [SerializeField] private AcidPuddleArea acidPuddlePrefab;

    private static readonly Dictionary<(int, int), Stack<TowerProjectile>> pools = new Dictionary<(int, int), Stack<TowerProjectile>>();
    private (int, int) poolKey;
    private bool pooled;
    private TrailRenderer[] trails;
    private ParticleSystem[] particles;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPools()
    {
        pools.Clear();
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= ClearScenePools;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += ClearScenePools;
    }

    private static void ClearScenePools(UnityEngine.SceneManagement.Scene scene)
    {
        var keys = new System.Collections.Generic.List<(int, int)>();
        foreach (var key in pools.Keys)
            if (key.Item1 == scene.handle) keys.Add(key);
        foreach (var key in keys) pools.Remove(key);
    }

    public static TowerProjectile Spawn(TowerProjectile prefab, Vector3 position, Quaternion rotation, UnityEngine.SceneManagement.Scene scene)
    {
        var key = (scene.handle, prefab.GetInstanceID());
        if (!pools.TryGetValue(key, out Stack<TowerProjectile> pool))
        {
            pool = new Stack<TowerProjectile>();
            pools.Add(key, pool);
        }
        TowerProjectile instance = null;
        while (pool.Count > 0 && instance == null) instance = pool.Pop();
        if (instance == null)
        {
            instance = Instantiate(prefab, position, rotation);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance.gameObject, scene);
        }
        instance.poolKey = key;
        instance.pooled = true;
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.transform.localScale = prefab.transform.localScale;
        instance.gameObject.SetActive(true);
        foreach (TrailRenderer trail in instance.trails) trail.Clear();
        foreach (ParticleSystem particle in instance.particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (particle.main.playOnAwake) particle.Play();
        }
        return instance;
    }

    private Vector3 moveDirection = Vector3.forward;
    private float lifetimeTimer;

    private string slowFamilyKey = string.Empty;
    private int slowSourceInstanceId = 0;
    private GameObject sourceObject;
    private int sourceCamoDetectionLevel;

    private Collider ownCollider;
    private Rigidbody rb;

    private bool isSpent = false;

    private readonly HashSet<EnemyHealth> alreadyHit = new HashSet<EnemyHealth>();

    private void Awake()
    {
        trails = GetComponentsInChildren<TrailRenderer>(true);
        particles = GetComponentsInChildren<ParticleSystem>(true);
        ownCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    public void Initialize(
        Vector3 direction,
        float moveSpeed,
        int pierce,
        float amount,
        EffectMode mode,
        float lifetime,
        string sourceFamilyKey,
        int sourceInstanceId,
        GameObject sourceObject,
        int sourceCamoDetectionLevel)
    {
        moveDirection = direction.sqrMagnitude <= 0.0001f ? Vector3.forward : direction.normalized;
        speed = Mathf.Max(0.01f, moveSpeed);
        remainingPierce = Mathf.Max(1, pierce);
        effectAmount = Mathf.Max(0.001f, amount);
        effectMode = mode;
        maxLifetime = Mathf.Max(0.1f, lifetime);

        slowFamilyKey = sourceFamilyKey ?? string.Empty;
        slowSourceInstanceId = sourceInstanceId;
        this.sourceObject = sourceObject;
        this.sourceCamoDetectionLevel = Mathf.Max(0, sourceCamoDetectionLevel);

        lifetimeTimer = 0f;
        isSpent = false;
        alreadyHit.Clear();

        if (ownCollider != null)
            ownCollider.enabled = true;

        transform.forward = moveDirection;
    }

    private void Update()
    {
        if (PauseState.IsPaused || isSpent)
            return;

        transform.position += moveDirection * speed * Time.deltaTime;

        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
            DestroyProjectile();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.collider == null)
            return;

        TryHit(collision.collider);
    }

    private void TryHit(Collider other)
    {
        if (PauseState.IsPaused || isSpent || other == null || other == ownCollider)
            return;

        EnemyHealth health = other.GetComponentInParent<EnemyHealth>();
        if (health == null || !health.IsAlive)
            return;

        if (!health.CanBeAffectedByTower(sourceCamoDetectionLevel))
            return;

        if (alreadyHit.Contains(health))
            return;

        alreadyHit.Add(health);

        switch (effectMode)
        {
            case EffectMode.Heal:
                health.Heal(effectAmount);
                break;

            case EffectMode.Damage:
            default:
                health.TakeDamage(new EnemyDamageInfo(
                    effectAmount,
                    source: sourceObject,
                    camoDetectionLevel: sourceCamoDetectionLevel));
                break;
        }

        if (applySlowOnHit)
        {
            EnemySlowController slowController = health.GetComponentInParent<EnemySlowController>();
            if (slowController != null)
            {
                slowController.ApplyOrRefreshSlow(
                    slowSourceInstanceId,
                    slowFamilyKey,
                    slowPercent,
                    slowDuration);
            }
        }

        if (spawnAcidPuddleOnHit && acidPuddlePrefab != null && effectMode == EffectMode.Damage)
        {
            AcidPuddleArea puddle = Instantiate(acidPuddlePrefab);
            puddle.InitializeFromImpactPosition(health.transform.position, sourceCamoDetectionLevel, sourceObject);
        }

        remainingPierce--;

        if (remainingPierce <= 0)
            DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        if (isSpent)
            return;

        isSpent = true;

        if (ownCollider != null)
            ownCollider.enabled = false;

        sourceObject = null;
        alreadyHit.Clear();
        if (pooled && pools.TryGetValue(poolKey, out Stack<TowerProjectile> pool) && pool.Count < 128)
        {
            gameObject.SetActive(false);
            pool.Push(this);
        }
        else
            Destroy(gameObject);
    }
}
