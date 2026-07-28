using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Controller Navigation")]
    [SerializeField] private Selectable firstSelected;

    private void Start()
    {
        SelectInitialButton();
    }

    private void ResetTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    public void StartGame()
    {
        ResetTime();
        SceneManager.LoadScene("Tower Defense Test");
    }

    public void RestartCutscene()
    {
        ResetTime();
        SceneManager.LoadScene("Opening");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void MainMenuTeleport()
    {
        ResetTime();
        SceneManager.LoadScene("MainMenu");
    }

    private void SelectInitialButton()
    {
        if (EventSystem.current == null)
            return;

        Selectable target = firstSelected != null ? firstSelected : FindFirstObjectByType<Selectable>();

        if (target == null || !target.gameObject.activeInHierarchy || !target.interactable)
            return;

        EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}
