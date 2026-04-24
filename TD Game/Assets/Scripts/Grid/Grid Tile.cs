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

    private Renderer rend;

    public int X => x;
    public int Z => z;
    public TerrainType Terrain => terrainType;

    public bool IsOccupied => occupied;
    public bool BlocksEnemies => blocksEnemies;
    public bool IsBuildable => buildable;
    public GameObject OccupiedTower => occupiedTower;

    public bool IsBlockedTerrain => terrainType == TerrainType.Blocked;
    public bool IsBeamTerrain => terrainType == TerrainType.Beam;
    public bool IsBrushTerrain => terrainType == TerrainType.Brush;
    public bool IsThickBrushTerrain => terrainType == TerrainType.ThickBrush;
    public bool IsRubbleTerrain => terrainType == TerrainType.Rubble;

    public bool IsPassableForEnemies => terrainType != TerrainType.Blocked && !blocksEnemies;
    public bool CanPlaceTower => IsBuildable && !occupied;

    public void SetBlocksEnemies(bool value) => blocksEnemies = value;
    public void SetBuildable(bool value) => buildable = value;

    public void SetOccupied(bool value) => occupied = value;

    public void SetOccupiedTower(GameObject towerGO)
    {
        occupiedTower = towerGO;
        occupied = towerGO != null;
    }

    public void ClearOccupiedTower()
    {
        occupiedTower = null;
        occupied = false;
    }

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        ApplyTerrainRules();
        ApplyVisuals();
    }

    public void Initialize(int newX, int newZ)
    {
        x = newX;
        z = newZ;
        gameObject.name = $"Tile ({x}, {z})";

        if (rend == null)
            rend = GetComponent<Renderer>();

        ApplyTerrainRules();
        ApplyVisuals();
    }

    public void SetTerrain(TerrainType type)
    {
        terrainType = type;
        ApplyTerrainRules();
        ApplyVisuals();
    }

    private void ApplyTerrainRules()
    {
        switch (terrainType)
        {
            case TerrainType.Blocked:
                buildable = false;
                blocksEnemies = true;
                break;

            case TerrainType.Beam:
            case TerrainType.ThickBrush:
            case TerrainType.Rubble:
                buildable = false;
                blocksEnemies = false;
                break;

            case TerrainType.Brush:
                buildable = true;
                blocksEnemies = false;
                break;

            default:
                buildable = true;
                blocksEnemies = false;
                break;
        }
    }

    public void ApplyVisuals()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        // Keep your existing visuals code exactly here.
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        rend = GetComponent<Renderer>();
        ApplyTerrainRules();
        ApplyVisuals();
    }
#endif
}