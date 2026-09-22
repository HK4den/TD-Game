# Performance review — 2026-09-21

This is a source-code review, not a Unity Profiler capture. Priorities below reflect
repeated work visible in the code; no FPS gain or measured bottleneck is claimed.

## Implemented: enemy aura pass

- Enemy Camo Aura now iterates EnemyRegistry instead of allocating a whole-scene
  FindObjectsByType result every refresh. Its X/Z distance test, refresh interval,
  strongest-aura rule, terrain bonuses, and source cleanup are unchanged.
- EnemyAgent registers/unregisters itself on enable/disable. The existing registry
  component can remain in scenes but no longer owns/clears the shared list. This
  also supports enemies re-enabled after being disabled, and aura-only test scenes.
  Spawn/removal events used by other systems remain unchanged.
- EnemyAgent caches health and damage-modifier references for aura queries.
- Damage-reduction auras reuse those references instead of repeatedly searching
  parents for health and damage modifiers on every refresh.
- EnemyDamageTakenController recalculates its combined multiplier when entries
  expire, are added, or change strength; refreshing the same strength only extends
  its expiry. It no longer rebuilds the same result every frame.
- EnemyRadiusVisualizer ignores unchanged radius/visibility requests, caches mesh
  scale until radius/thickness changes, and skips hidden-visual pose updates.
  Visible positioning, spin, ground height, and overlap ordering remain active.
- Healing and damage-reduction aura physics queries already use reusable arrays
  and deduplicated targets. Their sphere/collider/layer-mask semantics were retained.

Validation: Assembly-CSharp-Editor build succeeded with zero warnings/errors.
Play-mode validation and a before/after profile have not been performed here.

## Implemented: broader optimization pass

- Cached cumulative route distances in Enemy Agent. First/Last targeting now reads
  one cached distance plus the current partial segment instead of walking the
  remaining route for every candidate. Cache rebuilds on repath; grid tile positions
  are assumed static between repaths, as in the existing authored grid.
- Reused A* lists, sets and dictionaries across searches. Returned paths remain
  independent lists. Added hash-set open membership instead of linear Contains
  searches. Lowest-F selection and tie ordering are unchanged.
- Added an active Tower Combat Stats registry. Detection auras no longer allocate
  whole-scene search results every refresh; disabled towers leave the registry.
- Changed detection-aura old/current membership to hash sets, avoiding repeated
  linear membership searches. Tick intervals and exact range tests are unchanged.
- Cached tower power, attack interval, pierce and range totals until modifiers or
  base values change. Inspector validation and runtime setters invalidate caches.
- Cached the total external camo detection grant. Source changes/removals update
  it; disabling external detection still returns zero without discarding grants.
- Cached each enemy's strongest camo aura level. Only changed/removed sources
  trigger recomputation; intrinsic camo and terrain bonuses remain dynamic.
- Enemy Slow Controller recalculates only when slows change or expire, rather
  than every frame. Same-strength slow/speed-buff refreshes only extend expiry.
- Expired siphon overlays disable their update work after hiding; Show wakes them
  again. Active flash timing, pause behavior and renderer updates are retained.
- Tower targeting consumes the existing range result directly instead of copying
  every candidate. Strong/Weak targeting uses cached EnemyAgent health references.

Validation: Assembly-CSharp-Editor compiled successfully with zero warnings/errors.
No Unity play-mode run or before/after performance measurement was performed.
Check tower spawn/sell/disable/re-enable, overlapping detection grants, upgrades,
slow expiration, overlay retriggering, and First/Last targeting after placing towers.

### Status against the original review

| Item | Status | Remaining work |
| --- | --- | --- |
| 1. Remaining path distance | Complete | Profile with many First/Last towers |
| 2. Spatial lookup | Complete | Play-mode and performance measurement |
| 3. Tower registry | Complete | Play-mode lifecycle checks |
| 4. Shared path results | Complete | Play-mode placement/repath checks |
| 5. A* storage/open list | Complete | Profile large grids |
| 6. Projectile/audio pooling | Complete for tower projectiles and combat one-shots | Unity lifecycle/audio checks |
| 7. Placement preview cache | Complete | Visual play-mode checks |
| 8. Inactive overlays | Complete | Play-mode pause/retrigger checks |
| 9. Slow/speed refreshes | Complete | Play-mode expiration checks |
| 10. Combat/camo totals | Complete | Play-mode upgrade and source removal checks |
| 11. Shared spawn references | Complete | Scene transition checks |
| 12. Empty-range polling | Addressed by spatial lookup | Queries retain immediate acquisition timing |

Physics overlap buffers now grow and retry whenever full. Aura refresh rates,
attack timing, target limits, and movement tuning were not reduced to save work.

## Implemented: remaining optimization pass

- EnemyRegistry maintains X/Z position buckets. Tower range queries and camo auras
  request nearby candidates, then perform their original exact range and eligibility
  tests. Candidate order follows registration order to preserve targeting ties.
  Positions synchronize once per frame and immediately after EnemyAgent movement.
  Empty buckets are removed. Extremely broad queries fall back to the registry.
- Empty-range tower polling now checks local candidates instead of all enemies.
  No retry delay or target-acquisition latency was introduced.
- GridPathfinder caches up to 256 start/goal/blocked-start results, including no-path
  results. It invalidates on grid navigation changes, path version changes and grid
  replacement. Callers receive separate lists so they cannot corrupt cached paths.
- GridTile invalidates navigation on blocking/terrain/identity/lifecycle changes.
  Temporary preview blocking and restoration invalidate hypothetical results.
- A binary heap replaces the lowest-F linear scan. Insertion order breaks ties,
  including after priority decreases, matching the previous A* route choices.
- Enemy repathing and preview hovering no longer rebuild the entire grid lookup.
  The grid initializes its lookup lazily; runtime tile-layout changes should call
  GridManager.RebuildLookupFromChildren, which also invalidates cached routes.
- Placement previews cache their baseline path and reuse marker sets, lookup
  dictionaries and removal lists. Temporary blocking is restored in a finally block.
- Tower projectiles reuse instances per prefab and scene, retaining up to 128 idle
  instances per prefab. Initialize resets hit history, pierce, damage, direction,
  lifetime, slow source and detection. Spawn resets pose/scale, trails and particles.
  Billboard random rotation choice refreshes on enable.
- Tower firing, enemy death and explosion sounds reuse DestroyAfterAudio prefabs
  (up to 32 idle instances per prefab/scene). Each reuse restarts its timer and
  randomized pitch. Prefabs without DestroyAfterAudio retain normal instantiation.
  Existing mixer routing remains on the prefab AudioSource. Other sound spawn
  sites can adopt the same helper later; puddles and visual death effects are not pooled.
- Pool entries are cleared for the unloaded scene. Inactive objects belong to that
  scene and are destroyed by Unity when it unloads.
- Enemy Agent, Enemy Health, radius visuals and acid puddles share cached scene
  manager references. Unloading a scene clears the reference cache.
- Healing, damage reduction, death explosion and acid puddle overlap buffers double
  and retry when full, retaining the expanded arrays for subsequent queries.

Validation:
- Assembly-CSharp-Editor build: zero warnings/errors.
- A temporary standalone C# harness compiled the actual PathOpenHeap,
  GridPathfinder and EnemyRegistry sources against minimal Unity stubs.
- 4,000 deterministic randomized path/cache comparisons matched the previous
  list-based algorithm exactly. Additional checks covered blocker restoration,
  path-version invalidation and mutation of returned lists.
- 2,000 spatial queries matched full scans after movement, preserving registration
  order; removal left no stale candidates.
- These checks do not exercise Unity physics, audio, rendering or object lifecycle.
  Play-mode testing and before/after Profiler captures remain outstanding.

All original coding recommendations are now addressed within the scope above.
Next validation: play a crowded wave; check projectile reuse/pierce, acid impacts,
audio pitch/volume/pause, scene reload, tower upgrades, camo overlap, aura coverage
above 64 colliders, and placement-preview changes.

## Original recommendations (see status table above)

1. **Cache remaining path distance.** Enemy Agent's CalculateRemainingPathDistance
   walks the remaining route for each query; Tower Targeting Controller calls it
   per candidate for First/Last targeting. Store cumulative segment distances on
   repath, then add the current partial segment. This avoids repeated route walks
   across towers while preserving target decisions.
2. **Spatial lookup for tower/enemy queries.** Tower Range Query scans the full
   enemy registry for each request. Camo auras now avoid scene searches but still
   scan that list. A shared grid/spatial index can return nearby candidates before
   the existing exact range/camo checks. Preserve multi-box ranges and collider
   semantics where those are currently used.
3. **Tower registry for detection auras.** Tower Aura Grant Camo Detection still
   calls FindObjectsByType<TowerCombatStats> every refresh. Maintain active towers
   through their lifecycle and use hash sets for old/current target membership.
4. **Shared pathfinding results.** Every enemy ForceRepath performs A* after a path
   version change. Cache by start tile, goal, path version and blocked-start policy,
   or evaluate a reverse distance field for the common goal. Placement previews
   temporarily block tiles, so they must not accidentally reuse stale route data.
5. **Reduce A* allocations and open-list work.** Grid Path Finder creates lists,
   hash sets and dictionaries per search and scans the open list to find lowest F.
   Reusable search storage and a priority queue are candidates. Preserve tie-breaks
   if exact enemy route choices are important.
6. **Pool projectiles and one-shot sounds.** Tower Projectile Emitter creates both
   during combat; projectiles and death effects are later destroyed. Start with
   these short-lived objects before pooling enemies. Reset hit lists, pierce,
   lifetime, source detection and audio state when reusing objects.
7. **Cache current path for placement previews.** Grid Path Preview Visualizer
   already refreshes only on relevant changes, but moving between hovered tiles
   recomputes the unchanged current path as well as the hypothetical one, and builds
   temporary sets. Cache the baseline by path version and reuse scratch sets.
8. **Sleep inactive siphon overlays.** Enemy Siphon Overlay continues its renderer
   loop every frame after its flash expires. Disable its update work after hiding
   once and wake it in Show; preserve pause, death, and blend-shape handling.
9. **Recalculate slows only on change.** Enemy Slow Controller rebuilds its
   multiplier every Update and on same-strength refreshes. Apply the dirty/expiry
   strategy used in the damage-modifier optimization above. EnemyAgent speed-buff
   refreshes can similarly skip recomputing when only expiration changes.
10. **Cache frequently read combat totals.** Tower Combat Stats recalculates float
    modifiers and sums detection grants when read; EnemyAgent scans aura sources
    to get strongest camo. Cache results when modifiers/sources change, keeping
    Inspector/runtime changes and removal behavior correct.
11. **Share scene references at spawn.** Enemy Agent, Enemy Health, radius visuals
    and acid puddles independently search for grid/pathfinder/economy managers at
    creation. Supply references from the spawner or a scene-scoped service cache.
    This primarily targets spawning spikes rather than ongoing per-frame cost.
12. **Reduce empty-range polling.** A ready Tower Attack Controller with no target
    queries again every frame. A spatial index is preferable first; a configurable
    retry interval is another option but introduces target-acquisition latency and
    therefore requires an explicit gameplay tradeoff.

## Additional scaling concern

Several physics overlap queries use fixed arrays of 64 colliders. That is already
allocation-conscious, but crowded waves/multiple colliders can fill the buffer and
omit valid targets. Grow reusable buffers when full, or use a spatial index with
equivalent exact overlap checks. This is a correctness/scaling issue, not a reason
to silently lower aura tick rates or target counts for performance.

## Suggested validation

Use the same crowded wave, towers, camera and graphics settings for comparisons.
Record main-thread time, GC allocations, physics-query cost, spawn spikes and GPU
time (transparent overlapping aura visuals may be GPU-bound). Check camo overlap
and removal, damage-reduction expiration, healer targeting, visual spin/visibility,
pause, death, and enemy disable/re-enable after the aura pass.
