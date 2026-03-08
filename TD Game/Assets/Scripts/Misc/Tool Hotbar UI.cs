using System.Collections.Generic;
using UnityEngine;

public class ToolHotbarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ToolHotbar hotbar;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private HotbarSlotUI slotPrefab;

    private readonly List<HotbarSlotUI> spawnedSlots = new List<HotbarSlotUI>();

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

    public void Refresh()
    {
        ClearSlots();

        if (hotbar == null || slotContainer == null || slotPrefab == null)
            return;

        int ownedCount = hotbar.OwnedSlotCount;
        int selectedOwnedIndex = hotbar.GetOwnedIndexFromRealIndex(hotbar.CurrentSlotIndex);

        for (int i = 0; i < ownedCount; i++)
        {
            ToolHotbar.Slot slot = hotbar.GetOwnedSlot(i);
            HotbarSlotUI ui = Instantiate(slotPrefab, slotContainer);
            bool isSelected = (i == selectedOwnedIndex);

            ui.Setup(i + 1, slot.definition, isSelected);
            spawnedSlots.Add(ui);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
                Destroy(spawnedSlots[i].gameObject);
        }

        spawnedSlots.Clear();

        if (slotContainer == null)
            return;

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(slotContainer.GetChild(i).gameObject);
        }
    }
}