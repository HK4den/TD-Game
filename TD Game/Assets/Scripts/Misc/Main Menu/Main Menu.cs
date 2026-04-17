using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
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
}