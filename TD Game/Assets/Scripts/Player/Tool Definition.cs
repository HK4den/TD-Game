using UnityEngine;

[CreateAssetMenu(menuName = "Wizliens/Tools/Tool Definition")]
public class ToolDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public ToolHotbar.ToolKind toolKind;
    [Tooltip("Optional scene behaviour binding ID. Leave empty for the existing tool-kind mapping.")]
    public string behaviourId;

    [Header("Availability")]
    [Tooltip("Optional alternate hotbar icon while the item is on cooldown.")]
    public Sprite cooldownIcon;
    [Tooltip("Zero means unlimited uses. Limited charges must be restored explicitly by the item's behaviour.")]
    [Min(0)] public int maximumCharges;
    public ItemCooldownKind cooldownKind;
    [Min(0f)] public float cooldownAmount;

    [Header("Held Model")]
    [Tooltip("Assign a visual prefab to show in hand. Leave empty to use the colored cube placeholder during Play.")]
    public GameObject heldItemPrefab;
    [Tooltip("Position relative to the player camera. Positive X moves right, negative Y moves down, and positive Z moves forward.")]
    public Vector3 heldItemLocalPosition = new Vector3(0.28f, -0.22f, 0.58f);
    [Tooltip("Rotation applied to the held model holder. Keep the prefab root at its grip point for easy adjustment.")]
    public Vector3 heldItemLocalEulerAngles = new Vector3(0f, 12f, 0f);
    [Tooltip("Scale for the held model holder. The prefab's own root scale is preserved too.")]
    public Vector3 heldItemLocalScale = Vector3.one;
    [Tooltip("Color used only by the automatic cube placeholder.")]
    public Color heldItemTint = Color.white;
}
