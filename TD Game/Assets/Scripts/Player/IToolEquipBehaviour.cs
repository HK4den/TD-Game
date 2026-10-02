/// <summary>Optional contract for scene behaviours used by inventory items.</summary>
public interface IToolEquipBehaviour
{
    bool UsesGridHover { get; }
    bool AllowsTowerSelection { get; }
    bool AllowsEmptyTileSelection { get; }
    void Equip(ItemRuntimeState item);
    void Unequip();
}
