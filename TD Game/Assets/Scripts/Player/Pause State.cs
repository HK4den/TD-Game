using System;

public static class PauseState
{
    public static bool IsPaused { get; private set; }

    public static event Action<bool> OnPauseChanged;

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;
        OnPauseChanged?.Invoke(IsPaused);
    }

    public static void Toggle()
    {
        SetPaused(!IsPaused);
    }
}
