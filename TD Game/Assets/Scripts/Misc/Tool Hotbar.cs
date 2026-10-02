using System;
using System.Collections.Generic;
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
        Custom = 5,
    }

    [Serializable]
    public struct Slot
    {
        public ToolKind kind;
        public ToolDefinition definition;

        [Tooltip("Enable/disable this behaviour when the slot is equipped. Leave null for Empty.")]
        public Behaviour toolBehaviour;
        [NonSerialized] public ItemRuntimeState runtimeState;
    }

    [Serializable]
    public struct BehaviourBinding
    {
        public string id;
        public Behaviour behaviour;
    }

    [Header("Additional Item Behaviours")]
    [SerializeField] private BehaviourBinding[] behaviourBindings = Array.Empty<BehaviourBinding>();

    [Header("Slots (size should be 9 max)")]
    [SerializeField] private Slot[] slots = new Slot[9];

    [Header("Tool Behaviours (existing scripts)")]
    [SerializeField] private TowerPlacementController placementTool;
    [SerializeField] private TowerInspectorTool inspectTool;
    [SerializeField] private Behaviour miningTool;
    [SerializeField] private Behaviour targetingTool;

    [Header("Visual-only hover highlight (optional)")]
    [SerializeField] private GridHoverSelector hoverSelector;

    [Header("Start State")]
    [SerializeField] private int startSlotIndex = 0;
    [SerializeField] private bool debugLogSwitching = true;

    private int currentSlotIndex;
    private PlayerControls controls;
    private WaveSpawner waveSpawner;
    private Behaviour equippedBehaviour;
    private ItemRuntimeState lastAnnouncedItem;
    private bool equipmentSuspended;
    public ItemRuntimeState CurrentItem => CurrentSlot.runtimeState;
    public bool IsBehaviourEquipped(Behaviour behaviour) => behaviour != null && equippedBehaviour == behaviour;

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
        if (slots == null || ownedIndex < 0) return default;
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
    public event Action OnEquippedItemChanged;

    // The visible hotbar remains a compact list of owned items. Call these methods
    // when a class or level changes the player's loadout.
    public bool SetLoadout(IReadOnlyList<ToolDefinition> definitions)
    {
        if (definitions == null || definitions.Count > 9)
            return false;

        List<Slot> loadout = new List<Slot>();
        List<Slot> previous = GetLoadout();
        for (int i = 0; i < definitions.Count; i++)
            if (definitions[i] != null && definitions[i].toolKind != ToolKind.Empty)
            {
                int retained = previous.FindIndex(slot => slot.definition == definitions[i]);
                if (retained >= 0)
                {
                    loadout.Add(previous[retained]);
                    previous.RemoveAt(retained);
                }
                else loadout.Add(MakeSlot(definitions[i]));
            }
        return SetSlots(loadout);
    }

    private bool SetSlots(IReadOnlyList<Slot> loadout, int selectedOwnedIndex = -1)
    {
        if (loadout == null || loadout.Count > 9)
            return false;

        ToolDefinition selected = CurrentSlot.definition;
        ItemRuntimeState selectedItem = CurrentItem;
        ToolKind selectedKind = CurrentSlot.kind;
        UnequipCurrent();
        if (slots == null || slots.Length != 9)
            slots = new Slot[9];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = default;

        int count = 0;
        for (int i = 0; i < loadout.Count; i++)
        {
            Slot slot = loadout[i];
            if (slot.kind == ToolKind.Empty)
                continue;
            if (slot.runtimeState == null) slot.runtimeState = new ItemRuntimeState(slot.definition);
            slots[count++] = slot;
        }

        currentSlotIndex = 0;
        if (selectedOwnedIndex >= 0 && selectedOwnedIndex < count)
            currentSlotIndex = selectedOwnedIndex;
        else if (selectedKind != ToolKind.Empty)
        {
            for (int i = 0; i < count; i++)
                if ((selectedItem != null && slots[i].runtimeState == selectedItem) ||
                    (selectedItem == null && slots[i].kind == selectedKind && slots[i].definition == selected))
                { currentSlotIndex = i; break; }
        }
        ApplySlot(currentSlotIndex);
        return true;
    }

    public bool AddItem(ToolDefinition definition)
    {
        if (definition == null || definition.toolKind == ToolKind.Empty || OwnedSlotCount >= 9)
            return false;
        List<Slot> loadout = GetLoadout();
        loadout.Add(MakeSlot(definition));
        return SetSlots(loadout);
    }

    public bool RemoveItemAt(int ownedIndex)
    {
        List<Slot> loadout = GetLoadout();
        if (ownedIndex < 0 || ownedIndex >= loadout.Count)
            return false;
        loadout.RemoveAt(ownedIndex);
        return SetSlots(loadout);
    }

    public bool MoveItem(int fromOwnedIndex, int toOwnedIndex)
    {
        List<Slot> loadout = GetLoadout();
        if (fromOwnedIndex < 0 || fromOwnedIndex >= loadout.Count ||
            toOwnedIndex < 0 || toOwnedIndex >= loadout.Count)
            return false;
        Slot moved = loadout[fromOwnedIndex];
        loadout.RemoveAt(fromOwnedIndex);
        loadout.Insert(toOwnedIndex, moved);
        int selected = GetOwnedIndexFromRealIndex(currentSlotIndex);
        if (selected == fromOwnedIndex) selected = toOwnedIndex;
        else if (fromOwnedIndex < selected && selected <= toOwnedIndex) selected--;
        else if (toOwnedIndex <= selected && selected < fromOwnedIndex) selected++;
        return SetSlots(loadout, selected);
    }

    private List<Slot> GetLoadout()
    {
        List<Slot> loadout = new List<Slot>();
        if (slots == null) return loadout;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].kind != ToolKind.Empty)
                loadout.Add(slots[i]);
        return loadout;
    }

    private Slot MakeSlot(ToolDefinition definition)
    {
        return new Slot
        {
            kind = definition.toolKind,
            definition = definition,
            toolBehaviour = ResolveBehaviour(definition.toolKind),
            runtimeState = new ItemRuntimeState(definition)
        };
    }

    private Behaviour ResolveBehaviour(ToolKind kind)
    {
        switch (kind)
        {
            case ToolKind.Placement: return placementTool;
            case ToolKind.Inspect: return inspectTool;
            case ToolKind.Mining: return miningTool;
            case ToolKind.Targeting: return targetingTool;
            default: return null;
        }
    }

    private Behaviour ResolveBehaviour(Slot slot)
    {
        if (slot.definition != null && !string.IsNullOrWhiteSpace(slot.definition.behaviourId))
        {
            if (behaviourBindings != null)
                foreach (BehaviourBinding binding in behaviourBindings)
                    if (binding.id == slot.definition.behaviourId) return binding.behaviour;
            Debug.LogWarning($"No item behaviour binding for '{slot.definition.behaviourId}'.", this);
            return null;
        }
        return slot.toolBehaviour != null ? slot.toolBehaviour : ResolveBehaviour(slot.kind);
    }

    private void Update()
    {
        if (PauseState.IsPaused || slots == null) return;
        foreach (Slot slot in slots)
            slot.runtimeState?.AdvanceCooldown(ItemCooldownKind.Seconds, Time.deltaTime);
    }

    private void HandleWaveCompleted(int wave, int reward) => AdvanceItemCooldowns(ItemCooldownKind.Rounds, 1);
    private void HandleEnemyKilled() => AdvanceItemCooldowns(ItemCooldownKind.Kills, 1);

    private void AdvanceItemCooldowns(ItemCooldownKind kind, int amount)
    {
        if (slots == null) return;
        foreach (Slot slot in slots) slot.runtimeState?.AdvanceCooldown(kind, amount);
    }

    private void Awake()
    {
        controls = new PlayerControls();
        waveSpawner = FindFirstObjectByType<WaveSpawner>();

        AutoWireSlotsIfNeeded();

        if (placementTool != null)
            placementTool.enabled = false;

        if (inspectTool != null)
            inspectTool.enabled = false;

        if (hoverSelector != null)
            hoverSelector.enabled = false;

        for (int i = 0; i < slots.Length; i++)
        {
            Behaviour slotBehaviour = ResolveBehaviour(slots[i]);
            if (slotBehaviour != null && slotBehaviour != inspectTool)
                slotBehaviour.enabled = false;
        }

        currentSlotIndex = FindNearestOwnedSlot(
            Mathf.Clamp(startSlotIndex, 0, Mathf.Max(0, slots.Length - 1)));

        ApplySlot(currentSlotIndex);
    }

    private void OnEnable()
    {
        if (equipmentSuspended)
        {
            equipmentSuspended = false;
            ApplySlot(currentSlotIndex);
        }
        if (waveSpawner != null)
        {
            waveSpawner.OnWaveCompleted += HandleWaveCompleted;
            waveSpawner.OnEnemyKilled += HandleEnemyKilled;
        }
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
        if (waveSpawner != null)
        {
            waveSpawner.OnWaveCompleted -= HandleWaveCompleted;
            waveSpawner.OnEnemyKilled -= HandleEnemyKilled;
        }
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
        UnequipCurrent();
        equipmentSuspended = true;
    }

    private void OnDestroy() => controls?.Dispose();

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
        if (equippedBehaviour is IToolEquipBehaviour handler)
            handler.Unequip();
        else if (equippedBehaviour != null && equippedBehaviour != inspectTool)
            equippedBehaviour.enabled = false;
        equippedBehaviour = null;
        if (hoverSelector != null)
        {
            hoverSelector.enabled = false;
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

        equippedBehaviour = slot.kind != ToolKind.Empty ? ResolveBehaviour(slot) : null;
        IToolEquipBehaviour handler = equippedBehaviour as IToolEquipBehaviour;
        if (hoverSelector != null)
            hoverSelector.enabled = handler != null && handler.UsesGridHover;
        if (inspectTool != null)
            inspectTool.SetSelectionPermissions(handler != null && handler.AllowsTowerSelection,
                handler != null && handler.AllowsEmptyTileSelection);
        if (handler != null) handler.Equip(slot.runtimeState);
        else if (equippedBehaviour != null) equippedBehaviour.enabled = true;

        if (debugLogSwitching)
        {
            Debug.Log($"[ToolHotbar] Equipped slot {index + 1} ({slots[index].kind})");
        }

        OnHotbarChanged?.Invoke();
        if (lastAnnouncedItem != slot.runtimeState)
        {
            lastAnnouncedItem = slot.runtimeState;
            OnEquippedItemChanged?.Invoke();
        }
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
        if (slots == null)
            slots = new Slot[9];
        else if (slots.Length != 9)
            Array.Resize(ref slots, 9);

        bool anyConfiguredSlot = false;

        for (int i = 0; i < slots.Length; i++)
        {
            Slot slot = slots[i];

            // The definition is the item being equipped; keep its behavior and view in sync.
            if (slot.definition != null)
                slot.kind = slot.definition.toolKind;

            if (slot.toolBehaviour == null)
                slot.toolBehaviour = ResolveBehaviour(slot.kind);
            if (slot.kind != ToolKind.Empty)
                slot.runtimeState = new ItemRuntimeState(slot.definition);

            slots[i] = slot;
            if (slot.kind != ToolKind.Empty)
                anyConfiguredSlot = true;
        }

        if (anyConfiguredSlot)
            return;

        slots[0] = new Slot
        {
            kind = ToolKind.Placement,
            definition = null,
            toolBehaviour = placementTool,
            runtimeState = new ItemRuntimeState(null)
        };
        slots[1] = new Slot
        {
            kind = ToolKind.Inspect,
            definition = null,
            toolBehaviour = inspectTool,
            runtimeState = new ItemRuntimeState(null)
        };
    }
}
