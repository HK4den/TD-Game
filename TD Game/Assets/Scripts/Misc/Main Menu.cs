using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Load the main game scene
    public void StartGame()
    {
        SceneManager.LoadScene("Tower Defense Test"); // Replace with your actual scene name
    }

    // Load the cutscene scene
    public void RestartCutscene()
    {
        SceneManager.LoadScene("Opening"); // Replace with your actual scene name
    }

    // Quit the game
    public void QuitGame()
    {
        Debug.Log("Quit Game"); // Helps in editor testing
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}