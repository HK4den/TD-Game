using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Movement Smoothing")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;

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

    private float coyoteTimer;
    private float jumpBufferTimer;

    private MovementMode movementMode = MovementMode.Normal;

    // Temporary speed override (boost pads, etc.)
    private Coroutine speedOverrideRoutine;
    private bool hasSpeedOverride;
    private float overriddenMoveSpeed;

    // Forced movement support (for future launch crystals)
    private Vector3 forcedWorldVelocity;

    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => velocity.y;
    public Vector3 HorizontalVelocity => horizontalVelocity;
    public bool IsMovementLocked => movementMode == MovementMode.ForcedMovement;
    public bool HasSpeedOverride => hasSpeedOverride;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        controls = new PlayerControls();
    }

    private void OnEnable()
    {
        controls.Enable();
        controls.Player.Sprint.performed += OnSprintPerformed;
        controls.Player.Sprint.canceled += OnSprintCanceled;
        controls.Player.Jump.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        controls.Player.Sprint.performed -= OnSprintPerformed;
        controls.Player.Sprint.canceled -= OnSprintCanceled;
        controls.Player.Jump.performed -= OnJumpPerformed;
        controls.Disable();
    }

    private void Update()
    {
        UpdateGrounded();
        UpdateTimersAndJump();

        if (movementMode == MovementMode.ForcedMovement)
        {
            HandleForcedMovement();
        }
        else
        {
            HandleMovement();
        }

        ApplyGravityAndMove();
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

        float smoothRate = inputDir.sqrMagnitude > 0.01f ? acceleration : deceleration;

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
    }

    public void StartSpeedOverride(float speed, float duration)
    {
        if (speedOverrideRoutine != null)
            StopCoroutine(speedOverrideRoutine);

        speedOverrideRoutine = StartCoroutine(SpeedOverrideRoutine(speed, duration));
    }

    private IEnumerator SpeedOverrideRoutine(float speed, float duration)
    {
        hasSpeedOverride = true;
        overriddenMoveSpeed = speed;

        yield return new WaitForSeconds(duration);

        hasSpeedOverride = false;
        overriddenMoveSpeed = 0f;
        speedOverrideRoutine = null;
    }

    public void StartForcedMovement(Vector3 worldVelocity)
    {
        movementMode = MovementMode.ForcedMovement;
        forcedWorldVelocity = worldVelocity;

        // Optional: clear normal horizontal movement so it doesn't resume weirdly after
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
        if (movementMode != MovementMode.Normal)
            return;

        jumpBufferTimer = jumpBuffer;
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        if (hasSpeedOverride || movementMode != MovementMode.Normal)
            return;

        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
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