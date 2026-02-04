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
        var kb = Keyboard.current;
        if (kb == null || teleportTarget == null) return;

        if (kb.tKey.wasPressedThisFrame)
        {
            // Disable controller so it doesn't snap back
            controller.enabled = false;

            // Teleport in world space
            transform.position = teleportTarget.position;

            // Re-enable controller
            controller.enabled = true;
        }
    }
}
