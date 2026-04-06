using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemySelfRegen : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyHealth enemyHealth;

    [Header("Regen")]
    [Tooltip("Heals this percent of max HP each tick. Example: 0.10 = heal 10% max HP.")]
    [Range(0f, 1f)]
    [SerializeField] private float healPercentOfMaxPerTick = 0.10f;

    [SerializeField] private float tickInterval = 1f;

    [Header("Damage Cooldown")]
    [SerializeField] private bool requireDelayAfterDamage = true;
    [SerializeField] private float delayAfterTakingDamage = 1.5f;

    [Header("Behavior")]
    [SerializeField] private bool allowRegenAtFullHealth = false;
    [SerializeField] private bool startReadyImmediately = true;

    private float tickTimer;
    private float nextAllowedRegenTime;

    private void Awake()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged += HandleDamaged;
            enemyHealth.OnDied += HandleDied;
        }

        tickTimer = Mathf.Max(0.01f, tickInterval);

        if (startReadyImmediately)
            nextAllowedRegenTime = Time.time;
        else
            nextAllowedRegenTime = Time.time + Mathf.Max(0f, delayAfterTakingDamage);
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged -= HandleDamaged;
            enemyHealth.OnDied -= HandleDied;
        }
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (enemyHealth == null || enemyHealth.IsDead)
            return;

        if (requireDelayAfterDamage && Time.time < nextAllowedRegenTime)
            return;

        if (!allowRegenAtFullHealth && enemyHealth.CurrentHealth >= enemyHealth.MaxHealth)
            return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = Mathf.Max(0.01f, tickInterval);
        ApplyRegenTick();
    }

    private void ApplyRegenTick()
    {
        if (enemyHealth == null || enemyHealth.IsDead)
            return;

        float amount = enemyHealth.MaxHealth * Mathf.Clamp01(healPercentOfMaxPerTick);
        if (amount <= 0f)
            return;

        enemyHealth.Heal(amount);
    }

    private void HandleDamaged(EnemyHealth health, EnemyDamageInfo damageInfo, float finalDamage)
    {
        tickTimer = Mathf.Max(0.01f, tickInterval);

        if (requireDelayAfterDamage)
            nextAllowedRegenTime = Time.time + Mathf.Max(0f, delayAfterTakingDamage);
    }

    private void HandleDied(EnemyHealth health)
    {
        enabled = false;
    }
}