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

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => velocity.y;
    public Vector3 HorizontalVelocity => horizontalVelocity;
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

        UpdateGrounded();
        UpdateBoostTimer();
        UpdateTimersAndJump();

        bool hasMoveInputThisFrame = HasMoveInput();
        HandleSprintAutoCancel(hasMoveInputThisFrame);

        if (movementMode == MovementMode.ForcedMovement)
            HandleForcedMovement();
        else
            HandleMovement();

        ApplyGravityAndMove();

        hadMoveInputLastFrame = hasMoveInputThisFrame;
        wasGroundedLastFrame = isGrounded;
    }

    private void UpdateGrounded()
    {
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
        if (duration <= 0f)
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

        controller.Move(horizontalVelocity * Time.deltaTime);
    }

    private void HandleForcedMovement()
    {
        controller.Move(forcedWorldVelocity * Time.deltaTime);
    }

    private void ApplyGravityAndMove()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(Vector3.up * velocity.y * Time.deltaTime);
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
        if (replaceHorizontal)
        {
            horizontalVelocity.x = launchVelocity.x;
            horizontalVelocity.z = launchVelocity.z;
        }
        else
        {
            horizontalVelocity.x += launchVelocity.x;
            horizontalVelocity.z += launchVelocity.z;
        }

        if (replaceVertical)
            velocity.y = launchVelocity.y;
        else
            velocity.y += launchVelocity.y;

        movementMode = MovementMode.Normal;
        isSprinting = true;
    }

    // Returns true only if the boost was actually applied/replaced.
    public bool TryStartSpeedOverride(float speed, float duration)
    {
        if (duration <= 0f)
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
        movementMode = MovementMode.ForcedMovement;
        forcedWorldVelocity = worldVelocity;
        horizontalVelocity = Vector3.zero;
    }

    public void UpdateForcedMovementVelocity(Vector3 worldVelocity)
    {
        forcedWorldVelocity = worldVelocity;
    }

    public void StopForcedMovement()
    {
        movementMode = MovementMode.Normal;
        forcedWorldVelocity = Vector3.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (PauseState.IsPaused)
            return;

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
