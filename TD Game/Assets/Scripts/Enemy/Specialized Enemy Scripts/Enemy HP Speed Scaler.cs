using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyAgent))]
public class EnemyHpSpeedScaler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyAgent enemyAgent;

    [Header("HP -> Speed Multiplier")]
    [Tooltip("X axis = current HP percent (0 = dead/empty, 1 = full HP). Y axis = speed multiplier.")]
    [SerializeField]
    private AnimationCurve hpToSpeedMultiplier = new AnimationCurve(
        new Keyframe(0f, 1.5f),
        new Keyframe(1f, 1f)
    );

    [Header("Limits")]
    [SerializeField] private float minMultiplier = 0f;
    [SerializeField] private float maxMultiplier = 3f;

    [Header("Behavior")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool resetToOneOnDeath = true;

    private void Awake()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyAgent == null)
            enemyAgent = GetComponent<EnemyAgent>();
    }

    private void OnEnable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged += HandleHealthChanged;
            enemyHealth.OnHealed += HandleHealed;
            enemyHealth.OnDied += HandleDied;
        }

        if (applyOnAwake)
            RefreshMultiplier();
    }

    private void OnDisable()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnDamaged -= HandleHealthChanged;
            enemyHealth.OnHealed -= HandleHealed;
            enemyHealth.OnDied -= HandleDied;
        }
    }

    private void HandleHealthChanged(EnemyHealth health, EnemyDamageInfo damageInfo, float finalDamage)
    {
        RefreshMultiplier();
    }

    private void HandleHealed(EnemyHealth health, float healedAmount)
    {
        RefreshMultiplier();
    }

    private void HandleDied(EnemyHealth health)
    {
        if (enemyAgent == null)
            return;

        if (resetToOneOnDeath)
            enemyAgent.ClearHpSpeedMultiplier();
        else
            RefreshMultiplier();
    }

    [ContextMenu("Refresh Multiplier")]
    public void RefreshMultiplier()
    {
        if (enemyHealth == null || enemyAgent == null)
            return;

        float hpPercent = Mathf.Clamp01(enemyHealth.HealthPercent);
        float evaluated = hpToSpeedMultiplier != null ? hpToSpeedMultiplier.Evaluate(hpPercent) : 1f;
        float finalMultiplier = Mathf.Clamp(evaluated, minMultiplier, maxMultiplier);

        enemyAgent.SetHpSpeedMultiplier(finalMultiplier);
    }
}