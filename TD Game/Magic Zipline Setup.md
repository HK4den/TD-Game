# Magic Zipline

Script: Assets/Scripts/Environment/Magic Zipline.cs (next to the mushroom scripts).

## Scene setup

1. Create an empty entrance object and add Magic Zipline. Its required Box Collider
   becomes a trigger automatically. Size the box to the entrance area.
2. Create a separate empty exit object and assign it to End Point.
3. Optionally create a child Start Point on the entrance and assign it. Otherwise
   the entrance object's position is used. Both route points represent the PLAYER
   ROOT position, not the player's head. Match their height to the player's root
   when standing on the desired surface. Keep the full character capsule clear
   along the route and at the exit.
4. Enter the entrance trigger in Play mode. No changes to the player scene setup
   or input bindings are needed. The existing Jump action works on both devices.

## Ride settings

- Travel Speed: world units per second, default 10.
- Grab Transition Time: smooth attachment duration, default 0.1 seconds.
- Minimum Ride Time: time after EACH grab before jumping out, default 0.2 seconds.
  Jump-off also waits for the attachment transition to finish. Press Jump after
  this time; an early press is not buffered.
- Carrier Grab Radius: distance from the player's ROOT to the waiting carrier
  that allows a grab, default 0.45. This deliberately does not use the full capsule
  hitbox, which would often overlap the carrier for the entire jump. For low jumps,
  the effective radius automatically shrinks to at most half the predicted jump
  height, allowing the player to leave and return without retuning the radius.
- Regrab Return Time Fraction: default 0.4. Each jump computes 2 * takeoff speed /
  gravity strength using current jump settings. Regrab unlocks after this fraction
  of the expected return time. The player must also leave the grab radius first.
- Abandoned Lifetime: default 2 seconds. Regrabbing cancels this countdown.
- Extend Lifetime For High Jumps: on by default; keeps the carrier for at least
  predicted return time + 0.25 seconds so a high jump does not outlast it. Turn off
  for a strict two-second (or configured) timeout.
- Exit Carry Speed: small release along the route, default 2.
- Boost On Exit: replaces carry with Horizontal Launch Speed and Vertical Launch
  Speed. A purely vertical route uses the exit object's forward direction for
  horizontal launch. Rotate the exit if needed.

Jump-off clears all velocity and uses the player's current normal jump speed. The
existing jump sound plays. Normal air steering resumes immediately. The carrier
waits exactly at the jump-off position and regrabbing resumes toward the exit.
Entering the start again after the regrab cooldown resets the abandoned carrier
and starts a new ride. Another zipline can catch the player after jumping out.

While attached, walking, gravity, and external impulses/launches cannot move the
player off the route. Looking remains available. Blocking walls and ceilings, or
ground encountered when travelling downward, release the player with no momentum.
Gentle horizontal ground contact does not cancel the ride. Disabling the zipline,
losing its end point, or calling ResetRide also releases the player safely. Pause
freezes the ride, regrab timer, and expiry timer.

## Visuals and events

Without prefabs, the script builds a purple beam, an entrance ring, a distinct exit
diamond, and a carrier ring at runtime. The carrier disappears when idle, expired,
blocked, or finished. Selected-object gizmos show the route and grab radius in the
editor. Visual Offset defaults to one unit above the route/root positions.

Assign Beam Material for your project's rendering pipeline and builds; the runtime
fallback uses Sprites/Default, which must be available in the build to render.
Optional Start Visual Prefab, End Visual Prefab, and Carrier Visual Prefab replace
the corresponding marker. Use visual-only prefabs without solid colliders or
physics bodies. They are oriented along the route with local Z forward.

Carrier Rotation Offset defaults to (90, 0, 0), laying the default ring horizontally
while tilting with the route. Carrier Height Offset adds 0.2 world units above the
shared Visual Offset. Both affect only the carrier visual, not movement or grabbing;
adjust the rotation offset if a custom carrier prefab uses different axes.

On Grab, On Jump Off, On Arrive, On Blocked, and On Reset events can drive additional
effects, animation, and sound. They are separate from the player movement logic.

## Verification in Play mode

Check one-way entry, mouse/controller look and Jump, minimum ride time, stationary
jump-off, leaving/re-entering the grab area, low/high jumps, expiry, returning to
the start, transfer to a second ride, exit boost, pause/resume, ground grazing,
downward ground impact, a moving wall, and disabling a ride while attached.
The predicted return time assumes constant gravity and return to takeoff height;
future midair gravity changes or variable jump-cut abilities need additional logic.
