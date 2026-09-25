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
        if (hotbar == null || slotContainer == null || slotPrefab == null)
            return;

        int ownedCount = hotbar.OwnedSlotCount;
        int selectedOwnedIndex = hotbar.GetOwnedIndexFromRealIndex(hotbar.CurrentSlotIndex);

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
            spawnedSlots[i].Setup(i + 1, slot.definition, i == selectedOwnedIndex);
        }
    }
}
