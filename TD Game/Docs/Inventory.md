# Inventory and held items

The hotbar is a compact list of owned items, with up to nine items. Empty slots remain hidden. Classes are not implemented here.

## Item definitions and behaviours

Create a Tool Definition asset for each item. Its name, icon, held prefab, and availability defaults describe the item; they do not hold player progress.

Existing Placement, Inspect, Mining, and Targeting kinds retain their scene bindings. To add a new kind of action without editing the hotbar, choose Custom, give the definition a `behaviourId`, and add the same ID and a scene component to the hotbar's Additional Item Behaviours list. That component should implement `IToolEquipBehaviour`:

- `Equip(ItemRuntimeState)` receives the particular player's item instance. Store it to check availability or spend a use.
- `Unequip()` stops the action and clears temporary previews/input. Implementations must gate input while paused.
- The three permission properties describe grid hover and the global inspector's selection permissions.

The inspector stays enabled for upgrading, selling, and deselecting even when it is not held. Placement and targeting implement the contract and preserve their existing selection rules. A legacy component without the interface can still be enabled/disabled, but has no selection permissions by default.

## Per-item state

`CurrentItem` and `GetOwnedSlot(index).runtimeState` provide `ItemRuntimeState`. Duplicate copies have independent state. Switching, reordering, adding another item, and replacing a loadout with retained definitions preserve state. Removing an item discards its state; adding it again creates a fresh instance. State is runtime-only and is not saved across sessions.

Call `TryUse()` only once an action has a valid target and can succeed. Failed availability checks do not consume a charge. Existing unlimited tools do not call this API automatically; future limited-use behaviours must opt in at the successful action point.

- Maximum Charges = 0 means unlimited uses. Positive values start full, are spent on successful uses, and require explicit `RestoreCharges(amount)` calls to refill.
- Seconds cooldowns advance with game time and freeze during pause, victory, or defeat.
- Rounds cooldowns advance when a wave completes.
- Kills cooldowns count finalized deaths among active wave members. Enemies reaching the base do not count. This is the team's wave kill count, not individual player kill attribution.
- `StartCooldown(kind, amount)` supports behaviour-controlled cooldowns. Round and kill amounts are whole units.

An unlimited-charge item with a Rounds cooldown of 1 can become usable again when the current wave finishes. No classes, class abilities, progression, or passive skills are added by this system.

## UI

Changing the selected item briefly shows its name above the compact hotbar. The existing selected-background art stays intact. The label uses the hotbar's existing font and can be overridden with an assigned Text component.

An active cooldown uses the definition's optional Cooldown Icon; otherwise the ordinary icon dims. A circle shows remaining progress, with discrete cuts for rounds/kills and a smooth countdown for seconds. Large counts use at most 24 visual segments; the central number still shows the actual remaining requirement. Limited-charge items show their remaining uses and dim when exhausted. Current tools have no limits or cooldown configured, so they have no availability overlay.

## Held animations

Shared lowering and raising remain in HeldItemView. An optional HeldItemAnimation component on the visual prefab can trigger an Animator or UnityEvent after raising finishes and when lowering begins. Reordering or adding unrelated items does not restart the held animation.

## Verification

`dotnet msbuild Tests/Inventory/InventoryChecks.csproj -restore -target:Build` runs the production inventory/state code against minimal engine stubs. It checks independent copies, loadout mutation, selection preservation, custom behavior callbacks, charges, pause, and cooldown progress. This does not validate rendering or Unity scene behaviour; those still need play-mode checks.
