using System.Collections.Generic;
using DamageNumbersPro;
using UnityEngine;

public class EnemyCombatPopupManager : MonoBehaviour
{
    [Header("Damage Numbers Pro Prefabs")]
    [SerializeField] private DamageNumber damagePopupPrefab;
    [SerializeField] private DamageNumber healPopupPrefab;

    [Header("World Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector3 randomOffsetRange = new Vector3(0.2f, 0.1f, 0.2f);

    [Header("Distance Culling")]
    [SerializeField] private bool requireCameraInRange = true;
    [SerializeField] private float maxCameraDistance = 12f;

    [Header("Formatting")]
    [SerializeField] private bool roundToWholeNumbers = true;
    [SerializeField] private bool showZeroDamage = false;
    [SerializeField] private bool showZeroHealing = false;
    [SerializeField] private bool addPlusToHealing = true;

    private readonly HashSet<EnemyHealth> subscribedHealths = new HashSet<EnemyHealth>();
    private Camera mainCam;

    private void Awake()
    {
        CacheMainCamera();
    }

    private void OnEnable()
    {
        EnemyAgent.OnAnySpawned += HandleEnemySpawned;
        EnemyAgent.OnAnyRemoved += HandleEnemyRemoved;

        SubscribeToExistingEnemies();
    }

    private void OnDisable()
    {
        EnemyAgent.OnAnySpawned -= HandleEnemySpawned;
        EnemyAgent.OnAnyRemoved -= HandleEnemyRemoved;

        UnsubscribeAll();
    }

    private void HandleEnemySpawned(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemy.GetComponentInParent<EnemyHealth>();

        SubscribeToHealth(health);
    }

    private void HandleEnemyRemoved(EnemyAgent enemy)
    {
        if (enemy == null)
            return;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemy.GetComponentInParent<EnemyHealth>();

        UnsubscribeFromHealth(health);
    }

    private void SubscribeToExistingEnemies()
    {
        EnemyHealth[] existing = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
            SubscribeToHealth(existing[i]);
    }

    private void SubscribeToHealth(EnemyHealth health)
    {
        if (health == null)
            return;

        if (!subscribedHealths.Add(health))
            return;

        health.OnDamaged += HandleEnemyDamaged;
        health.OnHealed += HandleEnemyHealed;
    }

    private void UnsubscribeFromHealth(EnemyHealth health)
    {
        if (health == null)
            return;

        if (!subscribedHealths.Remove(health))
            return;

        health.OnDamaged -= HandleEnemyDamaged;
        health.OnHealed -= HandleEnemyHealed;
    }

    private void UnsubscribeAll()
    {
        foreach (EnemyHealth health in subscribedHealths)
        {
            if (health == null)
                continue;

            health.OnDamaged -= HandleEnemyDamaged;
            health.OnHealed -= HandleEnemyHealed;
        }

        subscribedHealths.Clear();
    }

    private void HandleEnemyDamaged(EnemyHealth health, EnemyDamageInfo damageInfo, float finalDamage)
    {
        if (health == null)
            return;

        if (finalDamage <= 0f && !showZeroDamage)
            return;

        if (!IsCameraCloseEnough(health.transform.position))
            return;

        if (damagePopupPrefab == null)
            return;

        float displayAmount = PrepareDisplayAmount(finalDamage);
        Vector3 spawnPos = GetPopupWorldPosition(health.transform.position);

        DamageNumber dn = damagePopupPrefab.Spawn(spawnPos, displayAmount);
        dn.enableNumber = true;
        dn.enableLeftText = false;
        dn.leftText = string.Empty;
    }

    private void HandleEnemyHealed(EnemyHealth health, float healedAmount)
    {
        if (health == null)
            return;

        if (healedAmount <= 0f && !showZeroHealing)
            return;

        if (!IsCameraCloseEnough(health.transform.position))
            return;

        if (healPopupPrefab == null)
            return;

        float displayAmount = PrepareDisplayAmount(healedAmount);
        Vector3 spawnPos = GetPopupWorldPosition(health.transform.position);

        DamageNumber dn = healPopupPrefab.Spawn(spawnPos, displayAmount);
        dn.enableNumber = true;

        if (addPlusToHealing)
        {
            dn.enableLeftText = true;
            dn.leftText = "+";
        }
        else
        {
            dn.enableLeftText = false;
            dn.leftText = string.Empty;
        }
    }

    private float PrepareDisplayAmount(float amount)
    {
        if (roundToWholeNumbers)
            return Mathf.Round(amount);

        return Mathf.Round(amount * 10f) / 10f;
    }

    private Vector3 GetPopupWorldPosition(Vector3 basePosition)
    {
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
            Random.Range(-randomOffsetRange.y, randomOffsetRange.y),
            Random.Range(-randomOffsetRange.z, randomOffsetRange.z));

        return basePosition + worldOffset + randomOffset;
    }

    private bool IsCameraCloseEnough(Vector3 worldPosition)
    {
        if (!requireCameraInRange)
            return true;

        CacheMainCamera();
        if (mainCam == null)
            return false;

        float maxDist = Mathf.Max(0f, maxCameraDistance);
        float distSqr = (mainCam.transform.position - worldPosition).sqrMagnitude;
        return distSqr <= maxDist * maxDist;
    }

    private void CacheMainCamera()
    {
        if (mainCam == null)
            mainCam = Camera.main;
    }
}