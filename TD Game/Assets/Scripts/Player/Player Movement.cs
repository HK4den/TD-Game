using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementMode
    {
        Normal,
        ForcedMovement
    }

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Ground Movement Smoothing")]
    [SerializeField] private float groundAcceleration = 90f;
    [SerializeField] private float groundDeceleration = 100f;

    [Header("Air Movement Smoothing")]
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float airDeceleration = 8f;

    [Header("External Momentum")]
    [Tooltip("Horizontal momentum lost per second while airborne, independent of movement input.")]
    [Min(0f)] [SerializeField] private float externalAirDrag = 8f;
    [Tooltip("Horizontal momentum lost per second when grounded.")]
    [Min(0f)] [SerializeField] private float externalGroundFriction = 35f;
    [Tooltip("Fraction of external horizontal momentum kept on landing. 0 stops it; 1 keeps all of it. Ground friction then slows the remainder.")]
    [Range(0f, 1f)] public float landingMomentumRetention = 0.25f;

    [Header("Jump/Grav")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float groundedStick = -2f;

    [Header("Forgiveness")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBuffer = 0.12f;

    private CharacterController controller;
    private PlayerControls controls;

    private Vector3 velocity;
    private Vector3 horizontalVelocity;
    private Vector3 externalHorizontalVelocity;
    private int impulseVersion;
    private bool landingMomentumApplied;

    private bool isSprinting;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private bool hadMoveInputLastFrame;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private MovementMode movementMode = MovementMode.Normal;

    // Boost override state
    private bool hasSpeedOverride;
    private float overriddenMoveSpeed;
    private float boostRemainingTime;
    private float boostMaxDuration;

    // Forced movement support (future launch crystals)
    private Vector3 forcedWorldVelocity;
    private MonoBehaviour guidedRideOwner;
    private Action guidedJumpRequest;
    private Vector3 guidedMoveDirection;
    private bool guidedMoveBlocked;

    public bool IsGuidedRideActive => guidedRideOwner != null;
    public float JumpTakeoffSpeed => Mathf.Sqrt(2f * Mathf.Max(0f, jumpHeight) * Mathf.Max(0f, -gravity));
    public float JumpReturnTime => gravity < -0.0001f ? 2f * JumpTakeoffSpeed / -gravity : 0f;

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => velocity.y;
    public Vector3 HorizontalVelocity => movementMode == MovementMode.ForcedMovement
        ? Vector3.ProjectOnPlane(forcedWorldVelocity, Vector3.up)
        : GetCombinedHorizontalVelocity();
    public bool IsMovementLocked => movementMode == MovementMode.ForcedMovement;
    public bool HasSpeedOverride => hasSpeedOverride;
    public bool IsSprinting => isSprinting;

    // Boost UI hooks
    public bool IsBoostActive => hasSpeedOverride;
    public float CurrentBoostRemaining => boostRemainingTime;
    public float CurrentBoostMaxDuration => boostMaxDuration;
    public float BoostNormalized =>
        (hasSpeedOverride && boostMaxDuration > 0f)
            ? Mathf.Clamp01(boostRemainingTime / boostMaxDuration)
            : 0f;

    public event Action Jumped;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Sprint.performed += OnSprintPerformed;
        controls.Player.Jump.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Sprint.performed -= OnSprintPerformed;
        controls.Player.Jump.performed -= OnJumpPerformed;
        controls.Disable();
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (IsGuidedRideActive)
        {
            UpdateBoostTimer();
            return;
        }

        UpdateGrounded();
        if (!isGrounded)
            landingMomentumApplied = false;
        if (isGrounded && !wasGroundedLastFrame)
            ApplyLandingMomentum();
        UpdateBoostTimer();
        UpdateTimersAndJump();

        bool hasMoveInputThisFrame = HasMoveInput();
        HandleSprintAutoCancel(hasMoveInputThisFrame);

        if (movementMode == MovementMode.Normal)
            HandleMovement();

        ApplyGravityAndMove();

        hadMoveInputLastFrame = hasMoveInputThisFrame;
        wasGroundedLastFrame = isGrounded;
    }

    private void UpdateGrounded()
    {
        if (velocity.y > 0f || (movementMode == MovementMode.ForcedMovement && forcedWorldVelocity.y > 0f))
        {
            isGrounded = false;
            return;
        }

        Vector3 origin = transform.position + controller.center;
        float radius = controller.radius * 0.95f;

        Vector3 bottom = origin + Vector3.down * (controller.height * 0.5f - controller.radius);
        Vector3 castStart = bottom + Vector3.up * 0.05f;

        isGrounded = Physics.SphereCast(
            castStart,
            radius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (controller.isGrounded)
            isGrounded = true;
    }

    private void UpdateBoostTimer()
    {
        if (!hasSpeedOverride)
            return;

        boostRemainingTime -= Time.deltaTime;

        if (boostRemainingTime > 0f)
            return;

        hasSpeedOverride = false;
        overriddenMoveSpeed = 0f;
        boostRemainingTime = 0f;
        boostMaxDuration = 0f;

        // When boost ends:
        // - grounded + no input = no sprint
        // - otherwise sprint on
        if (movementMode == MovementMode.Normal)
        {
            if (isGrounded && !HasMoveInput())
                isSprinting = false;
            else
                isSprinting = true;
        }
    }

    public void SustainSpeedOverride(float speed, float duration)
    {
        if (duration <= 0f || IsGuidedRideActive)
            return;

        hasSpeedOverride = true;
        overriddenMoveSpeed = speed;
        boostRemainingTime = duration;
        boostMaxDuration = duration;
    }
    private void UpdateTimersAndJump()
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        jumpBufferTimer -= Time.deltaTime;

        if (isGrounded && velocity.y < 0f)
            velocity.y = groundedStick;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f && movementMode == MovementMode.Normal)
        {
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            isGrounded = false;

            Jumped?.Invoke();
        }

        if (isGrounded && horizontalVelocity.magnitude < 0.01f && movementMode == MovementMode.Normal)
            horizontalVelocity = Vector3.zero;
    }

    private void HandleMovement()
    {
        Vector2 input = controls.Player.Move.ReadValue<Vector2>();

        Vector3 inputDir = (transform.right * input.x) + (transform.forward * input.y);
        inputDir = Vector3.ClampMagnitude(inputDir, 1f);

        float baseTargetSpeed = GetCurrentTargetSpeed();
        Vector3 targetVelocity = inputDir * baseTargetSpeed;

        float accelerationRate;
        float decelerationRate;

        if (isGrounded)
        {
            accelerationRate = groundAcceleration;
            decelerationRate = groundDeceleration;
        }
        else
        {
            accelerationRate = airAcceleration;
            decelerationRate = airDeceleration;
        }

        float smoothRate = inputDir.sqrMagnitude > 0.01f ? accelerationRate : decelerationRate;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            smoothRate * Time.deltaTime
        );

    }

    private Vector3 GetCombinedHorizontalVelocity()
    {
        float externalSpeed = externalHorizontalVelocity.magnitude;
        if (externalSpeed <= 0f)
            return horizontalVelocity;

        Vector3 externalDirection = externalHorizontalVelocity / externalSpeed;
        float forwardSpeed = Vector3.Dot(horizontalVelocity, externalDirection);
        Vector3 steeringVelocity = horizontalVelocity;
        if (forwardSpeed > 0f)
            steeringVelocity -= externalDirection * Mathf.Min(forwardSpeed, externalSpeed);

        float speedLimit = Mathf.Max(externalSpeed, horizontalVelocity.magnitude);
        return Vector3.ClampMagnitude(externalHorizontalVelocity + steeringVelocity, speedLimit);
    }

    private void ApplyGravityAndMove()
    {
        float drag = isGrounded ? externalGroundFriction : externalAirDrag;
        externalHorizontalVelocity = Vector3.MoveTowards(
            externalHorizontalVelocity, Vector3.zero, Mathf.Max(0f, drag) * Time.deltaTime);

        Vector3 movementVelocity = movementMode == MovementMode.ForcedMovement
            ? forcedWorldVelocity
            : GetCombinedHorizontalVelocity();
        controller.Move(movementVelocity * Time.deltaTime);

        if (IsGuidedRideActive)
            return;

        velocity.y += gravity * Time.deltaTime;
        int versionBeforeMove = impulseVersion;
        CollisionFlags collisions = controller.Move(Vector3.up * velocity.y * Time.deltaTime);

        if (versionBeforeMove == impulseVersion)
        {
            if ((collisions & CollisionFlags.Above) != 0 && velocity.y > 0f)
                velocity.y = 0f;
            if ((collisions & CollisionFlags.Below) != 0 && velocity.y < 0f)
            {
                if (!isGrounded)
                    ApplyLandingMomentum();
                velocity.y = groundedStick;
            }
        }
    }

    private void ApplyLandingMomentum()
    {
        if (landingMomentumApplied)
            return;

        externalHorizontalVelocity *= Mathf.Clamp01(landingMomentumRetention);
        landingMomentumApplied = true;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (IsGuidedRideActive)
        {
            bool drivingIntoGround = hit.normal.y > 0.5f && guidedMoveDirection.y < -0.01f;
            bool blockingSurface = hit.normal.y <= 0.5f && Vector3.Dot(guidedMoveDirection, hit.normal) < -0.1f;
            if (drivingIntoGround || blockingSurface)
                guidedMoveBlocked = true;
            return;
        }

        if (hit.normal.y >= Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad) || hit.normal.y < -0.01f)
            return;

        Vector3 wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
        RemoveBlockedVelocity(ref externalHorizontalVelocity, wallNormal);
    }

    private static void RemoveBlockedVelocity(ref Vector3 movementVelocity, Vector3 normal)
    {
        float intoSurface = Vector3.Dot(movementVelocity, normal);
        if (intoSurface < 0f)
            movementVelocity -= normal * intoSurface;
    }

    private float GetCurrentTargetSpeed()
    {
        if (hasSpeedOverride)
            return overriddenMoveSpeed;

        return isSprinting ? sprintSpeed : walkSpeed;
    }

    private bool HasMoveInput()
    {
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>();
        return moveInput.sqrMagnitude > 0.0001f;
    }

    private void HandleSprintAutoCancel(bool hasMoveInputThisFrame)
    {
        if (hasSpeedOverride)
            return;

        if (movementMode != MovementMode.Normal)
            return;

        bool justLanded = !wasGroundedLastFrame && isGrounded;

        if (isGrounded)
        {
            bool stoppedMovingThisFrame = hadMoveInputLastFrame && !hasMoveInputThisFrame;
            bool landedWithoutInput = justLanded && !hasMoveInputThisFrame;

            if (stoppedMovingThisFrame || landedWithoutInput)
                isSprinting = false;
        }
    }

    public void ApplyLaunch(Vector3 launchVelocity, bool replaceHorizontal = true, bool replaceVertical = true)
    {
        if (PauseState.IsPaused || IsGuidedRideActive)
            return;

        if (replaceHorizontal)
        {
            horizontalVelocity = Vector3.zero;
            externalHorizontalVelocity = Vector3.ProjectOnPlane(launchVelocity, Vector3.up);
        }
        else
        {
            externalHorizontalVelocity += Vector3.ProjectOnPlane(launchVelocity, Vector3.up);
        }

        if (replaceVertical)
            velocity.y = launchVelocity.y;
        else
            velocity.y += launchVelocity.y;

        movementMode = MovementMode.Normal;
        forcedWorldVelocity = Vector3.zero;
        isSprinting = true;
        RegisterImpulse();
    }

    public void AddImpulse(Vector3 velocityChange)
    {
        if (PauseState.IsPaused || movementMode != MovementMode.Normal)
            return;

        externalHorizontalVelocity += Vector3.ProjectOnPlane(velocityChange, Vector3.up);
        if (isGrounded && velocityChange.y > 0f && velocity.y < 0f)
            velocity.y = 0f;
        velocity.y += velocityChange.y;
        RegisterImpulse();
    }

    public void AddForce(Vector3 acceleration, float duration)
    {
        if (duration <= 0f)
            return;

        AddImpulse(acceleration * duration);
    }

    private void RegisterImpulse()
    {
        impulseVersion++;
        if (velocity.y <= 0f)
            return;

        isGrounded = false;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
    }

    // Returns true only if the boost was actually applied/replaced.
    public bool TryStartSpeedOverride(float speed, float duration)
    {
        if (duration <= 0f || IsGuidedRideActive)
            return false;

        // Only replace if this cloud gives MORE remaining boost time
        if (hasSpeedOverride && duration <= boostRemainingTime)
            return false;

        hasSpeedOverride = true;
        overriddenMoveSpeed = speed;
        boostRemainingTime = duration;
        boostMaxDuration = duration;

        return true;
    }

    // Compatibility wrapper so older scripts still compile
    public void StartSpeedOverride(float speed, float duration)
    {
        TryStartSpeedOverride(speed, duration);
    }

    public void StartForcedMovement(Vector3 worldVelocity)
    {
        if (IsGuidedRideActive)
            return;
        movementMode = MovementMode.ForcedMovement;
        forcedWorldVelocity = worldVelocity;
        horizontalVelocity = Vector3.zero;
        externalHorizontalVelocity = Vector3.zero;
    }

    public void UpdateForcedMovementVelocity(Vector3 worldVelocity)
    {
        forcedWorldVelocity = worldVelocity;
    }

    public void StopForcedMovement()
    {
        if (IsGuidedRideActive)
            return;
        movementMode = MovementMode.Normal;
        forcedWorldVelocity = Vector3.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused)
            return;

        if (IsGuidedRideActive)
        {
            guidedJumpRequest?.Invoke();
            return;
        }

        if (movementMode != MovementMode.Normal)
            return;

        jumpBufferTimer = jumpBuffer;
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused)
            return;

        if (hasSpeedOverride)
            return;

        if (movementMode != MovementMode.Normal)
            return;

        isSprinting = !isSprinting;
    }

    public bool BeginGuidedRide(MonoBehaviour owner, Action jumpRequest)
    {
        if (owner == null || PauseState.IsPaused || IsMovementLocked || !isActiveAndEnabled || !controller.enabled)
            return false;

        guidedRideOwner = owner;
        guidedJumpRequest = jumpRequest;
        movementMode = MovementMode.ForcedMovement;
        velocity = horizontalVelocity = externalHorizontalVelocity = forcedWorldVelocity = Vector3.zero;
        isGrounded = wasGroundedLastFrame = false;
        coyoteTimer = jumpBufferTimer = 0f;
        return true;
    }

    public bool MoveGuidedRide(MonoBehaviour owner, Vector3 target)
    {
        if (guidedRideOwner != owner || PauseState.IsPaused || !controller.enabled)
            return false;

        Vector3 displacement = target - transform.position;
        guidedMoveDirection = displacement.normalized;
        guidedMoveBlocked = false;
        controller.Move(displacement);
        return !guidedMoveBlocked;
    }

    public void EndGuidedRide(MonoBehaviour owner, Vector3 releaseVelocity, bool jump)
    {
        if (guidedRideOwner != owner)
            return;

        guidedRideOwner = null;
        guidedJumpRequest = null;
        movementMode = MovementMode.Normal;
        forcedWorldVelocity = horizontalVelocity = externalHorizontalVelocity = velocity = Vector3.zero;
        coyoteTimer = jumpBufferTimer = 0f;
        isGrounded = wasGroundedLastFrame = false;
        landingMomentumApplied = false;
        if (jump)
        {
            velocity.y = JumpTakeoffSpeed;
            RegisterImpulse();
            Jumped?.Invoke();
        }
        else
        {
            externalHorizontalVelocity = Vector3.ProjectOnPlane(releaseVelocity, Vector3.up);
            velocity.y = releaseVelocity.y;
            RegisterImpulse();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!TryGetComponent(out CharacterController cc)) return;

        Vector3 origin = transform.position + cc.center;
        float radius = cc.radius * 0.95f;
        Vector3 bottom = origin + Vector3.down * (cc.height * 0.5f - cc.radius);
        Vector3 castStart = bottom + Vector3.up * 0.05f;
        Vector3 castEnd = castStart + Vector3.down * groundCheckDistance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(castStart, radius);
        Gizmos.DrawWireSphere(castEnd, radius);
        Gizmos.DrawLine(castStart, castEnd);
    }
}
