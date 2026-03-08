using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [Header("Settings")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    private PlayerControls controls;
    private float pitch;

    [Header("Bobbing/Sprint FOV")]
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float walkBobAmount = 0.03f;

    [SerializeField] private float sprintBobSpeed = 12f;
    [SerializeField] private float sprintBobAmount = 0.06f;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float baseFOV = 75f;
    [SerializeField] private float sprintFOV = 82f;
    [SerializeField] private float fovSmoothSpeed = 10f;

    [Header("Strafe Tilt")]
    [SerializeField] private float maxTiltAngle = 4f;
    [SerializeField] private float tiltLerpSpeed = 8f;

    private float bobTimer;
    private Vector3 pivotStartLocalPos;
    private float currentTilt;

    private Coroutine fovOverrideRoutine;
    private bool hasFOVOverride;
    private float overriddenFOV;

    private void Awake()
    {
        controls = new PlayerControls();
        pivotStartLocalPos = cameraPivot.localPosition;

        if (playerCamera != null)
            playerCamera.fieldOfView = baseFOV;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        if (PauseState.IsPaused) return;

        Vector2 look = controls.Player.Look.ReadValue<Vector2>();

        transform.Rotate(Vector3.up * look.x * mouseSensitivity);

        pitch -= look.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        HandleCameraBob();
        HandleSprintFOV();
        HandleStrafeTilt();
        ApplyCameraRotation();
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleCameraBob()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        bool isMoving = move.sqrMagnitude > 0.01f;

        if (!isMoving)
        {
            bobTimer = 0f;
            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition,
                pivotStartLocalPos,
                Time.deltaTime * 10f
            );
            return;
        }

        bool sprinting = controls.Player.Sprint.IsPressed();

        float speed = sprinting ? sprintBobSpeed : walkBobSpeed;
        float amount = sprinting ? sprintBobAmount : walkBobAmount;

        bobTimer += Time.deltaTime * speed;

        float bobOffset = Mathf.Sin(bobTimer) * amount;
        cameraPivot.localPosition = pivotStartLocalPos + Vector3.up * bobOffset;
    }

    private void HandleSprintFOV()
    {
        if (playerCamera == null) return;

        float target;

        if (hasFOVOverride)
        {
            target = overriddenFOV;
        }
        else
        {
            target = controls.Player.Sprint.IsPressed() ? sprintFOV : baseFOV;
        }

        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            target,
            1f - Mathf.Exp(-fovSmoothSpeed * Time.deltaTime)
        );
    }

    private void HandleStrafeTilt()
    {
        Vector2 move = controls.Player.Move.ReadValue<Vector2>();

        float targetTilt = -move.x * maxTiltAngle;

        currentTilt = Mathf.Lerp(
            currentTilt,
            targetTilt,
            1f - Mathf.Exp(-tiltLerpSpeed * Time.deltaTime)
        );
    }

    private void ApplyCameraRotation()
    {
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, currentTilt);
    }

    public void StartFOVOverride(float fov, float duration)
    {
        if (fovOverrideRoutine != null)
            StopCoroutine(fovOverrideRoutine);

        fovOverrideRoutine = StartCoroutine(FOVOverrideRoutine(fov, duration));
    }

    private IEnumerator FOVOverrideRoutine(float fov, float duration)
    {
        hasFOVOverride = true;
        overriddenFOV = fov;

        yield return new WaitForSeconds(duration);

        hasFOVOverride = false;
        fovOverrideRoutine = null;
    }
}