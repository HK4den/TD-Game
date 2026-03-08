using UnityEngine;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text slotNumberText;

    [Header("Background Sprites")]
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;

    public void Setup(int slotNumber, ToolDefinition definition, bool isSelected)
    {
        if (slotNumberText != null)
            slotNumberText.text = slotNumber.ToString();

        if (iconImage != null)
        {
            if (definition != null && definition.icon != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        SetSelected(isSelected);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.sprite = selected
            ? selectedBackgroundSprite
            : normalBackgroundSprite;
    }
}