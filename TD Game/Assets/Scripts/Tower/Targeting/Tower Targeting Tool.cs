using UnityEngine;
using UnityEngine.InputSystem;

public class TowerTargetingTool : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;
    [SerializeField] private ToolHotbar hotbar;
    [SerializeField] private TowerRangeVisualizer rangeVisualizer;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private LayerMask tileMask;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 8f;

    private PlayerControls controls;
    private GridTile hoveredTile;
    private GameObject hoveredTower;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (hotbar == null) hotbar = FindFirstObjectByType<ToolHotbar>();
        if (rangeVisualizer == null) rangeVisualizer = FindFirstObjectByType<TowerRangeVisualizer>();

        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.PrimaryClick.performed += OnPrimaryClick;
        controls.Player.SecondaryClick.performed += OnSecondaryClick;

        hoveredTile = null;
        hoveredTower = null;

        if (rangeVisualizer != null)
            rangeVisualizer.ClearHoveredTower();
    }

    private void OnDisable()
    {
        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Player.SecondaryClick.performed -= OnSecondaryClick;
        controls.Disable();

        hoveredTile = null;
        hoveredTower = null;

        if (rangeVisualizer != null)
            rangeVisualizer.ClearHoveredTower();
    }

    private void Update()
    {
        if (PauseState.IsPaused || !IsTargetingToolActive())
        {
            ClearHoverState();
            return;
        }

        UpdateHoveredTower();
    }

    private bool IsTargetingToolActive()
    {
        if (hotbar == null)
            return false;

        return hotbar.CurrentSlot.kind == ToolHotbar.ToolKind.Targeting;
    }

    private void UpdateHoveredTower()
    {
        hoveredTile = null;
        hoveredTower = null;

        if (cam == null)
        {
            ClearHoverState();
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            ClearHoverState();
            return;
        }

        GridTile tile = hit.collider.GetComponentInParent<GridTile>();
        if (tile == null || tile.OccupiedTower == null)
        {
            ClearHoverState();
            return;
        }

        float dist = Vector3.Distance(cam.transform.position, tile.OccupiedTower.transform.position);
        if (dist > interactionDistance)
        {
            ClearHoverState();
            return;
        }

        hoveredTile = tile;
        hoveredTower = tile.OccupiedTower;

        if (rangeVisualizer != null)
            rangeVisualizer.SetHoveredTower(hoveredTower);
    }

    private void ClearHoverState()
    {
        hoveredTile = null;
        hoveredTower = null;

        if (rangeVisualizer != null)
            rangeVisualizer.ClearHoveredTower();
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || !IsTargetingToolActive())
            return;

        if (hoveredTower == null)
            return;

        TowerTargetingController targeting = hoveredTower.GetComponent<TowerTargetingController>();
        if (targeting == null) targeting = hoveredTower.GetComponentInChildren<TowerTargetingController>();
        if (targeting == null || !targeting.CanCycleTargetingMode())
            return;

        TowerTargetingMode newMode = targeting.CycleForward();

        TowerTargetingFeedback feedback = hoveredTower.GetComponent<TowerTargetingFeedback>();
        if (feedback == null) feedback = hoveredTower.GetComponentInChildren<TowerTargetingFeedback>();

        if (feedback != null)
            feedback.ShowMode(newMode);
    }

    private void OnSecondaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused || !IsTargetingToolActive())
            return;

        if (hoveredTower == null)
            return;

        TowerRotationController rotation = hoveredTower.GetComponent<TowerRotationController>();
        if (rotation == null) rotation = hoveredTower.GetComponentInChildren<TowerRotationController>();
        if (rotation == null || !rotation.CanManualRotate)
            return;

        rotation.RotateManualForward();
    }
}