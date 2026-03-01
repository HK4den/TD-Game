using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Renderer))]
public class GridTile : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private int x;
    [SerializeField] private int z;

    [Header("Terrain")]
    [SerializeField] private TerrainType terrainType = TerrainType.Normal;

    [Header("Rules")]
    [SerializeField] private bool buildable = true;

    [SerializeField] private bool blocksEnemies = false;

    [Header("Occupancy (runtime)")]
    [SerializeField] private bool occupied;
    [SerializeField] private GameObject occupiedTower;
    public bool BlocksEnemies => blocksEnemies;


    // ... (your existing visuals fields)

    private Renderer rend;

    public void SetBlocksEnemies(bool value) => blocksEnemies = value;
    public void SetBuildable(bool value) => buildable = value;

    public int X => x;
    public int Z => z;
    public TerrainType Terrain => terrainType;

    public bool IsOccupied => occupied;
    public bool IsBuildable => buildable;

    public bool IsPassableForEnemies => terrainType != TerrainType.Blocked && !blocksEnemies;
    public bool CanPlaceTower => buildable && !occupied;

    public GameObject OccupiedTower => occupiedTower;

    public void SetOccupied(bool value) => occupied = value;

    public void SetOccupiedTower(GameObject towerGO)
    {
        occupiedTower = towerGO;
        occupied = (towerGO != null);
    }

    public void ClearOccupiedTower()
    {
        occupiedTower = null;
        occupied = false;
    }

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        ApplyVisuals();
    }

    public void Initialize(int newX, int newZ)
    {
        x = newX;
        z = newZ;
        gameObject.name = $"Tile ({x}, {z})";

        if (rend == null) rend = GetComponent<Renderer>();
        ApplyVisuals();
    }

    public void SetTerrain(TerrainType type)
    {
        terrainType = type;
        ApplyVisuals();
    }

    public void ApplyVisuals()
    {
        if (rend == null) rend = GetComponent<Renderer>();

        // (keep your existing visuals code exactly)
        // ...
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        rend = GetComponent<Renderer>();
        ApplyVisuals();
    }
#endif
}