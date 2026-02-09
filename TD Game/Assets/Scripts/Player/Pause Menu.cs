using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot; // the pause panel
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnResume = true;

    private float originalFixedDeltaTime;

    private void Awake()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);

        PauseState.OnPauseChanged += HandlePauseChanged;

        // start unpaused
        HandlePauseChanged(false);
    }

    private void OnDestroy()
    {
        PauseState.OnPauseChanged -= HandlePauseChanged;
    }

    private void Update()
    {
        // Escape toggles pause
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PauseState.Toggle();
        }
    }

    private void HandlePauseChanged(bool paused)
    {
        if (panelRoot != null)
            panelRoot.SetActive(paused);

        if (paused)
        {
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0f;

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
        PauseState.SetPaused(false);
    }

    public void Quit()
    {
        // Works in builds
        Application.Quit();

#if UNITY_EDITOR
        // Works in editor play mode
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
