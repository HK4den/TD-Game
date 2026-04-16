using UnityEngine;

[RequireComponent(typeof(TowerCombatStats))]
[RequireComponent(typeof(TowerTargetingController))]
[RequireComponent(typeof(TowerProjectileEmitter))]
public class TowerAttackController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TowerCombatStats combatStats;
    [SerializeField] private TowerTargetingController targetingController;
    [SerializeField] private TowerProjectileEmitter projectileEmitter;

    [Header("Debug")]
    [SerializeField] private bool debugLogAttacks = false;

    private float cooldownRemaining;

    private void Awake()
    {
        if (combatStats == null)
            combatStats = GetComponent<TowerCombatStats>();

        if (targetingController == null)
            targetingController = GetComponent<TowerTargetingController>();

        if (projectileEmitter == null)
            projectileEmitter = GetComponent<TowerProjectileEmitter>();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        TickCooldown();

        if (projectileEmitter == null || projectileEmitter.IsEmitting)
            return;

        if (cooldownRemaining > 0f)
            return;

        TryStartAttack();
    }

    private void TickCooldown()
    {
        if (cooldownRemaining <= 0f)
            return;

        cooldownRemaining -= Time.deltaTime;
        if (cooldownRemaining < 0f)
            cooldownRemaining = 0f;
    }

    private void TryStartAttack()
    {
        if (targetingController == null || combatStats == null || projectileEmitter == null)
            return;

        EnemyAgent lockedTarget = null;

        if (targetingController.IsNoneTower)
        {
            if (!targetingController.HasAnyEnemyInRange())
                return;
        }
        else
        {
            lockedTarget = targetingController.GetCurrentTarget();
            if (lockedTarget == null)
                return;
        }

        bool started = projectileEmitter.TryBeginAttack(lockedTarget);
        if (!started)
            return;

        cooldownRemaining = Mathf.Max(0.001f, combatStats.SecondsBetweenShots);

        if (debugLogAttacks)
        {
            string targetName = lockedTarget != null ? lockedTarget.name : "(none)";
            //Debug.Log($"[TowerAttackController] Attack started by {name} target={targetName}");
        }
    }

    public void ResetCooldown()
    {
        cooldownRemaining = 0f;
    }

    public void SetCooldownFromCurrentStats()
    {
        if (combatStats == null)
            return;

        cooldownRemaining = Mathf.Max(0.001f, combatStats.SecondsBetweenShots);
    }
}