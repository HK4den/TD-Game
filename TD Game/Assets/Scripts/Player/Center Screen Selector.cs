using UnityEngine;
using UnityEngine.InputSystem;

public class CenterScreenSelector : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private InspectPanelUI inspectUI;

    [Header("Input (New Input System)")]
    [Tooltip("Bind this to your left click / 'Select' action.")]
    [SerializeField] private InputActionReference selectAction;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private LayerMask towerMask;
    [SerializeField] private LayerMask tileMask;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        if (selectAction != null)
        {
            selectAction.action.Enable();
            selectAction.action.performed += OnSelectPerformed;
        }
    }

    private void OnDisable()
    {
        if (selectAction != null)
        {
            selectAction.action.performed -= OnSelectPerformed;
            selectAction.action.Disable();
        }
    }

    private void OnSelectPerformed(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused) return;
        TrySelect();
    }

    private void TrySelect()
    {
        if (cam == null || inspectUI == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // 1) Prefer tower selection
        if (Physics.Raycast(ray, out RaycastHit hitTower, maxDistance, towerMask, QueryTriggerInteraction.Ignore))
        {
            TowerIdentity tower = hitTower.collider.GetComponentInParent<TowerIdentity>();
            if (tower != null)
            {
                TowerUpgradeState upgradeState = tower.GetComponent<TowerUpgradeState>();

                GridTile under = FindTileUnderObject(tower.transform.position);
                inspectUI.SetSelectedTower(tower, upgradeState, under);
                return;
            }
        }

        // 2) Otherwise select a tile
        if (Physics.Raycast(ray, out RaycastHit hitTile, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            GridTile tile = hitTile.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                inspectUI.SetSelectedTile(tile);
                return;
            }
        }
    }

    private GridTile FindTileUnderObject(Vector3 worldPos)
    {
        // Cast downward to find the tile under a tower
        Ray down = new Ray(worldPos + Vector3.up * 2f, Vector3.down);
        if (Physics.Raycast(down, out RaycastHit hit, 10f, tileMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponent<GridTile>();
        }
        return null;
    }
}