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
    [SerializeField] private GameObject hoverHighlightPrefab; // drag your prefab here
    [SerializeField] private float yOffset = 0.02f;
    [SerializeField] private float scalePadding = 1.02f;

    private Transform hoverHighlight;
    private GridTile currentHover;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;

        if (hoverHighlightPrefab != null)
        {
            GameObject obj = Instantiate(hoverHighlightPrefab);
            obj.name = "HoverHighlight (Runtime)";
            obj.layer = 2; // Ignore Raycast (prevents blocking tile raycasts)
            hoverHighlight = obj.transform;

            // Ensure it starts hidden
            hoverHighlight.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("GridHoverSelector: hoverHighlightPrefab is not assigned.");
        }
    }

    private void Update()
    {
        UpdateHover();
    }

    private void UpdateHover()
    {
        currentHover = null;

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
                currentHover = tile;
                MoveHighlightTo(tile);
                SetHighlightActive(true);
                return;
            }
        }

        SetHighlightActive(false);
    }

    private void MoveHighlightTo(GridTile tile)
    {
        // Position above tile
        Vector3 pos = tile.transform.position;
        pos.y += yOffset;
        hoverHighlight.position = pos;

        // DON'T override rotation; use prefab's rotation as-is.

        // Get tile size in world
        Renderer tileRend = tile.GetComponent<Renderer>();
        if (tileRend == null) return;

        Vector3 tileSize = tileRend.bounds.size;

        // Get highlight mesh size in world (at current scale)
        Renderer hlRend = hoverHighlight.GetComponentInChildren<Renderer>();
        if (hlRend == null) return;

        // Bounds size includes current scale, so normalize by current scale to get "base" size.
        // Use lossyScale because highlight might have parent transforms.
        Vector3 hlScale = hoverHighlight.lossyScale;
        Vector3 hlBaseSize = new Vector3(
            hlRend.bounds.size.x / Mathf.Max(hlScale.x, 0.0001f),
            hlRend.bounds.size.y / Mathf.Max(hlScale.y, 0.0001f),
            hlRend.bounds.size.z / Mathf.Max(hlScale.z, 0.0001f)
        );

        // We want highlight XZ footprint to match tile XZ footprint.
        // Depending on your highlight mesh orientation, footprint axes could be XZ or XY.
        // We'll assume it's lying flat already (your prefab rotation handles that).
        // So we scale X by tileSize.x and Z by tileSize.z relative to highlight base size.
        float targetX = (tileSize.x * scalePadding) / Mathf.Max(hlBaseSize.x, 0.0001f);
        float targetZ = (tileSize.z * scalePadding) / Mathf.Max(hlBaseSize.z, 0.0001f);

        Vector3 local = hoverHighlight.localScale;
        hoverHighlight.localScale = new Vector3(targetX, local.y, targetZ);
    }


    private void SetHighlightActive(bool active)
    {
        if (hoverHighlight.gameObject.activeSelf != active)
            hoverHighlight.gameObject.SetActive(active);
    }
}
