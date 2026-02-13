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

    private void Awake()
    {
        if (economy == null) economy = FindFirstObjectByType<EconomyManager>();
        if (baseHealth == null) baseHealth = FindFirstObjectByType<BaseHealth>();
        if (waveSpawner == null) waveSpawner = FindFirstObjectByType<WaveSpawner>();
    }

    private void OnEnable()
    {
        if (economy != null) economy.OnMoneyChanged += OnMoneyChanged;
        if (baseHealth != null) baseHealth.OnHealthChanged += OnHealthChanged;

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStarted += OnWaveStarted;
            waveSpawner.OnWaveCompleted += OnWaveCompleted;
        }

        RefreshAll();
    }

    private void OnDisable()
    {
        if (economy != null) economy.OnMoneyChanged -= OnMoneyChanged;
        if (baseHealth != null) baseHealth.OnHealthChanged -= OnHealthChanged;

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveStarted -= OnWaveStarted;
            waveSpawner.OnWaveCompleted -= OnWaveCompleted;
        }
    }

    private void Update()
    {
        if (PauseState.IsPaused) return;

        // Press Enter to start next wave IF not currently in progress
        var kb = Keyboard.current;
        if (kb != null && kb.enterKey.wasPressedThisFrame)
        {
            if (waveSpawner != null && !waveSpawner.IsWaveInProgress)
            {
                waveSpawner.StartNextWave();
                RefreshWaveAndStatus(); // immediate UI update
            }
        }
    }

    private void OnMoneyChanged(int newMoney) => RefreshMoney();
    private void OnHealthChanged(int hp, int maxHp) => RefreshHP();

    private void OnWaveStarted(int waveNumber)
    {
        RefreshWaveAndStatus();
    }

    private void OnWaveCompleted(int waveNumber, int reward)
    {
        // Grant per-wave reward on completion
        if (economy != null && reward > 0)
            economy.AddMoney(reward);

        RefreshWaveAndStatus();
    }

    private void RefreshAll()
    {
        RefreshMoney();
        RefreshHP();
        RefreshWaveAndStatus();
    }

    private void RefreshMoney()
    {
        if (moneyText == null) return;

        if (economy == null) moneyText.text = "Money: ?";
        else moneyText.text = $"Money: {economy.Money}";
    }

    private void RefreshHP()
    {
        if (hpText == null) return;

        if (baseHealth == null) hpText.text = "HP: ?";
        else hpText.text = $"HP: {baseHealth.HP}";
    }

    private void RefreshWaveAndStatus()
    {
        if (waveSpawner == null)
        {
            if (waveText != null) waveText.text = "Wave: ? / ?";
            if (statusText != null) statusText.text = "";
            return;
        }

        int total = waveSpawner.TotalWaves;
        int next = waveSpawner.NextWaveNumber;

        // Your requested format: "Wave: X / Y" where X is the wave about to start
        if (waveText != null)
            waveText.text = $"Wave: {next} / {total}";

        if (statusText != null)
        {
            if (waveSpawner.IsWaveInProgress)
                statusText.text = "Wave in Progress";
            else
                statusText.text = "Start Wave (Press Enter)";
        }
    }
}
