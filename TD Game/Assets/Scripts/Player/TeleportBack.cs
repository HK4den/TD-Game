using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportBack : MonoBehaviour
{
    public Transform teleportTarget;

    CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (PauseState.IsPaused) return;

        var kb = Keyboard.current;
        if (kb == null || teleportTarget == null) return;

        if (kb.tKey.wasPressedThisFrame)
        {
            controller.enabled = false;
            transform.position = teleportTarget.position;
            controller.enabled = true;
        }
    }

}
