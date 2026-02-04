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

    [Header("Hover Border Decal")]
    [SerializeField] private Material hoverBorderMaterial;
    [SerializeField] private float hoverYOffset = 0.015f; // tiny lift to avoid z-fighting
    [SerializeField] private float hoverScale = 1.01f;    // slightly bigger than tile

    private Renderer rend;
    private Color baseColor = Color.white;

    private GameObject hoverQuad;
    private Renderer hoverRend;
    private bool isHovered;

    public int X => x;
    public int Z => z;
    public TerrainType Terrain => terrainType;
    public bool IsPassable => terrainType != TerrainType.Blocked;

    private void Awake()
    {
        rend = GetComponent<Renderer>();

        // Play mode: allow per-tile material instances (simple approach)
        if (Application.isPlaying && rend.sharedMaterial != null)
            rend.material = new Material(rend.sharedMaterial);

        EnsureHoverQuad();
        SetHover(false);

        ApplyVisuals();
    }

    public void Initialize(int newX, int newZ)
    {
        x = newX;
        z = newZ;
        gameObject.name = $"Tile ({x}, {z})";

        if (rend == null) rend = GetComponent<Renderer>();

        if (Application.isPlaying && rend.sharedMaterial != null)
            rend.material = new Material(rend.sharedMaterial);

        EnsureHoverQuad();
        SetHover(false);

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

        if (hoverQuad != null)
        {
            // Keep it sized correctly even if you resize tiles later
            if (isHovered) UpdateHoverQuadTransform();
            hoverQuad.SetActive(isHovered);
        }
    }

    private void EnsureHoverQuad()
    {
        if (hoverBorderMaterial == null) return;
        if (hoverQuad != null) return;

        if (rend == null) rend = GetComponent<Renderer>();

        // Create a child quad (no collider)
        hoverQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        hoverQuad.name = "HoverBorder";

        // Remove any collider so it never blocks raycasts
        Collider c = hoverQuad.GetComponent<Collider>();
        if (c != null) DestroyImmediate(c);

        hoverRend = hoverQuad.GetComponent<Renderer>();
        hoverRend.sharedMaterial = hoverBorderMaterial;

        // Make sure it doesn't interfere with raycasts
        hoverQuad.layer = 2; // Ignore Raycast

        // Position/size it based on the tile's actual world bounds
        UpdateHoverQuadTransform();
    }

    private void UpdateHoverQuadTransform()
    {
        if (hoverQuad == null || rend == null) return;

        // Place it slightly above the tile in world space
        Vector3 pos = transform.position;
        pos.y += hoverYOffset;
        hoverQuad.transform.position = pos;

        // Lay flat on XZ
        hoverQuad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Size it to match THIS tile's renderer bounds (world size)
        Vector3 size = rend.bounds.size;
        hoverQuad.transform.localScale = new Vector3(size.x * hoverScale, size.z * hoverScale, 1f);
    }

    public void ApplyVisuals()
    {
        if (rend == null) rend = GetComponent<Renderer>();

        int variantIdx = -1;
        if (variantMaterials != null && variantMaterials.Length > 0)
            variantIdx = Mathf.Abs(Hash(x, z)) % variantMaterials.Length;

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
            // Editor: avoid material instancing spam
            if (variantIdx >= 0 && variantMaterials[variantIdx] != null)
                rend.sharedMaterial = variantMaterials[variantIdx];

            if (rend.sharedMaterial != null && rend.sharedMaterial.HasProperty("_Color"))
            {
                rend.sharedMaterial.color = tint;
                baseColor = tint;
            }
        }
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
