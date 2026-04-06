using System;
using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public event Action<EnemyHealth, EnemyDamageInfo, float> OnDamaged;
    public event Action<EnemyHealth, float> OnHealed;
    public event Action<EnemyHealth> OnDied;
    public event Action<EnemyHealth> OnDeathFinalized;

    [Header("Health")]
    [SerializeField] private float maxHP = 20f;
    [SerializeField] private int rewardMoney = 10;

    [Header("Death Animation")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float deathAnimationDuration = 0.35f;
    [SerializeField] private float minAxisRotationSpeed = 220f;
    [SerializeField] private float maxAxisRotationSpeed = 540f;
    [SerializeField] private bool disableAllCollidersOnDeath = true;

    private float hp;
    private bool died;
    private bool deathFinalized;
    private bool rewardGranted;

    private EconomyManager economy;
    private Collider[] cachedColliders;
    private Rigidbody[] cachedRigidbodies;
    private EnemyDeathBehavior[] deathBehaviors;
    private MonoBehaviour[] allBehaviours;

    private Vector3 initialVisualScale;
    private Vector3 deathRotationSpeed;

    public float CurrentHP => hp;
    public float CurrentHealth => hp;
    public float MaxHP => maxHP;
    public float MaxHealth => maxHP;
    public bool IsAlive => !died && hp > 0f;
    public bool IsDead => died;
    public bool IsTargetable => !died;
    public float HealthPercent => maxHP <= 0f ? 0f : hp / maxHP;

    private void Awake()
    {
        hp = Mathf.Max(0.01f, maxHP);
        economy = FindFirstObjectByType<EconomyManager>();

        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedRigidbodies = GetComponentsInChildren<Rigidbody>(true);
        deathBehaviors = GetComponents<EnemyDeathBehavior>();
        allBehaviours = GetComponentsInChildren<MonoBehaviour>(true);

        if (visualRoot == null)
            visualRoot = transform;

        initialVisualScale = visualRoot.localScale;
    }

    public float TakeDamage(float amount)
    {
        EnemyDamageInfo info = new EnemyDamageInfo(amount);
        return TakeDamage(info);
    }

    public float TakeDamage(EnemyDamageInfo damageInfo)
    {
        if (died)
            return 0f;

        if (damageInfo.damage <= 0f)
            return 0f;

        EnemyNearbyDamageSiphon.TryRedirectNearbyDamage(this, ref damageInfo);

        // Let plug-on behaviours modify the hit before final application.
        for (int i = 0; i < allBehaviours.Length; i++)
        {
            if (allBehaviours[i] is IEnemyIncomingDamageModifier modifier)
                modifier.ModifyIncomingDamage(this, ref damageInfo);
        }

        float finalDamage = Mathf.Max(0f, damageInfo.damage);
        if (finalDamage <= 0f)
            return 0f;

        if (!damageInfo.ignoreDamageTakenModifiers)
        {
            EnemyDamageTakenController damageTakenController = GetComponent<EnemyDamageTakenController>();
            if (damageTakenController == null)
                damageTakenController = GetComponentInParent<EnemyDamageTakenController>();

            if (damageTakenController != null)
                finalDamage = damageTakenController.ModifyIncomingDamage(finalDamage);
        }

        finalDamage = Mathf.Max(0f, finalDamage);
        if (finalDamage <= 0f)
            return 0f;

        hp -= finalDamage;
        hp = Mathf.Max(0f, hp);

        OnDamaged?.Invoke(this, damageInfo, finalDamage);

        if (hp <= 0f)
            Die();

        return finalDamage;
    }

    public float Heal(float amount)
    {
        if (died)
            return 0f;

        if (amount <= 0f)
            return 0f;

        float before = hp;
        hp += amount;
        hp = Mathf.Min(hp, maxHP);

        float actualHealed = hp - before;
        if (actualHealed > 0f)
            OnHealed?.Invoke(this, actualHealed);

        return actualHealed;
    }

    private void Die()
    {
        if (died)
            return;

        died = true;

        DisableCombatPresence();
        OnDied?.Invoke(this);

        GrantRewardOnce();

        float maxDeathDelay = 0f;
        for (int i = 0; i < deathBehaviors.Length; i++)
        {
            EnemyDeathBehavior behavior = deathBehaviors[i];
            if (behavior == null || !behavior.enabled)
                continue;

            maxDeathDelay = Mathf.Max(maxDeathDelay, behavior.GetRequiredDelay());
            behavior.TriggerDeath(this);
        }

        deathRotationSpeed = new Vector3(
            UnityEngine.Random.Range(minAxisRotationSpeed, maxAxisRotationSpeed) * RandomSign(),
            UnityEngine.Random.Range(minAxisRotationSpeed, maxAxisRotationSpeed) * RandomSign(),
            UnityEngine.Random.Range(minAxisRotationSpeed, maxAxisRotationSpeed) * RandomSign());

        StartCoroutine(DeathRoutine(maxDeathDelay));
    }

    private IEnumerator DeathRoutine(float finalizeDelay)
    {
        float totalLifetime = Mathf.Max(deathAnimationDuration, finalizeDelay);
        float elapsed = 0f;
        bool finalizedThisRun = false;

        while (elapsed < totalLifetime)
        {
            if (!PauseState.IsPaused)
            {
                elapsed += Time.deltaTime;

                float animT = deathAnimationDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / deathAnimationDuration);
                visualRoot.localScale = Vector3.Lerp(initialVisualScale, Vector3.zero, animT);
                visualRoot.Rotate(deathRotationSpeed * Time.deltaTime, Space.Self);

                if (!finalizedThisRun && elapsed >= finalizeDelay)
                {
                    finalizedThisRun = true;
                    FinalizeDeathForWave();
                }
            }

            yield return null;
        }

        if (!finalizedThisRun)
            FinalizeDeathForWave();

        Destroy(gameObject);
    }

    private void DisableCombatPresence()
    {
        if (disableAllCollidersOnDeath)
        {
            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                    cachedColliders[i].enabled = false;
            }
        }

        for (int i = 0; i < cachedRigidbodies.Length; i++)
        {
            if (cachedRigidbodies[i] == null)
                continue;

            cachedRigidbodies[i].linearVelocity = Vector3.zero;
            cachedRigidbodies[i].angularVelocity = Vector3.zero;
            cachedRigidbodies[i].isKinematic = true;
        }
    }

    private void FinalizeDeathForWave()
    {
        if (deathFinalized)
            return;

        deathFinalized = true;
        OnDeathFinalized?.Invoke(this);
    }

    private void GrantRewardOnce()
    {
        if (rewardGranted)
            return;

        rewardGranted = true;

        if (economy != null && rewardMoney > 0)
            economy.AddMoney(rewardMoney);
    }

    private static int RandomSign()
    {
        return UnityEngine.Random.value < 0.5f ? -1 : 1;
    }
}