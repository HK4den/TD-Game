# Player movement and outside forces

PlayerMovement combines player-controlled horizontal movement and external horizontal
momentum in the first CharacterController move, then applies gravity and vertical
movement in a separate move, preserving the original step-handling order.
Movement input changes only the player-controlled portion. Gravity acts on vertical
velocity from both jumping and outside forces.

## Tuning

On the player's Player Movement component:

- External Air Drag defaults to 8: horizontal external speed lost per second in air.
  Set it to 0 to keep that momentum until a collision or landing.
- External Ground Friction defaults to 35: horizontal external speed lost per second
  on the ground, including leftover landing momentum and grounded pushes.
- Landing Momentum Retention defaults to 0.25: keeps 25% of external horizontal
  momentum when landing. 0 removes it all; 1 keeps it all. This is applied once per
  landing, then ground friction slows the remainder. Player-controlled movement is
  retained, so holding a direction still moves you normally after landing.
- Air Acceleration and Air Deceleration still control the player's own steering.
  Player steering speed is added to external momentum, so holding forward can extend
  a launch and holding backward can counteract it.

Existing mushrooms work automatically through ApplyLaunch. Their configured launch
replaces existing horizontal and vertical movement, then steering builds again.
Repeated mushrooms therefore reset the launch rather than stacking launch speeds.

## Other gameplay scripts

- `AddImpulse(Vector3 velocityChange)` adds an instantaneous world-space velocity
  change in units per second. Use once for knockback or a push.
- `AddForce(Vector3 acceleration, float duration)` adds world-space acceleration
  over the supplied simulation time. Call with Time.deltaTime from Update or
  Time.fixedDeltaTime from FixedUpdate for a sustained force. The duration is the
  elapsed time for this call, not a request to schedule a future effect.
- `ApplyLaunch(velocity, replaceHorizontal, replaceVertical)` sets or adds launch
  velocity per axis group. It exits forced movement mode.

These methods ignore requests while paused. AddImpulse and AddForce also ignore
requests during explicit forced movement. Forces are mass-independent; this is a
CharacterController motor, not a Rigidbody. Other objects must call these methods
to push the player; ordinary Rigidbody contacts do not automatically supply forces.

Walls remove external horizontal velocity directed into them while retaining sideways
momentum. Collision callbacks do not cancel player-controlled walking velocity;
the CharacterController handles walking against obstacles and stepping over edges.
Ceilings clear upward vertical velocity. Upward launches clear grounded
state and pending jumps so a buffered jump cannot overwrite the bounce.

## Play-mode checks

Try an angled mushroom with no input, forward input, sideways input, and backward
input. Check wall glancing, ceiling hits, repeated mushrooms, boosted launches,
landing friction, normal jumps, jump/landing audio, and pausing mid-launch. Tune
drag and friction after judging the feel in the actual level.
