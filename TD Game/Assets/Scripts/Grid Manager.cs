using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float cellSize = 1f;

    [Header("Grid Origin")]
    [SerializeField] private Vector3 origin = Vector3.zero;

    //Actual grid coding below

    public bool IsInBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < gridWidth && z < gridHeight;
    }

    public Vector3 GridToWorld(int x, int z)
    {
        return origin + new Vector3(
            x * cellSize + cellSize * 0.5f,
            0f,
            z * cellSize + cellSize * 0.5f
        );
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - origin.x) / cellSize);
        int z = Mathf.FloorToInt((worldPos.z - origin.z) / cellSize);
        return new Vector2Int(x, z);
    }

    // Expose for later systems
    public int Width => gridWidth;
    public int Height => gridHeight;
    public float CellSize => cellSize;
    public Vector3 Origin => origin;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        // Vertical lines (Z direction)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
            Vector3 end = origin + new Vector3(x * cellSize, 0f, gridHeight * cellSize);
            Gizmos.DrawLine(start, end);
        }

        // Horizontal lines (X direction)
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = origin + new Vector3(0f, 0f, z * cellSize);
            Vector3 end = origin + new Vector3(gridWidth * cellSize, 0f, z * cellSize);
            Gizmos.DrawLine(start, end);
        }
    }
}
