using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 20;
    [SerializeField] private int gridHeight = 20;
    [SerializeField] private float cellSize = 1f;

    [Header("Grid Origin")]
    [SerializeField] private Vector3 origin = Vector3.zero;

    [Header("Tile")]
    [SerializeField] private GridTile tilePrefab;
    [SerializeField] private Transform tileParent;

    private GridTile[,] tiles;

    private void Awake()
    {
        // Grid is authored in-editor, so at runtime we just rebuild the lookup.
        RebuildLookupFromChildren();
    }

    // ----------------------
    // Lookup / Access

    public void RebuildLookupFromChildren()
    {
        tiles = new GridTile[gridWidth, gridHeight];

        if (tileParent == null)
        {
            Debug.LogWarning("GridManager: tileParent is null. Can't rebuild lookup.");
            return;
        }

        for (int i = 0; i < tileParent.childCount; i++)
        {
            GridTile t = tileParent.GetChild(i).GetComponent<GridTile>();
            if (t == null) continue;

            if (IsInBounds(t.X, t.Z))
                tiles[t.X, t.Z] = t;
        }
    }

    public bool IsInBounds(int x, int z)
    {
        return x >= 0 && z >= 0 && x < gridWidth && z < gridHeight;
    }

    public GridTile GetTile(int x, int z)
    {
        if (!IsInBounds(x, z)) return null;
        return tiles[x, z];
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

    // 4-way neighbors (up/down/left/right) on XZ grid
    public void GetNeighbors4(GridTile tile, System.Collections.Generic.List<GridTile> results)
    {
        results.Clear();
        if (tile == null) return;

        int x = tile.X;
        int z = tile.Z;

        TryAdd(x + 1, z);
        TryAdd(x - 1, z);
        TryAdd(x, z + 1);
        TryAdd(x, z - 1);

        void TryAdd(int nx, int nz)
        {
            GridTile t = GetTile(nx, nz);
            if (t != null) results.Add(t);
        }
    }

#if UNITY_EDITOR
    public void GenerateGridInEditor()
    {
        if (tilePrefab == null)
        {
            Debug.LogError("GridManager: Tile Prefab is not assigned.");
            return;
        }

        if (tileParent == null)
        {
            GameObject parentObj = new GameObject("Tiles");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = Vector3.zero;
            tileParent = parentObj.transform;
        }

        ClearGridInEditor();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPos = GridToWorld(x, z);

                GridTile tile = (GridTile)UnityEditor.PrefabUtility.InstantiatePrefab(tilePrefab, tileParent);
                tile.transform.position = worldPos;
                tile.transform.rotation = Quaternion.identity;
                tile.Initialize(x, z);

                UnityEditor.Undo.RegisterCreatedObjectUndo(tile.gameObject, "Create Grid Tile");
            }
        }

        // keep lookup valid in editor too
        RebuildLookupFromChildren();
    }

    public void ClearGridInEditor()
    {
        if (tileParent == null) return;

        for (int i = tileParent.childCount - 1; i >= 0; i--)
        {
            Transform child = tileParent.GetChild(i);
            UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
        }

        tiles = null;
    }
#endif
}
