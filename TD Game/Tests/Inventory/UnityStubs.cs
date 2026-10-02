// Only the engine/input surface needed to exercise inventory logic outside Unity.
using System;
namespace UnityEngine
{
    public class Behaviour { public bool enabled; }
    public class MonoBehaviour : Behaviour { protected static T FindFirstObjectByType<T>() => default; }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public static class Time { public static float deltaTime = 0.1f; }
    public static class Debug { public static void Log(object value) { } public static void LogWarning(object value, object context) { } }
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Ceil(float value) => MathF.Ceiling(value);
    }
}
namespace UnityEngine.InputSystem
{
    public class InputAction
    {
        public struct CallbackContext { }
        public event Action<CallbackContext> performed;
        public void Perform() => performed?.Invoke(default);
    }
}
public class PlayerControls : IDisposable
{
    public readonly Actions Player = new Actions();
    public void Enable() { } public void Disable() { } public void Dispose() { }
    public class Actions
    {
        public UnityEngine.InputSystem.InputAction Slot1 = new(), Slot2 = new(), Slot3 = new(), Slot4 = new(), Slot5 = new(), Slot6 = new(), Slot7 = new(), Slot8 = new(), Slot9 = new(), NextSlot = new(), PrevSlot = new();
    }
}
public class ToolDefinition
{
    public ToolHotbar.ToolKind toolKind = ToolHotbar.ToolKind.Custom;
    public string behaviourId;
    public int maximumCharges;
    public ItemCooldownKind cooldownKind;
    public float cooldownAmount;
}
public class TowerPlacementController : UnityEngine.Behaviour { }
public class TowerInspectorTool : UnityEngine.Behaviour { public void SetSelectionPermissions(bool towers, bool empty) { } }
public class GridHoverSelector : UnityEngine.Behaviour { }
public class WaveSpawner
{
    public event Action<int, int> OnWaveCompleted;
    public event Action OnEnemyKilled;
    public void CompleteWave() => OnWaveCompleted?.Invoke(1, 0);
    public void KillEnemy() => OnEnemyKilled?.Invoke();
}
