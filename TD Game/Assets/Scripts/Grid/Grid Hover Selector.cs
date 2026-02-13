using UnityEngine;
using UnityEngine.InputSystem;

public class GridHoverSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask tileMask;

    [Header("Highlight Prefab")]
    [SerializeField] private GameObject hoverHighlightPrefab; // drag prefab
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float scalePadding = 1.02f;

    [Header("Click Blink")]
    [SerializeField] private float clickBlinkDuration = 0.12f;

    private Transform hoverHighlight;
    private float suppressUntilUnscaledTime;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

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

    private void Update()
    {
        // Blink the highlight off briefly when clicking
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            suppressUntilUnscaledTime = Time.unscaledTime + clickBlinkDuration;

        UpdateHover();
    }

    private void UpdateHover()
    {
        // During blink, force off
        if (Time.unscaledTime < suppressUntilUnscaledTime)
        {
            SetHighlightActive(false);
            return;
        }

        if (Mouse.current == null || cam == null || hoverHighlight == null)
        {
            SetHighlightActive(false);
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

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
