using System;
using UnityEngine;

public enum ItemCooldownKind { None, Seconds, Rounds, Kills }

/// <summary>Player-owned state, never written back into the shared item asset.</summary>
public sealed class ItemRuntimeState
{
    public ToolDefinition Definition { get; }
    public int Charges { get; private set; }
    public ItemCooldownKind CooldownKind { get; private set; }
    public float CooldownRemaining { get; private set; }
    public float CooldownTotal { get; private set; }
    public bool IsOnCooldown => CooldownRemaining > 0f;
    public bool HasLimitedCharges => Definition != null && Definition.maximumCharges > 0;
    public bool CanUse => !PauseState.IsPaused && !IsOnCooldown && (!HasLimitedCharges || Charges > 0);
    public float CooldownFraction => CooldownTotal > 0f ? Mathf.Clamp01(CooldownRemaining / CooldownTotal) : 0f;
    public event Action Changed;

    public ItemRuntimeState(ToolDefinition definition)
    {
        Definition = definition;
        Charges = definition != null ? Mathf.Max(0, definition.maximumCharges) : 0;
    }

    // Call only after the action has found a valid target and will actually succeed.
    public bool TryUse()
    {
        if (!CanUse) return false;
        if (HasLimitedCharges) Charges--;
        if (Definition != null)
            SetCooldown(Definition.cooldownKind, Definition.cooldownAmount, false);
        Changed?.Invoke();
        return true;
    }

    public void StartCooldown(ItemCooldownKind kind, float amount)
    {
        SetCooldown(kind, amount, true);
    }

    private void SetCooldown(ItemCooldownKind kind, float amount, bool notify)
    {
        amount = float.IsNaN(amount) || float.IsInfinity(amount) ? 0f : Mathf.Max(0f, amount);
        if (kind == ItemCooldownKind.Rounds || kind == ItemCooldownKind.Kills)
            amount = Mathf.Ceil(amount);
        CooldownTotal = kind == ItemCooldownKind.None ? 0f : amount;
        CooldownRemaining = CooldownTotal;
        CooldownKind = CooldownTotal > 0f ? kind : ItemCooldownKind.None;
        if (notify) Changed?.Invoke();
    }

    public void AdvanceCooldown(ItemCooldownKind kind, float amount)
    {
        if (PauseState.IsPaused || !IsOnCooldown || kind != CooldownKind ||
            amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        CooldownRemaining = Mathf.Max(0f, CooldownRemaining - amount);
        Changed?.Invoke();
    }

    public void RestoreCharges(int amount)
    {
        if (!HasLimitedCharges || amount <= 0) return;
        Charges = (int)Math.Min(Definition.maximumCharges, (long)Charges + amount);
        Changed?.Invoke();
    }
}
