using UnityEngine;
using UnityEngine.InputSystem;

public class TowerInspectorTool : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 8f;

    [Header("Raycast")]
    [SerializeField] private LayerMask towerMask;
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

        TrySelect();
    }

    private void TrySelect()
    {
        if (cam == null || inspectPanel == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 1️⃣ Check for tower first
        if (Physics.Raycast(ray, out RaycastHit towerHit, maxDistance, towerMask, QueryTriggerInteraction.Ignore))
        {
            TowerIdentity identity = towerHit.collider.GetComponentInParent<TowerIdentity>();
            TowerUpgradeState upgradeState = towerHit.collider.GetComponentInParent<TowerUpgradeState>();
            GridTile tileUnder = FindTileUnderObject(towerHit.collider.transform.position);

            if (identity != null)
            {
                inspectPanel.SetSelectedTower(identity, upgradeState, tileUnder);
                return;
            }
        }

        // 2️⃣ Otherwise check for terrain tile
        if (Physics.Raycast(ray, out RaycastHit tileHit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            GridTile tile = tileHit.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                inspectPanel.SetSelectedTile(tile);
                return;
            }
        }
    }

    private GridTile FindTileUnderObject(Vector3 worldPos)
    {
        Ray down = new Ray(worldPos + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(down, out RaycastHit hit, 10f, tileMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponent<GridTile>();
        }
        return null;
    }
}
