using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HotbarSlotUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text slotNumberText;

    [Header("Background Sprites")]
    [SerializeField] private Sprite normalBackgroundSprite;
    [SerializeField] private Sprite selectedBackgroundSprite;
    private ToolDefinition itemDefinition;
    private ItemRuntimeState itemState;
    private Image cooldownRing;
    private Text cooldownText;
    private Text chargesText;
    private static readonly Dictionary<int, Sprite> ringSprites = new Dictionary<int, Sprite>();
    public Font NumberFont => slotNumberText != null ? slotNumberText.font : null;

    public void Setup(int slotNumber, ToolDefinition definition, bool isSelected, ItemRuntimeState state = null)
    {
        if (itemState != null) itemState.Changed -= RefreshAvailability;
        itemDefinition = definition;
        itemState = state;
        if (itemState != null) itemState.Changed += RefreshAvailability;
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
        RefreshAvailability();
    }

    private void OnDestroy()
    {
        if (itemState != null) itemState.Changed -= RefreshAvailability;
    }

    private void OnApplicationQuit()
    {
        foreach (Sprite sprite in ringSprites.Values)
        {
            if (sprite == null) continue;
            Destroy(sprite.texture);
            Destroy(sprite);
        }
        ringSprites.Clear();
    }

    private void RefreshAvailability()
    {
        bool coolingDown = itemState != null && itemState.IsOnCooldown;
        bool exhausted = itemState != null && itemState.HasLimitedCharges && itemState.Charges == 0;
        if (iconImage != null)
        {
            iconImage.sprite = coolingDown && itemDefinition != null && itemDefinition.cooldownIcon != null
                ? itemDefinition.cooldownIcon : itemDefinition != null ? itemDefinition.icon : null;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.color = exhausted || (coolingDown && (itemDefinition == null || itemDefinition.cooldownIcon == null))
                ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
        }
        if (itemState != null && itemState.HasLimitedCharges && chargesText == null)
        {
            GameObject label = new GameObject("Uses Remaining", typeof(RectTransform), typeof(Text));
            label.layer = gameObject.layer;
            RectTransform rect = (RectTransform)label.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 8f);
            rect.sizeDelta = new Vector2(50f, 20f);
            chargesText = label.GetComponent<Text>();
            chargesText.font = NumberFont != null ? NumberFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            chargesText.fontSize = 20;
            chargesText.alignment = TextAnchor.MiddleCenter;
            chargesText.raycastTarget = false;
        }
        if (chargesText != null)
        {
            chargesText.gameObject.SetActive(itemState != null && itemState.HasLimitedCharges);
            chargesText.text = itemState != null ? itemState.Charges.ToString() : "";
        }
        if (coolingDown && cooldownRing == null) CreateCooldownOverlay();
        if (cooldownRing == null) return;
        cooldownRing.gameObject.SetActive(coolingDown);
        if (!coolingDown) return;
        bool discrete = itemState.CooldownKind != ItemCooldownKind.Seconds;
        int segments = discrete ? Mathf.Clamp(Mathf.CeilToInt(itemState.CooldownTotal), 1, 24) : 1;
        cooldownRing.sprite = GetRingSprite(segments);
        cooldownRing.fillAmount = itemState.CooldownFraction;
        cooldownText.text = Mathf.CeilToInt(itemState.CooldownRemaining).ToString();
    }

    private void CreateCooldownOverlay()
    {
        GameObject ring = new GameObject("Cooldown", typeof(RectTransform), typeof(Image));
        ring.layer = gameObject.layer;
        RectTransform rect = (RectTransform)ring.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(54f, 54f);
        cooldownRing = ring.GetComponent<Image>();
        cooldownRing.type = Image.Type.Filled;
        cooldownRing.fillMethod = Image.FillMethod.Radial360;
        cooldownRing.fillOrigin = (int)Image.Origin360.Top;
        cooldownRing.fillClockwise = true;
        cooldownRing.color = new Color(1f, 0.8f, 0.3f, 0.95f);
        cooldownRing.raycastTarget = false;
        GameObject label = new GameObject("Remaining", typeof(RectTransform), typeof(Text));
        label.layer = gameObject.layer;
        RectTransform labelRect = (RectTransform)label.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        cooldownText = label.GetComponent<Text>();
        cooldownText.font = NumberFont != null ? NumberFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cooldownText.fontSize = 24;
        cooldownText.alignment = TextAnchor.MiddleCenter;
        cooldownText.color = Color.white;
        cooldownText.raycastTarget = false;
        label.AddComponent<Shadow>().effectDistance = new Vector2(1f, -1f);
    }

    private static Sprite GetRingSprite(int segments)
    {
        if (ringSprites.TryGetValue(segments, out Sprite cached) && cached != null) return cached;
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Hotbar Cooldown Ring " + segments;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 point = new Vector2(x + 0.5f - size / 2f, y + 0.5f - size / 2f);
            float radius = point.magnitude;
            float clockwiseAngle = Mathf.Repeat(Mathf.Atan2(point.x, point.y), Mathf.PI * 2f);
            float segmentPhase = Mathf.Repeat(clockwiseAngle * segments / (Mathf.PI * 2f), 1f);
            bool gap = segments > 1 && (segmentPhase < 0.035f || segmentPhase > 0.965f);
            float alpha = gap ? 0f : Mathf.Clamp01(Mathf.Min(radius - 49f, 62f - radius));
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        ringSprites[segments] = sprite;
        return sprite;
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
