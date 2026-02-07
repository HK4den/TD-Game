using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class ReloadOnEscape : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.backquoteKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Tower Defense Test");
        }
    }
}
