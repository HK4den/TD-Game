# Enemy camo aura

Add Enemy Camo Aura (Assets/Scripts/Enemy/Specialized Enemy Scripts) to an enemy
with EnemyHealth. Set Radius and Camo Level (1-3), and optionally Include Self.
Assign its Enemy Radius Visualizer, or leave the reference empty to find a child.

The radius is a circle in the horizontal X/Z plane. Allies within it receive the
strongest active aura level; multiple camo auras never add their levels together.
Intrinsic camo is not reduced: effective camo is the stronger of intrinsic camo and
aura camo, plus current terrain camo, capped at 4. Thus a level-3 aura in brush
produces level 4, and two level-2 auras still provide only level 2 before terrain.
Beam-protected allies can receive the buff but remain damage-immune.

Leaving the radius updates at Refresh Interval (default 0.2 seconds). Death,
disable, or destruction of the emitter removes its grants immediately. Weaker
remaining auras still work. Assign distinct visualizers if multiple aura components
on the same enemy need different displayed radii.

# Shared enemy radius visualizer

The existing Enemy Radius Visualizer now flattens its assigned mesh/sprite onto
the grid's ground height. It sizes the two surface axes to the actual aura diameter
and uses Ground Thickness for the thin vertical axis. It accounts for mesh bounds
instead of assuming every visual is a unit-sized X/Z plane. Runtime visuals are
detached from scaled/rotated enemy parents and cleaned up with their owner.

Visual Object should be a dedicated child mesh/sprite, not the enemy root or the
object holding Enemy Radius Visualizer. Use a visual-only object without colliders.
The terrain reference is the GridManager plane, matching the game's flat grid;
without a grid it falls back to the owning enemy's root height. It is not a terrain
decal that conforms to arbitrary slopes.

Each later-created visual is a tiny amount LOWER than earlier visuals. The total
drop stays below 0.005 units so even long games do not sink auras underground.
Renderer sorting order also prioritizes earlier visuals for transparent materials.
Y Offset has a small safety minimum to keep the effect above the ground.

Spin settings on Enemy Radius Visualizer:

- Spin: enables rotation around world up. Off by default.
- Spin Direction: Clockwise, Counterclockwise, or Random per instance (viewed from above).
- Randomize Spin Speed: off uses Spin Speed for every instance of that prefab;
  on chooses between Minimum Spin Speed and Maximum Spin Speed at creation.
- Speeds are degrees per second; use 5 and 8 for a random 5-8 degree/second spin.

Spin pauses with the game. Existing material, radius, and timed/always-visible
APIs remain available to healing, damage-reduction, and siphon effects. The visual
hides when its owner dies or the visualizer is disabled.

Play-mode checks: different mesh sizes and enemy parent scales, overlapping aura
order, fixed/random spin, pause/resume, camo aura overlap with different levels,
leaving range, emitter death, and terrain camo bonuses.
