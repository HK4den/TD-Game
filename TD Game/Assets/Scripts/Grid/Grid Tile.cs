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

    [Header("Visual Variants (optional)")]
    [SerializeField] private Material[] variantMaterials;

    [Header("Terrain Tints (optional)")]
    [SerializeField] private Color normalTint = Color.white;
    [SerializeField] private Color swampTint = new Color(0.75f, 1f, 0.75f, 1f);
    [SerializeField] private Color fireTint = new Color(1f, 0.8f, 0.6f, 1f);
    [SerializeField] private Color energyTint = new Color(0.7f, 0.9f, 1f, 1f);
    [SerializeField] private Color blockedTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Hover")]
    [SerializeField] private float hoverBrightenMultiplier = 1.15f;

    private Renderer rend;
    private Color baseColor = Color.white;
    private bool isHovered;

    public int X => x;
    public int Z => z;
    public TerrainType Terrain => terrainType;
    public bool IsPassable => terrainType != TerrainType.Blocked;

    private void Awake()
    {
        rend = GetComponent<Renderer>();

        // Only create per-tile material instances while playing.
        if (Application.isPlaying)
            EnsureInstanceMaterial();

        ApplyVisuals();
    }

    public void Initialize(int newX, int newZ)
    {
        x = newX;
        z = newZ;
        gameObject.name = $"Tile ({x}, {z})";

        if (rend == null) rend = GetComponent<Renderer>();

        if (Application.isPlaying)
            EnsureInstanceMaterial();

        ApplyVisuals();
    }

    public void SetTerrain(TerrainType type)
    {
        terrainType = type;
        ApplyVisuals();
    }

    public void SetHover(bool hovering)
    {
        isHovered = hovering;
        ApplyHoverColor();
    }

    private void EnsureInstanceMaterial()
    {
        // Create a unique material instance for this tile in play mode
        if (rend == null) rend = GetComponent<Renderer>();
        if (rend.sharedMaterial != null)
            rend.material = new Material(rend.sharedMaterial);
    }

    public void ApplyVisuals()
    {
        if (rend == null) rend = GetComponent<Renderer>();

        // Choose stable variant index
        int variantIdx = -1;
        if (variantMaterials != null && variantMaterials.Length > 0)
            variantIdx = Mathf.Abs(Hash(x, z)) % variantMaterials.Length;

        // Terrain tint
        Color tint = terrainType switch
        {
            TerrainType.Swamp => swampTint,
            TerrainType.Fire => fireTint,
            TerrainType.Energy => energyTint,
            TerrainType.Blocked => blockedTint,
            _ => normalTint
        };

        if (Application.isPlaying)
        {
            // PLAY MODE: per-tile material instances allowed
            if (variantIdx >= 0 && variantMaterials[variantIdx] != null)
                rend.material = new Material(variantMaterials[variantIdx]);

            if (rend.material != null && rend.material.HasProperty("_Color"))
            {
                rend.material.color = tint;
                baseColor = tint;
            }
        }
        else
        {
            // EDIT MODE: only touch sharedMaterial (no instancing spam)
            if (variantIdx >= 0 && variantMaterials[variantIdx] != null)
                rend.sharedMaterial = variantMaterials[variantIdx];

            if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_Color"))
            {
                rend.sharedMaterial.color = tint;
                baseColor = tint;
            }
        }

        ApplyHoverColor();
    }

    private void ApplyHoverColor()
    {
        if (rend == null) return;

        // In edit mode, don't try to do hover color live (we'll do hover in play mode anyway)
        if (!Application.isPlaying) return;

        if (rend.material == null || !rend.material.HasProperty("_Color"))
            return;

        rend.material.color = isHovered ? baseColor * hoverBrightenMultiplier : baseColor;
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
        // Don't run on the prefab asset itself
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        rend = GetComponent<Renderer>();
        ApplyVisuals();
    }
#endif
}
