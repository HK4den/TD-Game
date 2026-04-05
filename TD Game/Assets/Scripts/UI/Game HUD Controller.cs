using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameHUDController : MonoBehaviour
{
    [Header("UI (Legacy Text)")]
    [SerializeField] private Text moneyText;
    [SerializeField] private Text hpText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text statusText;

    [Header("Refs")]
    [SerializeField] private EconomyManager economy;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private WaveSpawner waveSpawner;

    private int totalWaves;
    private int currentWaveInProgress = 0;
    private int nextWaveToStart = 1;

    private void Awake()
    {
        if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
        if (baseHealth == null) baseHealth = FindFirstObjectByType<BaseHealth>();
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<WaveSpawner>();

        totalWaves = (waveSpawner != null) ? waveSpawner.TotalWaves : 0;

        RefreshMoney();
        RefreshHP(baseHealth != null ? baseHealth.HP : 0, baseHealth != null ? baseHealth.MaxHP : 0);
        RefreshWaveAndStatus();
    }

    private void OnEnable()
    {
        if (economy != null)
            economy.OnMoneyChanged += OnMoneyChanged;

        if (baseHealth != null)
            baseHealth.OnHealthChanged += OnHealthChanged;

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStarted += OnWaveStarted;
            waveSpawner.OnWaveCompleted += OnWaveCompleted;
        }
    }

    private void OnDisable()
    {
        if (economy != null)
            economy.OnMoneyChanged -= OnMoneyChanged;

        if (baseHealth != null)
            baseHealth.OnHealthChanged -= OnHealthChanged;

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStarted -= OnWaveStarted;
            waveSpawner.OnWaveCompleted -= OnWaveCompleted;
        }
    }

    private void Update()
    {
        if (PauseState.IsPaused) return;

        var kb = Keyboard.current;
        if (kb != null && kb.enterKey.wasPressedThisFrame)
        {
            if (waveSpawner != null && currentWaveInProgress == 0 && nextWaveToStart <= totalWaves)
            {
                waveSpawner.StartNextWave();
            }
        }
    }

    private void OnMoneyChanged(int newMoney)
    {
        RefreshMoney();
    }

    private void OnHealthChanged(int hp, int maxHp)
    {
        RefreshHP(hp, maxHp);
    }

    private void OnWaveStarted(int waveNumber)
    {
        currentWaveInProgress = waveNumber;
        RefreshWaveAndStatus();
    }

    private void OnWaveCompleted(int waveNumber, int reward)
    {
        currentWaveInProgress = 0;
        nextWaveToStart = Mathf.Min(waveNumber + 1, totalWaves);
        RefreshWaveAndStatus();
    }

    private void RefreshMoney()
    {
        if (moneyText == null) return;
        moneyText.text = economy != null ? $"{FormatMoney(economy.Money)}" : "?";
    }

    private void RefreshHP(int hp, int maxHp)
    {
        if (hpText == null) return;
        hpText.text = $"HP: {hp}";
    }

    private void RefreshWaveAndStatus()
    {
        if (waveText != null)
        {
            int displayWave = (currentWaveInProgress != 0)
                ? currentWaveInProgress
                : nextWaveToStart;

            if (totalWaves > 0)
                displayWave = Mathf.Clamp(displayWave, 1, totalWaves);

            waveText.text = $"Wave: {displayWave} / {totalWaves}";
        }

        if (statusText != null)
        {
            statusText.text = (currentWaveInProgress != 0)
                ? "Wave in Progress"
                : "Start Wave (Press Enter)";
        }
    }

    private string FormatMoney(int amount)
    {
        if (amount < 0)
            return $"-${Mathf.Abs(amount)}";

        return $"${amount}";
    }
}