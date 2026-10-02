using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolHotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolHotbar hotbar;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private HotbarSlotUI slotPrefab;
    [Header("Equipped Item Name")]
    [SerializeField] private Text itemNameText;
    [SerializeField] private float nameVisibleSeconds = 1.5f;
    [SerializeField] private float nameFadeSeconds = 0.3f;
    private CanvasGroup nameGroup;
    private float nameAge;
    private ItemRuntimeState namedItem;

    private readonly List<HotbarSlotUI> spawnedSlots = new List<HotbarSlotUI>();

    private void Awake()
    {
        if (hotbar == null) hotbar = FindFirstObjectByType<ToolHotbar>();
    }

    private void OnEnable()
    {
        if (hotbar != null)
            hotbar.OnHotbarChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (hotbar != null)
            hotbar.OnHotbarChanged -= Refresh;
    }

    private void Update()
    {
        if (nameGroup == null || PauseState.IsPaused) return;
        nameAge += Time.deltaTime;
        nameGroup.alpha = namedItem == null ? 0f : 1f - Mathf.Clamp01((nameAge - nameVisibleSeconds) / Mathf.Max(0.01f, nameFadeSeconds));
    }

    private void EnsureNameLabel()
    {
        if (itemNameText == null)
        {
            GameObject label = new GameObject("Equipped Item Name", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            label.layer = slotContainer.gameObject.layer;
            RectTransform rect = (RectTransform)label.transform;
            rect.SetParent(slotContainer, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 12f);
            rect.sizeDelta = new Vector2(400f, 40f);
            label.GetComponent<LayoutElement>().ignoreLayout = true;
            itemNameText = label.GetComponent<Text>();
            itemNameText.font = slotPrefab.NumberFont != null ? slotPrefab.NumberFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemNameText.fontSize = 28;
            itemNameText.alignment = TextAnchor.MiddleCenter;
            itemNameText.raycastTarget = false;
            itemNameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Shadow shadow = label.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(1f, -1f);
        }
        if (nameGroup == null)
        {
            nameGroup = itemNameText.GetComponent<CanvasGroup>();
            if (nameGroup == null) nameGroup = itemNameText.gameObject.AddComponent<CanvasGroup>();
            nameGroup.blocksRaycasts = false;
            nameGroup.interactable = false;
        }
    }

    public void Refresh()
    {
        if (hotbar == null || slotContainer == null || slotPrefab == null)
            return;

        int ownedCount = hotbar.OwnedSlotCount;
        int selectedOwnedIndex = hotbar.GetOwnedIndexFromRealIndex(hotbar.CurrentSlotIndex);
        EnsureNameLabel();
        if (namedItem != hotbar.CurrentItem)
        {
            namedItem = hotbar.CurrentItem;
            ToolHotbar.Slot current = hotbar.CurrentSlot;
            itemNameText.text = current.definition != null && !string.IsNullOrWhiteSpace(current.definition.displayName)
                ? current.definition.displayName : current.kind.ToString();
            nameAge = 0f;
            nameGroup.alpha = namedItem != null ? 1f : 0f;
        }

        while (spawnedSlots.Count < ownedCount)
        {
            HotbarSlotUI ui = Instantiate(slotPrefab, slotContainer);
            spawnedSlots.Add(ui);
        }

        while (spawnedSlots.Count > ownedCount)
        {
            int last = spawnedSlots.Count - 1;
            if (spawnedSlots[last] != null)
                Destroy(spawnedSlots[last].gameObject);
            spawnedSlots.RemoveAt(last);
        }

        for (int i = 0; i < ownedCount; i++)
        {
            ToolHotbar.Slot slot = hotbar.GetOwnedSlot(i);
            spawnedSlots[i].Setup(i + 1, slot.definition, i == selectedOwnedIndex, slot.runtimeState);
        }
    }
}
