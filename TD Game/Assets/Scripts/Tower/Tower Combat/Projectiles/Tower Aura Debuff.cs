using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TowerRangeQuery))]
public class TowerAuraDebuff : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TowerRangeQuery rangeQuery;
    [SerializeField] private TowerIdentity towerIdentity;

    [Header("Tick")]
    [SerializeField] private float auraTickInterval = 0.20f;

    [Header("Aura Slow")]
    [SerializeField] private bool applySlow = true;
    [Range(0f, 1f)]
    [SerializeField] private float slowPercent = 0.15f;

    [Header("Aura Extra Damage Taken")]
    [SerializeField] private bool applyExtraDamageTaken = false;
    [Range(0f, 10f)]
    [SerializeField] private float extraDamageTakenPercent = 0.10f;

    private readonly List<EnemyAgent> enemiesInRange = new List<EnemyAgent>(32);
    private float tickTimer;

    private void Awake()
    {
        if (rangeQuery == null)
            rangeQuery = GetComponent<TowerRangeQuery>();

        if (towerIdentity == null)
            towerIdentity = GetComponent<TowerIdentity>();

        if (towerIdentity == null)
            towerIdentity = GetComponentInChildren<TowerIdentity>();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        tickTimer -= Time.deltaTime;
        if (tickTimer > 0f)
            return;

        tickTimer = Mathf.Max(0.01f, auraTickInterval);
        ApplyAura();
    }

    private void ApplyAura()
    {
        enemiesInRange.Clear();

        List<EnemyAgent> queriedEnemies = rangeQuery.GetEnemiesInRange();
        for (int i = 0; i < queriedEnemies.Count; i++)
        {
            enemiesInRange.Add(queriedEnemies[i]);
        }

        string familyKey = towerIdentity != null ? towerIdentity.TowerFamilyKey : string.Empty;
        int sourceInstanceId = transform.root.gameObject.GetInstanceID();
        float effectDuration = Mathf.Max(0.05f, auraTickInterval + 0.05f);

        for (int i = 0; i < enemiesInRange.Count; i++)
        {
            EnemyAgent enemy = enemiesInRange[i];
            if (enemy == null)
                continue;

            if (applySlow)
            {
                EnemySlowController slowController = enemy.GetComponent<EnemySlowController>();
                if (slowController == null)
                    slowController = enemy.GetComponentInParent<EnemySlowController>();

                if (slowController != null)
                {
                    slowController.ApplyOrRefreshSlow(
                        sourceInstanceId,
                        familyKey,
                        slowPercent,
                        effectDuration);
                }
            }

            if (applyExtraDamageTaken)
            {
                EnemyDamageTakenController damageTakenController = enemy.GetComponent<EnemyDamageTakenController>();
                if (damageTakenController == null)
                    damageTakenController = enemy.GetComponentInParent<EnemyDamageTakenController>();

                if (damageTakenController != null)
                {
                    damageTakenController.ApplyOrRefreshExtraDamageTaken(
                        sourceInstanceId,
                        familyKey,
                        extraDamageTakenPercent,
                        effectDuration);
                }
            }
        }
    }
}