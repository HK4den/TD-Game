using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Jump/Grav")]
    [SerializeField] private float jumpHeight = 1.5f; // meters
    [SerializeField] private float gravity = -20f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.2f; // how far below controller we check
    [SerializeField] private float groundedStick = -2f; // keeps you stuck to ground

    [Header("Forgiveness")]
    [SerializeField] private float coyoteTime = 0.12f; // seconds after leaving ground you can still jump
    [SerializeField] private float jumpBuffer = 0.12f; // seconds before landing jump input is remembered

    private CharacterController controller;
    private PlayerControls controls;

    private Vector3 velocity;
    private bool isSprinting;

    private float coyoteTimer;
    private float jumpBufferTimer;

    private bool isGrounded;

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
        UpdateGrounded();          // uses sphere cast (stable even when standing still)
        UpdateTimersAndJump();     // coyote + buffer and jump execution
        HandleMovement();          // horizontal
        ApplyGravityAndMove();     // vertical
    }

    private void UpdateGrounded()
    {
        // Controller bottom in world space
        Vector3 origin = transform.position + controller.center;
        float radius = controller.radius * 0.95f;

        // Bottom point of capsule
        Vector3 bottom = origin + Vector3.down * (controller.height * 0.5f - controller.radius);

        // SphereCast slightly downward from just above bottom
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

        // Keep controller.isGrounded as a fallback (helps on some setups)
        if (controller.isGrounded)
            isGrounded = true;
    }

    private void UpdateTimersAndJump()
    {
        // Update coyote
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        // Update jump buffer
        jumpBufferTimer -= Time.deltaTime;

        // Stick to ground
        if (isGrounded && velocity.y < 0f)
            velocity.y = groundedStick;

        // Execute jump if buffered and allowed
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(2f * jumpHeight * -gravity);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    private void HandleMovement()
    {
        Vector2 input = controls.Player.Move.ReadValue<Vector2>();
        Vector3 move = (transform.right * input.x) + (transform.forward * input.y);

        float speed = isSprinting ? sprintSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);
    }

    private void ApplyGravityAndMove()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpBufferTimer = jumpBuffer;
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx) => isSprinting = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => isSprinting = false;

    private void OnDrawGizmosSelected()
    {
        // Visualize the ground check in Scene view
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
