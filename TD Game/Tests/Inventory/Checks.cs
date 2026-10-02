using System;
using System.Reflection;

static class Checks
{
    sealed class TestTool : UnityEngine.Behaviour, IToolEquipBehaviour
    {
        public bool UsesGridHover => true;
        public bool AllowsTowerSelection => false;
        public bool AllowsEmptyTileSelection => false;
        public ItemRuntimeState Item;
        public void Equip(ItemRuntimeState item) { Item = item; enabled = true; }
        public void Unequip() { Item = null; enabled = false; }
    }
    static int checks;
    static void Assert(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
    }
    static void Invoke(object obj, string method) => obj.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(obj, null);
    static void Main()
    {
        ToolDefinition limited = new ToolDefinition { maximumCharges = 2, cooldownKind = ItemCooldownKind.Seconds, cooldownAmount = 3 };
        ItemRuntimeState first = new ItemRuntimeState(limited), second = new ItemRuntimeState(limited);
        Assert(first.TryUse() && first.Charges == 1, "Valid use spends one charge");
        Assert(second.Charges == 2 && second.CanUse, "Copies have independent state");
        Assert(!first.TryUse() && first.Charges == 1, "Blocked use cannot spend charges");
        first.AdvanceCooldown(ItemCooldownKind.Rounds, 1);
        Assert(first.CooldownRemaining == 3, "Wrong progress source cannot reduce cooldown");
        PauseState.SetPaused(true);
        first.AdvanceCooldown(ItemCooldownKind.Seconds, 3);
        Assert(first.CooldownRemaining == 3 && !second.TryUse(), "Pause freezes progress and use");
        PauseState.SetPaused(false);
        first.AdvanceCooldown(ItemCooldownKind.Seconds, 1);
        Assert(Math.Abs(first.CooldownFraction - 2f / 3f) < 0.001f, "Time countdown supplies normalized UI progress");
        first.AdvanceCooldown(ItemCooldownKind.Seconds, 10);
        Assert(first.CanUse && first.CooldownRemaining == 0, "Cooldown completes without negative remaining time");
        Assert(first.TryUse(), "Second charge usable after cooldown");
        first.AdvanceCooldown(ItemCooldownKind.Seconds, 3);
        Assert(!first.CanUse, "Exhausted charges stay unavailable until explicitly restored");
        first.RestoreCharges(int.MaxValue);
        Assert(first.Charges == 2 && first.CanUse, "Restore clamps safely to capacity");
        first.StartCooldown(ItemCooldownKind.Rounds, 2.1f);
        first.AdvanceCooldown(ItemCooldownKind.Rounds, 1);
        Assert(first.CooldownRemaining == 2, "Round requirements are whole units");
        first.StartCooldown(ItemCooldownKind.Kills, 2);
        first.AdvanceCooldown(ItemCooldownKind.Kills, 2);
        Assert(first.CanUse, "Kill-based cooldown completes");
        Assert(limited.maximumCharges == 2 && limited.cooldownAmount == 3, "Shared asset defaults stay unchanged");

        ToolHotbar hotbar = new ToolHotbar();
        Invoke(hotbar, "Awake");
        ToolDefinition other = new ToolDefinition();
        Assert(hotbar.SetLoadout(new[] { limited, limited, other }), "Replace loadout");
        ItemRuntimeState copyA = hotbar.GetOwnedSlot(0).runtimeState;
        ItemRuntimeState copyB = hotbar.GetOwnedSlot(1).runtimeState;
        copyB.StartCooldown(ItemCooldownKind.Rounds, 2);
        hotbar.EquipOwnedSlot(1);
        int equipEvents = 0;
        hotbar.OnEquippedItemChanged += () => equipEvents++;
        Assert(hotbar.MoveItem(1, 2) && hotbar.CurrentItem == copyB, "Reorder preserves exact selected copy");
        Assert(equipEvents == 0, "Reorder does not restart held-item animation");
        Assert(hotbar.AddItem(other) && hotbar.CurrentItem == copyB, "Adding does not disturb selected item");
        Assert(hotbar.RemoveItemAt(0) && hotbar.CurrentItem == copyB, "Removing another item preserves selection");
        Assert(hotbar.SetLoadout(new[] { limited, other }) && hotbar.GetOwnedSlot(0).runtimeState == copyB,
            "Retained item keeps cooldown when loadout changes");
        Assert(copyB.CooldownRemaining == 2 && copyA != copyB, "Per-copy state survives operations");
        Assert(!hotbar.SetLoadout(new ToolDefinition[10]) && hotbar.OwnedSlotCount == 2, "Oversized loadout rejects without modifying inventory");
        Assert(!hotbar.RemoveItemAt(8) && !hotbar.MoveItem(-1, 0), "Invalid mutations reject safely");
        Assert(hotbar.SetLoadout(Array.Empty<ToolDefinition>()) && hotbar.CurrentItem == null && hotbar.OwnedSlotCount == 0,
            "Empty loadout clears selection");
        Assert(hotbar.AddItem(other) && hotbar.CurrentItem.Definition == other, "First added item becomes equipped");

        TestTool custom = new TestTool();
        typeof(ToolHotbar).GetField("behaviourBindings", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(hotbar, new[] { new ToolHotbar.BehaviourBinding { id = "book", behaviour = custom } });
        ToolDefinition book = new ToolDefinition { behaviourId = "book" };
        hotbar.SetLoadout(new[] { book, other });
        hotbar.EquipOwnedSlot(0);
        Assert(custom.enabled && custom.Item == hotbar.CurrentItem && hotbar.IsBehaviourEquipped(custom),
            "Custom binding receives exact equipped instance through the contract");
        hotbar.EquipOwnedSlot(1);
        Assert(!custom.enabled && custom.Item == null, "Switching invokes custom unequip");
        hotbar.EquipOwnedSlot(0);
        Invoke(hotbar, "OnDisable");
        Assert(!custom.enabled, "Disabling hotbar disables equipped action");

        WaveSpawner spawner = new WaveSpawner();
        typeof(ToolHotbar).GetField("waveSpawner", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hotbar, spawner);
        Invoke(hotbar, "OnEnable");
        Assert(custom.enabled && custom.Item == hotbar.CurrentItem, "Re-enabling restores equipment context");
        hotbar.CurrentItem.StartCooldown(ItemCooldownKind.Rounds, 2);
        spawner.KillEnemy();
        Assert(hotbar.CurrentItem.CooldownRemaining == 2, "Kills do not advance round cooldown");
        spawner.CompleteWave();
        Assert(hotbar.CurrentItem.CooldownRemaining == 1, "Wave completion advances round cooldown");
        hotbar.CurrentItem.StartCooldown(ItemCooldownKind.Kills, 2);
        spawner.KillEnemy();
        Assert(hotbar.CurrentItem.CooldownRemaining == 1, "Wave kill event advances kill cooldown");
        hotbar.CurrentItem.StartCooldown(ItemCooldownKind.Seconds, 1);
        PauseState.SetPaused(true);
        Invoke(hotbar, "Update");
        Assert(hotbar.CurrentItem.CooldownRemaining == 1, "Hotbar update respects pause");
        PauseState.SetPaused(false);
        Invoke(hotbar, "Update");
        Assert(Math.Abs(hotbar.CurrentItem.CooldownRemaining - 0.9f) < 0.001f, "Hotbar advances timed cooldown");
        Console.WriteLine($"Passed {checks} inventory checks.");
    }
}
