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

        controls.Player.Slot1.performed += OnSlot1Performed;
        controls.Player.Slot2.performed += OnSlot2Performed;
        controls.Player.Slot3.performed += OnSlot3Performed;
        controls.Player.Slot4.performed += OnSlot4Performed;
        controls.Player.Slot5.performed += OnSlot5Performed;
        controls.Player.Slot6.performed += OnSlot6Performed;
        controls.Player.Slot7.performed += OnSlot7Performed;
        controls.Player.Slot8.performed += OnSlot8Performed;
        controls.Player.Slot9.performed += OnSlot9Performed;

        controls.Player.NextSlot.performed += OnNextPerformed;
        controls.Player.PrevSlot.performed += OnPrevPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Slot1.performed -= OnSlot1Performed;
        controls.Player.Slot2.performed -= OnSlot2Performed;
        controls.Player.Slot3.performed -= OnSlot3Performed;
        controls.Player.Slot4.performed -= OnSlot4Performed;
        controls.Player.Slot5.performed -= OnSlot5Performed;
        controls.Player.Slot6.performed -= OnSlot6Performed;
        controls.Player.Slot7.performed -= OnSlot7Performed;
        controls.Player.Slot8.performed -= OnSlot8Performed;
        controls.Player.Slot9.performed -= OnSlot9Performed;
        controls.Player.NextSlot.performed -= OnNextPerformed;
        controls.Player.PrevSlot.performed -= OnPrevPerformed;

        controls.Disable();
    }

    private void OnSlot1Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(0);
    private void OnSlot2Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(1);
    private void OnSlot3Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(2);
    private void OnSlot4Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(3);
    private void OnSlot5Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(4);
    private void OnSlot6Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(5);
    private void OnSlot7Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(6);
    private void OnSlot8Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(7);
    private void OnSlot9Performed(InputAction.CallbackContext ctx) => EquipOwnedSlot(8);

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
        if (PauseState.IsPaused)
            return;

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
        if (PauseState.IsPaused)
            return;

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
        if (PauseState.IsPaused)
            return;

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
