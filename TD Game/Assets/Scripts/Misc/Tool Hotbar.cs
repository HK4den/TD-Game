using UnityEngine;

public class ToolHotbar : MonoBehaviour
{
    public enum ToolType
    {
        PlacementStaff = 0,
        InspectStaff = 1,
    }

    [Header("Tools")]
    [SerializeField] private TowerPlacementController placementTool;
    [SerializeField] private TowerInspectorTool inspectTool;

    [SerializeField] private ToolType current = ToolType.PlacementStaff;

    private void Awake()
    {
        Apply();
    }

    public ToolType Current => current;

    public void SetTool(ToolType tool)
    {
        if (current == tool) return;
        current = tool;
        Apply();
    }

    private void Apply()
    {
        if (placementTool != null)
        {
            placementTool.enabled = (current == ToolType.PlacementStaff);
            if (!placementTool.enabled)
                placementTool.ClearSelectionAndHideGhost();
        }

        if (inspectTool != null)
            inspectTool.enabled = (current == ToolType.InspectStaff);
    }
}
