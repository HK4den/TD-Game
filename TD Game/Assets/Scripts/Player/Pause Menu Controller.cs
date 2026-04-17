using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanelRoot;
    [SerializeField] private GameObject settingsPanelRoot;

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitToMenuButton;
    [SerializeField] private Button quitGameButton;

    [Header("Settings Buttons")]
    [SerializeField] private Button backButton;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnResume = true;

    private float originalFixedDeltaTime;

    private void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(QuitToMenu);
        if (quitGameButton != null) quitGameButton.onClick.AddListener(QuitGame);
        if (backButton != null) backButton.onClick.AddListener(CloseSettings);

        PauseState.OnPauseChanged += HandlePauseChanged;

        HandlePauseChanged(false);
    }

    private void OnDestroy()
    {
        PauseState.OnPauseChanged -= HandlePauseChanged;
    }

    private void Update()
    {
        if (GameEndController.IsGameEnded)
        {
            if (pausePanelRoot != null && pausePanelRoot.activeSelf)
                pausePanelRoot.SetActive(false);

            if (settingsPanelRoot != null && settingsPanelRoot.activeSelf)
                settingsPanelRoot.SetActive(false);

            return;
        }

        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanelRoot != null && settingsPanelRoot.activeSelf)
            {
                CloseSettings();
                return;
            }

            PauseState.Toggle();
        }
    }

    private void HandlePauseChanged(bool paused)
    {
        if (GameEndController.IsGameEnded)
        {
            if (pausePanelRoot != null)
                pausePanelRoot.SetActive(false);

            if (settingsPanelRoot != null)
                settingsPanelRoot.SetActive(false);

            return;
        }

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(paused);

        if (!paused && settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);

        if (paused)
        {
            Time.timeScale = 0f;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;

            if (lockCursorOnResume)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    public void Resume()
    {
        if (GameEndController.IsGameEnded) return;
        PauseState.SetPaused(false);
    }

    public void OpenSettings()
    {
        if (GameEndController.IsGameEnded) return;

        if (pausePanelRoot != null)
            pausePanelRoot.SetActive(false);

        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseSettings()
    {
        if (settingsPanelRoot != null)
            settingsPanelRoot.SetActive(false);

        if (GameEndController.IsGameEnded)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        if (PauseState.IsPaused)
        {
            if (pausePanelRoot != null)
                pausePanelRoot.SetActive(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (lockCursorOnResume)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
        PauseState.SetPaused(false);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}