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
    [Tooltip("Can the player place a tower here? (independent of terrain)")]
    [SerializeField] private bool buildable = true;

    [Tooltip("Does this tile block enemy pathing? (terrain Blocked will also block)")]
    [SerializeField] private bool blocksEnemies = false;

    [Header("Occupancy (runtime)")]
    [SerializeField] private bool occupied; // later: store tower reference

    [Header("Visual Variants (optional)")]
    [SerializeField] private Material[] variantMaterials;

    [Header("Terrain Tints (optional)")]
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private Color swampTint = new Color(0.75f, 1f, 0.75f, 1f);
    [SerializeField] private Color fireTint = new Color(1f, 0.8f, 0.6f, 1f);
    [SerializeField] private Color energyTint = new Color(0.7f, 0.9f, 1f, 1f);
    [SerializeField] private Color blockedTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    private Renderer rend;

    public void SetBlocksEnemies(bool value) => blocksEnemies = value;
    public void SetBuildable(bool value) => buildable = value;


    public int X => x;
    public int Z => z;
    public TerrainType Terrain => terrainType;

    // Enemy passability: blocked terrain OR explicit blocksEnemies
    public bool IsPassableForEnemies => terrainType != TerrainType.Blocked && !blocksEnemies;

    // Placement rules: must be buildable, not occupied. (Terrain can still be swamp/fire/etc.)
    public bool CanPlaceTower => buildable && !occupied;

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

    // We'll use these later when placing/removing towers
    public void SetOccupied(bool value) => occupied = value;

    public void ApplyVisuals()
    {
        if (rend == null) rend = GetComponent<Renderer>();

        int variantIdx = -1;
        if (variantMaterials != null && variantMaterials.Length > 0)
            variantIdx = Mathf.Abs(Hash(x, z)) % variantMaterials.Length;

        if (variantIdx >= 0 && variantMaterials[variantIdx] != null)
            rend.sharedMaterial = variantMaterials[variantIdx];

        Color tint = terrainType switch
        {
            TerrainType.Swamp => swampTint,
            TerrainType.Fire => fireTint,
            TerrainType.Energy => energyTint,
            TerrainType.Blocked => blockedTint,
            _ => normalTint
        };

        if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_Color"))
            rend.sharedMaterial.color = tint;
    }

    private int Hash(int a, int b)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + a;
            h = h * 31 + b;
            return h;
        }
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
