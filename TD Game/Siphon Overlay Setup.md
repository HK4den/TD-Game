# Siphon damage and protected-enemy flash

Redirected siphon damage bypasses the recipient's beam protection and camo level.
The original target must still pass its usual damage checks before siphoning starts.
Normal health loss, damage modifiers, damage events, regeneration cooldowns and death
handling remain active on the siphoner. A fully siphoned original hit produces no
damage event or number on the protected enemy.

On Enemy Nearby Damage Siphon, under Protected Enemy Flash:

- Show Siphon Overlay is enabled by default.
- Assign Siphon Overlay Material.
- Siphon Overlay Duration defaults to 0.1 seconds.

The flash appears on the enemy protected by the siphon, not the siphoner. Use a
transparent overlay material compatible with the project's rendering pipeline,
preferably with depth writes disabled and suitable depth bias to avoid z-fighting.
An opaque material will obscure the underlying appearance while active.

The helper is added automatically on the first flash. It renders an extra pass for
each mesh/submesh without replacing original materials, follows skinned animation
and blend shapes, and excludes radius visuals. MeshRenderer and SkinnedMeshRenderer
are supported. Repeated hits refresh one overlay rather than creating more objects.
The duration pauses with the game; overlays hide on disable and are cleaned up on
destruction. The normal original materials are never modified.

Verify full/partial siphon, a camouflaged or beam-protected siphoner, the protected
enemy's flash, repeated hits, pause during a flash, and death while flashing.
