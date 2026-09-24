using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ReloadOnEscape : MonoBehaviour
{
    void Update()
    {
        if (!PauseState.IsPaused && Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Tower Defense Test");
        }
    }
}
