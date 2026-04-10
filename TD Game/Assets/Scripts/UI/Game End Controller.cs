using UnityEngine;

public class GameEndController : MonoBehaviour
{
    public static bool IsGameEnded { get; private set; }

    [Header("Refs")]
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private PlayerLook playerLook;

    [Header("Panels")]
    [SerializeField] private GameObject winPanelRoot;
    [SerializeField] private GameObject losePanelRoot;

    [Header("Cursor")]
    [SerializeField] private bool showCursorOnEnd = true;

    private float originalFixedDeltaTime;

    private void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;
        IsGameEnded = false;

        if (baseHealth == null)
            baseHealth = FindFirstObjectByType<BaseHealth>();

        if (waveSpawner == null)
            waveSpawner = FindFirstObjectByType<WaveSpawner>();

        if (playerLook == null)
            playerLook = FindFirstObjectByType<PlayerLook>();

        if (winPanelRoot != null)
            winPanelRoot.SetActive(false);

        if (losePanelRoot != null)
            losePanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (baseHealth != null)
            baseHealth.OnBaseDestroyed += HandleLose;

        if (waveSpawner != null)
            waveSpawner.OnWaveCompleted += HandleWaveCompleted;
    }

    private void OnDisable()
    {
        if (baseHealth != null)
            baseHealth.OnBaseDestroyed -= HandleLose;

        if (waveSpawner != null)
            waveSpawner.OnWaveCompleted -= HandleWaveCompleted;
    }

    private void HandleLose()
    {
        if (IsGameEnded)
            return;

        EndGame(false);
    }

    private void HandleWaveCompleted(int completedWaveNumber, int reward)
    {
        if (IsGameEnded)
            return;

        if (waveSpawner == null)
            return;

        if (completedWaveNumber >= waveSpawner.TotalWaves)
            EndGame(true);
    }

    private void EndGame(bool won)
    {
        IsGameEnded = true;

        // Force the regular pause state off so the pause menu does not stay open.
        PauseState.SetPaused(false);

        if (winPanelRoot != null)
            winPanelRoot.SetActive(won);

        if (losePanelRoot != null)
            losePanelRoot.SetActive(!won);

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;

        if (playerLook != null)
        {
            playerLook.SetLookBlocked(true);
            playerLook.UnlockCursorForUI();
        }
        else if (showCursorOnEnd)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        Debug.Log(won ? "YOU WIN" : "YOU LOSE");
    }

    public static void ResetGameEndState()
    {
        IsGameEnded = false;
    }

    private void OnDestroy()
    {
        if (!Application.isPlaying)
            return;

        if (!IsGameEnded)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }
}