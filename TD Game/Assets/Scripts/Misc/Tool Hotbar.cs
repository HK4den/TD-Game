using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolHotbar : MonoBehaviour
{
    public enum ToolKind
    {
        Empty = 0,
        Placement = 1,
        Inspect = 2,
        Mining = 3,
        Targeting = 4,
    }

    [Serializable]
    public struct Slot
    {
        public ToolKind kind;
        public ToolDefinition definition;

        [Tooltip("Enable/disable this behaviour when the slot is equipped. Leave null for Empty.")]
        public Behaviour toolBehaviour;
    }

    [Header("Slots (size should be 9 max)")]
    [SerializeField] private Slot[] slots = new Slot[9];

    [Header("Tool Behaviours (existing scripts)")]
    [SerializeField] private TowerPlacementController placementTool;
    [SerializeField] private TowerInspectorTool inspectTool;

    [Header("Visual-only hover highlight (optional)")]
    [SerializeField] private GridHoverSelector hoverSelector;

    [Header("Start State")]
    [SerializeField] private int startSlotIndex = 0;
    [SerializeField] private bool debugLogSwitching = true;

    private int currentSlotIndex;
    private PlayerControls controls;

    public int CurrentSlotIndex => currentSlotIndex;

    public int OwnedSlotCount
    {
        get
        {
            if (slots == null) return 0;

            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].kind != ToolKind.Empty)
                    count++;
            }
            return count;
        }
    }

    public Slot GetOwnedSlot(int ownedIndex)
    {
        int count = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind == ToolKind.Empty)
                continue;

            if (count == ownedIndex)
                return slots[i];

            count++;
        }

        return default;
    }

    public int GetOwnedIndexFromRealIndex(int realIndex)
    {
        if (slots == null || realIndex < 0 || realIndex >= slots.Length)
            return -1;

        int ownedIndex = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind == ToolKind.Empty)
                continue;

            if (i == realIndex)
                return ownedIndex;

            ownedIndex++;
        }

        return -1;
    }

    public Slot CurrentSlot =>
        (slots != null && slots.Length > 0)
        ? slots[Mathf.Clamp(currentSlotIndex, 0, slots.Length - 1)]
        : default;

    public event Action OnHotbarChanged;

    private void Awake()
    {
        controls = new PlayerControls();

        AutoWireSlotsIfNeeded();

        if (placementTool != null)
            placementTool.enabled = false;

        if (inspectTool != null)
            inspectTool.enabled = false;

        if (hoverSelector != null)
            hoverSelector.enabled = false;

        currentSlotIndex = FindNearestOwnedSlot(
            Mathf.Clamp(startSlotIndex, 0, Mathf.Max(0, slots.Length - 1)));

        ApplySlot(currentSlotIndex);
    }

    private void OnEnable()
    {
        controls.Enable();

        controls.Player.Slot1.performed += _ => EquipOwnedSlot(0);
        controls.Player.Slot2.performed += _ => EquipOwnedSlot(1);
        controls.Player.Slot3.performed += _ => EquipOwnedSlot(2);
        controls.Player.Slot4.performed += _ => EquipOwnedSlot(3);
        controls.Player.Slot5.performed += _ => EquipOwnedSlot(4);
        controls.Player.Slot6.performed += _ => EquipOwnedSlot(5);
        controls.Player.Slot7.performed += _ => EquipOwnedSlot(6);
        controls.Player.Slot8.performed += _ => EquipOwnedSlot(7);
        controls.Player.Slot9.performed += _ => EquipOwnedSlot(8);

        controls.Player.NextSlot.performed += OnNextPerformed;
        controls.Player.PrevSlot.performed += OnPrevPerformed;
    }

    private void OnDisable()
    {
        controls.Player.NextSlot.performed -= OnNextPerformed;
        controls.Player.PrevSlot.performed -= OnPrevPerformed;

        controls.Disable();
    }

    private void OnNextPerformed(InputAction.CallbackContext ctx)
    {
        EquipNextOwnedSlot(+1);
    }

    private void OnPrevPerformed(InputAction.CallbackContext ctx)
    {
        EquipNextOwnedSlot(-1);
    }

    private void EquipNextOwnedSlot(int direction)
    {
        if (OwnedSlotCount <= 0)
            return;

        int currentOwned = GetOwnedIndexFromRealIndex(currentSlotIndex);
        if (currentOwned < 0)
        {
            EquipOwnedSlot(0);
            return;
        }

        int nextOwned = currentOwned + direction;

        if (nextOwned < 0)
            nextOwned = OwnedSlotCount - 1;
        else if (nextOwned >= OwnedSlotCount)
            nextOwned = 0;

        EquipOwnedSlot(nextOwned);
    }

    public void EquipOwnedSlot(int ownedIndex)
    {
        if (OwnedSlotCount <= 0)
            return;

        if (ownedIndex < 0 || ownedIndex >= OwnedSlotCount)
            return;

        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind == ToolKind.Empty)
                continue;

            if (count == ownedIndex)
            {
                EquipRealSlot(i);
                return;
            }

            count++;
        }
    }

    public void EquipRealSlot(int realIndex)
    {
        if (slots == null || slots.Length == 0)
            return;

        realIndex = Mathf.Clamp(realIndex, 0, slots.Length - 1);

        if (slots[realIndex].kind == ToolKind.Empty)
            return;

        if (currentSlotIndex == realIndex)
            return;

        UnequipCurrent();
        currentSlotIndex = realIndex;
        ApplySlot(currentSlotIndex);
    }

    private void UnequipCurrent()
    {
        if (placementTool != null)
        {
            placementTool.enabled = false;
            placementTool.ClearSelectionAndHideGhost();
        }

        // Do NOT disable inspectTool here.
        // It remains the global inspection / upgrade / sell authority.

        if (hoverSelector != null)
        {
            hoverSelector.enabled = false;
        }

        if (slots != null && currentSlotIndex >= 0 && currentSlotIndex < slots.Length)
        {
            if (slots[currentSlotIndex].toolBehaviour != null &&
                slots[currentSlotIndex].toolBehaviour != placementTool &&
                slots[currentSlotIndex].toolBehaviour != inspectTool)
            {
                slots[currentSlotIndex].toolBehaviour.enabled = false;
            }
        }
    }

    private void ApplySlot(int index)
    {
        if (slots == null || slots.Length == 0)
            return;

        Slot slot = slots[index];

        // Inspector stays active globally so upgrades/sell/deselect still work.
        if (inspectTool != null)
            inspectTool.enabled = true;

        //Change this to change what can use the grid hover. Should just be these 3 for right now, maybe more in future.
        bool wantsHover =
    slot.kind == ToolKind.Placement ||
    slot.kind == ToolKind.Inspect ||
    slot.kind == ToolKind.Targeting;

        if (hoverSelector != null)
            hoverSelector.enabled = wantsHover;

        switch (slot.kind)
        {
            case ToolKind.Empty:

                if (placementTool != null)
                    placementTool.enabled = false;

                if (inspectTool != null)
                    inspectTool.SetSelectionPermissions(false, false);

                break;

            case ToolKind.Placement:

                if (placementTool != null)
                    placementTool.enabled = true;

                if (inspectTool != null)
                    inspectTool.SetSelectionPermissions(true, false);

                break;

            case ToolKind.Inspect:

                if (placementTool != null)
                    placementTool.enabled = false;

                if (inspectTool != null)
                    inspectTool.SetSelectionPermissions(true, true);

                break;

            case ToolKind.Mining:
            case ToolKind.Targeting:

                if (placementTool != null)
                    placementTool.enabled = false;

                if (inspectTool != null)
                    inspectTool.SetSelectionPermissions(false, false);

                if (slot.toolBehaviour != null)
                    slot.toolBehaviour.enabled = true;

                break;
        }

        if (debugLogSwitching)
        {
            Debug.Log($"[ToolHotbar] Equipped slot {index + 1} ({slots[index].kind})");
        }

        OnHotbarChanged?.Invoke();
    }

    private int FindNearestOwnedSlot(int startIndex)
    {
        if (slots == null || slots.Length == 0)
            return 0;

        if (slots[startIndex].kind != ToolKind.Empty)
            return startIndex;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind != ToolKind.Empty)
                return i;
        }

        return 0;
    }

    private void AutoWireSlotsIfNeeded()
    {
        if (slots == null || slots.Length != 9)
            slots = new Slot[9];

        bool anyNonEmpty = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].kind != ToolKind.Empty)
            {
                anyNonEmpty = true;
                break;
            }
        }

        if (anyNonEmpty)
            return;

        slots[0] = new Slot { kind = ToolKind.Placement, toolBehaviour = placementTool };
        slots[1] = new Slot { kind = ToolKind.Inspect, toolBehaviour = inspectTool };

        for (int i = 2; i < slots.Length; i++)
        {
            slots[i] = new Slot { kind = ToolKind.Empty, toolBehaviour = null, definition = null };
        }
    }
}