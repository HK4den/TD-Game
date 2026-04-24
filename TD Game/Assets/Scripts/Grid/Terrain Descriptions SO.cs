using UnityEngine;

[CreateAssetMenu(menuName = "Wizliens/Terrain Descriptions", fileName = "TerrainDescriptions")]
public class TerrainDescriptionsSO : ScriptableObject
{
    [TextArea(2, 6)] public string normalDescription = "Towers can be placed here, enemies will move through.";
    [TextArea(2, 6)] public string swampDescription = "Towers shoot slower while on the tile, but enemies move slower.";
    [TextArea(2, 6)] public string fireDescription = "Towers and enemies take damage while on the tile.";
    [TextArea(2, 6)] public string energyDescription = "Towers shoot faster and deal more damage and enemies move quicker while on the tile.";
    [TextArea(2, 6)] public string blockedDescription = "Towers can't be placed here and enemies can't move through it.";
    [TextArea(2, 6)] public string beamDescription = "Enemies come from here!";
    [TextArea(2, 6)] public string brushDescription = "Enemies are hidden through here and move slower, but towers can still be placed on this tile.";
    [TextArea(2, 6)] public string thickBrushDescription = "Enemies are hidden through here and move much slower. Towers cannot be placed here.";
    [TextArea(2, 6)] public string rubbleDescription = "Enemies can move through this tile, but towers cannot be placed here.";

    public string GetDescription(TerrainType type)
    {
        return type switch
        {
            TerrainType.Swamp => swampDescription,
            TerrainType.Fire => fireDescription,
            TerrainType.Energy => energyDescription,
            TerrainType.Blocked => blockedDescription,
            TerrainType.Beam => beamDescription,
            TerrainType.Brush => brushDescription,
            TerrainType.ThickBrush => thickBrushDescription,
            TerrainType.Rubble => rubbleDescription,
            _ => normalDescription
        };
    }
}