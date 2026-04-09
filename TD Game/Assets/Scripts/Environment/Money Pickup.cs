using UnityEngine;

public class MoneyPickup : MonoBehaviour
{
    [Header("Who Can Collect")]
    [SerializeField] private string playerTag = "Player";

    [Header("Reward")]
    [SerializeField] private int moneyAmount = 25;

    [Header("Refs")]
    [SerializeField] private EconomyManager economy;
    [SerializeField] private EndOfWaveIncomeSummaryManager incomeSummaryManager;

    [Header("Collect SFX")]
    [SerializeField] private GameObject collectSoundPrefab;

    [Header("Behavior")]
    [SerializeField] private bool disableInsteadOfDestroy = false;

    private bool collected;

    private void Awake()
    {
        if (economy == null)
            economy = FindFirstObjectByType<EconomyManager>();

        if (incomeSummaryManager == null)
            incomeSummaryManager = FindFirstObjectByType<EndOfWaveIncomeSummaryManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        if (!other.CompareTag(playerTag))
            return;

        Collect();
    }

    private void Collect()
    {
        collected = true;

        if (economy != null)
            economy.AddMoney(moneyAmount);
        else
            Debug.LogWarning($"[{name}] No EconomyManager found.");

        if (incomeSummaryManager != null)
            incomeSummaryManager.SpawnPickupMoneyPopup(moneyAmount);
        else
            Debug.LogWarning($"[{name}] No EndOfWaveIncomeSummaryManager found.");

        SpawnCollectSoundObject();

        if (disableInsteadOfDestroy)
            gameObject.SetActive(false);
        else
            Destroy(gameObject);
    }

    private void SpawnCollectSoundObject()
    {
        if (collectSoundPrefab == null)
            return;

        Instantiate(collectSoundPrefab, transform.position, Quaternion.identity);
    }
}