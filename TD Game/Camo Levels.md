# Camo and detection levels

## Enemy setup

On Enemy Abilities, enable Always Camo and set Camo Level from 1 to 3. With Always
Camo off, intrinsic camo is 0. Existing camo enemies default to level 1.

Brush adds 1 and thick brush adds 2 to intrinsic camo, capped at 4. The bonus is
calculated from the enemy's current terrain, so leaving removes it automatically.
A level-3 enemy in thick brush has level 4, not 5. A non-camo enemy in thick brush
has level 2. Beam protection remains absolute regardless of detection level.

## Tower setup

On Tower Combat Stats, enable Has Natural Camo Detection and set Natural Camo
Detection Level. With the checkbox off, base detection is 0. Existing checked
towers default to level 1. Detection can exceed 4, though enemy camo currently cannot.

Allow External Camo Detection determines whether bonuses contribute. It defaults
to enabled to preserve existing aura behavior. Disabling it ignores external
bonuses without changing natural detection. Still-active sources become effective
again if re-enabled; sources removed in the meantime stay removed.

On Tower Aura Grant Camo Detection, set Granted Detection Levels (default 1).
Different sources add together: natural 1 + source A granting 1 + source B granting
2 = detection 4. Removing B leaves detection 2, removing A leaves natural 1.
Repeated refreshes from the same source update its amount without stacking copies.
Leaving range removes that source at the next aura refresh (default 0.2 seconds).
Disabling/destroying the aura removes its grants immediately.

Other systems can call AddGrantedCamoSource(uniqueSourceId, levels) and later
RemoveGrantedCamoSource(uniqueSourceId). Use a distinct ID per independent source.
Passing zero levels removes the grant. NaturalCamoDetectionLevel and
AllowExternalCamoDetection are runtime properties with stat-change notifications.

## Combat rules

Detection must be at least the enemy's current camo level. The same comparison
applies to targeting, projectiles, and puddles. Insufficient detection does not
consume projectile pierce or advance a protected enemy's puddle cooldown.

Projectiles snapshot detection when fired; puddles inherit the projectile's level.
Removing an aura changes the tower's targeting and future shots, not attacks already
in flight or existing puddles. Enemy terrain camo is checked again at impact/tick.
Legacy boolean damage/query APIs interpret true as detection 1, not unlimited.

## Play-mode checks

Verify enemies with intrinsic levels 0, 1, 2, 3 entering/exiting brush and thick
brush. Verify equal detection succeeds and one level below fails; all detection
levels must still fail on beam-protected enemies. Test overlapping auras, changing
grant amounts, removing each source separately, the external-bonus toggle, and
shots/puddles encountering enemies as terrain protection changes.
