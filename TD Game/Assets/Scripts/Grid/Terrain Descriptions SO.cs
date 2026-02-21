using UnityEngine;

[CreateAssetMenu(menuName = "Wizliens/Terrain Descriptions", fileName = "TerrainDescriptions")]
public class TerrainDescriptionsSO : ScriptableObject
{
    [TextArea(2, 6)] public string normalDescription = "Towers can be placed here, enemies will walk through. No special effects.";
    [TextArea(2, 6)] public string swampDescription = "Towers shoot slower while on the tile, but enemies move slower";
    [TextArea(2, 6)] public string fireDescription = "Towers and enemies take damage while on the tile";
    [TextArea(2, 6)] public string energyDescription = "Towers shoot faster and deal more damage and enemies move quicker while on the tile";
    [TextArea(2, 6)] public string blockedDescription = "Towers can't be placed here but enemies can't move through it.";

    public string GetDescription(TerrainType type)
    {
        return type switch
        {
            TerrainType.Swamp => swampDescription,
            TerrainType.Fire => fireDescription,
            TerrainType.Energy => energyDescription,
            TerrainType.Blocked => blockedDescription,
            _ => normalDescription
        };
    }
}
