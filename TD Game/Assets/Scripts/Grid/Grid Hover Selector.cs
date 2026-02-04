using UnityEngine;
using UnityEngine.InputSystem;

public class GridHoverSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask tileMask; // set to your Tile layer

    [Header("Debug")]
    [SerializeField] private bool logCoords = true;

    private GridTile currentHover;
    private Vector2Int lastCoord = new Vector2Int(int.MinValue, int.MinValue);

    private void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void Update()
    {
        UpdateHover();
    }

    private void UpdateHover()
    {
        // Unhover previous
        if (currentHover != null)
            currentHover.SetHover(false);

        currentHover = null;

        if (Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, tileMask, QueryTriggerInteraction.Ignore))
        {
            GridTile tile = hit.collider.GetComponent<GridTile>();
            if (tile != null)
            {
                currentHover = tile;
                currentHover.SetHover(true);

                Vector2Int coord = new Vector2Int(tile.X, tile.Z);

                if (logCoords && coord != lastCoord)
                {
                    Debug.Log($"Hover Tile: ({coord.x}, {coord.y})");
                    lastCoord = coord;
                }

                return;
            }
        }

        // Not hovering a tile, reset so it logs next time
        lastCoord = new Vector2Int(int.MinValue, int.MinValue);
    }
}
