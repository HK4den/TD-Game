using UnityEngine;

public class AutoUnlockCursor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool unlockCursor = true;
    [SerializeField] private bool showCursor = true;

    private void Awake()
    {
        ApplyCursorState();
    }

    private void Start()
    {
        // Extra safety in case something else overrides it in Awake
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        if (unlockCursor)
            Cursor.lockState = CursorLockMode.None;
        else
            Cursor.lockState = CursorLockMode.Locked;

        Cursor.visible = showCursor;
    }
}