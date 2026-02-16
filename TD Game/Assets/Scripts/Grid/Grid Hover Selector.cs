using UnityEngine;
using UnityEngine.InputSystem;

public class GridHoverSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private LayerMask tileMask;

    [Header("Highlight Prefab")]
    [SerializeField] private GameObject hoverHighlightPrefab;
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float scalePadding = 1.02f;

    [Header("Click Blink")]
    [SerializeField] private float clickBlinkDuration = 0.12f;

    private Transform hoverHighlight;
    private float suppressUntilUnscaledTime;

    private PlayerControls controls;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        controls = new PlayerControls();

        if (hoverHighlightPrefab != null)
        {
            var obj = Instantiate(hoverHighlightPrefab);
            obj.name = "HoverHighlight (Runtime)";
            obj.layer = 2; // Ignore Raycast
            hoverHighlight = obj.transform;
            hoverHighlight.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("GridHoverSelector: hoverHighlightPrefab is not assigned.");
        }
    }

    private void OnEnable()
    {
        controls.Enable();

        // Requires you to have PrimaryClick in your input actions (you do)
        controls.Player.PrimaryClick.performed += OnPrimaryClick;
    }

    private void OnDisable()
    {
        controls.Player.PrimaryClick.performed -= OnPrimaryClick;
        controls.Disable();
    }

    private void Update()
    {
        UpdateHover();
    }

    private void OnPrimaryClick(InputAction.CallbackContext ctx)
    {
        suppressUntilUnscaledTime = Time.unscaledTime + clickBlinkDuration;
    }

    private void UpdateHover()
    {
        if (Time.unscaledTime < suppressUntilUnscaledTime)
        {
            SetHighlightActive(false);
            return;
        }

        if (cam == null || hoverHighlight == null)
        {
            SetHighlightActive(false);
            return;
        }

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                MoveHighlightTo(tile);
                SetHighlightActive(true);
                return;
            }
        }

        SetHighlightActive(false);
    }

    private void MoveHighlightTo(GridTile tile)
    {
        Vector3 pos = tile.transform.position;
        pos.y += yOffset;
        hoverHighlight.position = pos;

        Renderer tileRend = tile.GetComponent<Renderer>();
        Renderer hlRend = hoverHighlight.GetComponentInChildren<Renderer>();
        if (tileRend == null || hlRend == null) return;

        Vector3 tileSize = tileRend.bounds.size;

        Vector3 hlScale = hoverHighlight.lossyScale;
        Vector3 hlBaseSize = new Vector3(
            hlRend.bounds.size.x / Mathf.Max(hlScale.x, 0.0001f),
            hlRend.bounds.size.y / Mathf.Max(hlScale.y, 0.0001f),
            hlRend.bounds.size.z / Mathf.Max(hlScale.z, 0.0001f)
        );

        float targetX = (tileSize.x * scalePadding) / Mathf.Max(hlBaseSize.x, 0.0001f);
        float targetZ = (tileSize.z * scalePadding) / Mathf.Max(hlBaseSize.z, 0.0001f);

        Vector3 local = hoverHighlight.localScale;
        hoverHighlight.localScale = new Vector3(targetX, local.y, targetZ);
    }

    private void SetHighlightActive(bool active)
    {
        if (hoverHighlight != null && hoverHighlight.gameObject.activeSelf != active)
            hoverHighlight.gameObject.SetActive(active);
    }
}
