using UnityEngine;
using UnityEngine.InputSystem;

public class TowerInspectorTool : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 8f;

    [Header("Raycast")]
    [SerializeField] private LayerMask towerMask;

    [Header("UI")]
    [SerializeField] private TowerInspectPanel inspectPanel;

    private PlayerControls controls;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (inspectPanel == null) inspectPanel = FindFirstObjectByType<TowerInspectPanel>();

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

        Tower tower = RaycastTowerCenterScreen();
        if (tower == null) return;

        if (inspectPanel != null)
            inspectPanel.Toggle(tower);
    }

    private Tower RaycastTowerCenterScreen()
    {
        if (cam == null) return null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, towerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<Tower>();
        }

        return null;
    }
}
