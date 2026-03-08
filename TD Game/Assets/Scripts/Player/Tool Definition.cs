using UnityEngine;

[CreateAssetMenu(menuName = "Wizliens/Tools/Tool Definition")]
public class ToolDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public ToolHotbar.ToolKind toolKind;
}