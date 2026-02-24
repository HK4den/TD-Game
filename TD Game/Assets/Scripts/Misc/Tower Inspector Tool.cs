using UnityEngine;
using UnityEngine.InputSystem;

public class TowerInspectorTool : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 8f;

    [Header("Raycast")]
    [SerializeField] private LayerMask tileMask;

    [Header("UI")]
    [SerializeField] private InspectPanelUI inspectPanel;

    private PlayerControls controls;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (inspectPanel == null) inspectPanel = FindFirstObjectByType<InspectPanelUI>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
        // If your inputactions doesn't have PrimaryClick yet, DON'T compile this way.
        // Replace this binding with whatever action you actually have for click.
        controls.Player.PrimaryClick.performed += OnPrimaryClick;
    }

    private void OnDisable()
    {
        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Disable();
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        if (cam == null || inspectPanel == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile == null)
            {
                inspectPanel.ClearSelection(); // you’ll add this method below
                return;
            }

            // Tile-based: tower info is determined by what's stored on the tile
            if (tile.OccupiedTower != null)
            {
                var id = tile.OccupiedTower.GetComponentInChildren<TowerIdentity>();
                var up = tile.OccupiedTower.GetComponentInChildren<TowerUpgradeState>();
                if (id != null) inspectPanel.SetSelectedTower(id, up, tile);
                else inspectPanel.SetSelectedTile(tile);
            }
            else
            {
                inspectPanel.SetSelectedTile(tile);
            }
        }
        else
        {
            // click did NOT hit a reachable tile => close
            inspectPanel.ClearSelection();
        }
    }
}