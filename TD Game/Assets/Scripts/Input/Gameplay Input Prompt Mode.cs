using System;
using UnityEngine.InputSystem;

public enum GameplayInputPromptMode
{
    KeyboardMouse,
    Controller
}

public static class GameplayInputPromptModeTracker
{
    public static event Action<GameplayInputPromptMode> OnModeChanged;

    private static bool initialized;
    private static GameplayInputPromptMode currentMode = GameplayInputPromptMode.KeyboardMouse;

    public static GameplayInputPromptMode CurrentMode
    {
        get
        {
            EnsureInitialized();
            return currentMode;
        }
    }

    public static bool IsController
    {
        get
        {
            EnsureInitialized();
            return currentMode == GameplayInputPromptMode.Controller;
        }
    }

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;
        InputSystem.onActionChange += HandleActionChange;
    }

    private static void HandleActionChange(object actionObject, InputActionChange change)
    {
        if (change != InputActionChange.ActionPerformed)
            return;

        if (!(actionObject is InputAction action))
            return;

        if (action.actionMap == null || action.actionMap.name != "Player")
            return;

        InputControl control = action.activeControl;
        if (control == null)
            return;

        InputDevice device = control.device;
        if (device is Gamepad)
        {
            SetMode(GameplayInputPromptMode.Controller);
            return;
        }

        if (device is Keyboard || device is Mouse)
            SetMode(GameplayInputPromptMode.KeyboardMouse);
    }

    private static void SetMode(GameplayInputPromptMode mode)
    {
        if (currentMode == mode)
            return;

        currentMode = mode;
        OnModeChanged?.Invoke(currentMode);
    }
}
