using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public class MagicZipline : MonoBehaviour
{
    [Header("Route (Player Root Positions)")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [Min(0.1f)] public float travelSpeed = 10f;

    [Header("Grab / Jump Off")]
    [Min(0f)] public float grabTransitionTime = 0.1f;
    [Min(0f)] public float minimumRideTime = 0.2f;
    [Min(0.05f)] public float carrierGrabRadius = 0.45f;
    [Range(0.05f, 0.9f)] public float regrabReturnTimeFraction = 0.4f;
    [Min(0f)] public float abandonedLifetime = 2f;
    [Tooltip("Keeps the carrier available through a high jump even when its return time exceeds Abandoned Lifetime.")]
    public bool extendLifetimeForHighJumps = true;

    [Header("Release At End")]
    [Min(0f)] public float exitCarrySpeed = 2f;
    public bool boostOnExit;
    [Min(0f)] public float horizontalLaunchSpeed = 12f;
    public float verticalLaunchSpeed = 8f;

    [Header("Jump-Off Launch")]
    [Tooltip("Jump launches in your full look direction and removes the waiting hook, preventing regrab. Returning to the entrance can start a new ride.")]
    public bool launchOnJumpOff;
    [Min(0f)] public float jumpOffLaunchForce = 12f;

    [Header("Visuals")]
    [SerializeField] private Material beamMaterial;
    [SerializeField] private Color beamColor = new Color(0.65f, 0.15f, 1f, 0.8f);
    [Min(0.01f)] [SerializeField] private float beamWidth = 0.07f;
    [SerializeField] private Vector3 visualOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private GameObject startVisualPrefab;
    [SerializeField] private GameObject endVisualPrefab;
    [SerializeField] private GameObject carrierVisualPrefab;
    [Tooltip("Extra world-space height for the carrier visual only; does not move the player or grab area.")]
    [SerializeField] private float carrierHeightOffset = 0.2f;
    [Tooltip("Local rotation relative to the route. X = 90 lays the default carrier ring flat while retaining the route's slope.")]
    [SerializeField] private Vector3 carrierRotationOffset = new Vector3(90f, 0f, 0f);

    private float hookStretchMultiplier => ZiplineSpringSettings.Shared.hookStretchMultiplier;
    private float stretchStrength => ZiplineSpringSettings.Shared.stretchStrength;
    private float maximumStretch => ZiplineSpringSettings.Shared.maximumStretch;
    private float springStiffness => ZiplineSpringSettings.Shared.springStiffness;
    private float springDamping => ZiplineSpringSettings.Shared.springDamping;

    [Header("Events")]
    public UnityEvent onGrab = new UnityEvent();
    public UnityEvent onJumpOff = new UnityEvent();
    public UnityEvent onArrive = new UnityEvent();
    public UnityEvent onBlocked = new UnityEvent();
    public UnityEvent onReset = new UnityEvent();

    private PlayerMovement rider;
    private PlayerMovement departedPlayer;
    private Vector3 carrierPosition;
    private Vector3 grabOffset;
    private Vector3 hookStretch;
    private Vector3 hookSpringVelocity;
    private float grabbedAt;
    private float regrabAt;
    private float expiresAt;
    private bool waiting;
    private bool leftCarrier;
    private float activeGrabRadius;
    private LineRenderer beam;
    private Transform startVisual;
    private Transform endVisual;
    private Transform carrierVisual;
    private Material generatedMaterial;

    private Vector3 StartPosition => startPoint != null ? startPoint.position : transform.position;

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        carrierPosition = StartPosition;
        if (endPoint == null)
            Debug.LogWarning($"[{name}] Magic Zipline needs an End Point.", this);
        CreateVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        if (player == null || PauseState.IsPaused || rider != null || endPoint == null)
            return;
        if (player == departedPlayer && Time.time < regrabAt)
            return;
        TryGrab(player, true);
    }

    private void LateUpdate()
    {
        if (PauseState.IsPaused)
            return;

        if (endPoint == null)
        {
            ResetRide();
            UpdateVisuals();
            return;
        }

        if (rider != null)
        {
            if (!rider.isActiveAndEnabled || !rider.IsGuidedRideActive)
                ResetRide();
            else
                AdvanceRide();
        }
        else if (waiting)
        {
            if (Time.time >= expiresAt || departedPlayer == null)
                ResetRide();
            else
            {
                bool inside = Vector3.Distance(departedPlayer.transform.position, carrierPosition) <= activeGrabRadius;
                if (!inside)
                    leftCarrier = true;
                if (inside && leftCarrier && Time.time >= regrabAt)
                    TryGrab(departedPlayer, false);
            }
        }

        UpdateVisuals();
    }

    private void TryGrab(PlayerMovement player, bool restart)
    {
        Vector3 entryVelocity = player.HorizontalVelocity + Vector3.up * player.VerticalVelocity;
        if (player.IsGrounded)
            entryVelocity.y = 0f;
        if (!player.BeginGuidedRide(this, TryJumpOff))
            return;

        hookStretch = Vector3.zero;
        hookSpringVelocity = entryVelocity * Mathf.Max(0f, stretchStrength);
        rider = player;
        if (restart)
            carrierPosition = StartPosition;
        grabOffset = rider.transform.position - carrierPosition;
        grabbedAt = Time.time;
        waiting = false;
        leftCarrier = false;
        onGrab.Invoke();
    }

    private void AdvanceRide()
    {
        UpdateHookSpring();
        Vector3 direction = (endPoint.position - carrierPosition).normalized;
        carrierPosition = Vector3.MoveTowards(carrierPosition, endPoint.position, Mathf.Max(0.1f, travelSpeed) * Time.deltaTime);
        float blend = grabTransitionTime <= 0f ? 1f : Mathf.Clamp01((Time.time - grabbedAt) / grabTransitionTime);
        float endBlendDistance = Mathf.Max(0.1f, maximumStretch * Mathf.Max(0f, hookStretchMultiplier));
        float springBlend = Mathf.Clamp01(Vector3.Distance(carrierPosition, endPoint.position) / endBlendDistance);
        Vector3 target = carrierPosition + grabOffset * (1f - Mathf.SmoothStep(0f, 1f, blend))
            + hookStretch * Mathf.Max(0f, hookStretchMultiplier) * Mathf.SmoothStep(0f, 1f, springBlend);
        if (!rider.MoveGuidedRide(this, target))
        {
            Release(Vector3.zero);
            onBlocked.Invoke();
            return;
        }

        if (blend >= 1f && Vector3.Distance(carrierPosition, endPoint.position) < 0.001f)
        {
            if (direction.sqrMagnitude < 0.001f)
                direction = (endPoint.position - StartPosition).normalized;
            Vector3 release = direction * Mathf.Max(0f, exitCarrySpeed);
            if (boostOnExit)
            {
                Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
                if (horizontal.sqrMagnitude < 0.001f)
                    horizontal = Vector3.ProjectOnPlane(endPoint.forward, Vector3.up).normalized;
                release = horizontal * Mathf.Max(0f, horizontalLaunchSpeed) + Vector3.up * verticalLaunchSpeed;
            }
            Release(release);
            onArrive.Invoke();
        }
    }

    private void TryJumpOff()
    {
        if (rider == null || PauseState.IsPaused || Time.time - grabbedAt < Mathf.Max(minimumRideTime, grabTransitionTime))
            return;

        if (launchOnJumpOff)
        {
            PlayerLook look = rider.GetComponentInChildren<PlayerLook>();
            Vector3 direction = look != null ? look.ViewDirection : rider.transform.forward;
            departedPlayer = rider;
            regrabAt = Time.time + 0.25f;
            Release(direction.normalized * Mathf.Max(0f, jumpOffLaunchForce), true);
            onJumpOff.Invoke();
            return;
        }

        float returnTime = rider.JumpReturnTime;
        float jumpApexHeight = rider.JumpTakeoffSpeed * returnTime * 0.25f;
        activeGrabRadius = Mathf.Min(Mathf.Max(0.01f, carrierGrabRadius), Mathf.Max(0.01f, jumpApexHeight * 0.5f));
        departedPlayer = rider;
        carrierPosition = rider.transform.position;
        hookStretch = hookSpringVelocity = Vector3.zero;
        regrabAt = Time.time + returnTime * Mathf.Clamp(regrabReturnTimeFraction, 0.05f, 0.9f);
        float lifetime = Mathf.Max(0f, abandonedLifetime);
        if (extendLifetimeForHighJumps)
            lifetime = Mathf.Max(lifetime, returnTime + 0.25f);
        expiresAt = Time.time + lifetime;
        leftCarrier = false;
        waiting = true;
        rider.EndGuidedRide(this, Vector3.zero, true);
        rider = null;
        onJumpOff.Invoke();
    }

    private void Release(Vector3 releaseVelocity, bool playJumpSound = false)
    {
        hookStretch = hookSpringVelocity = Vector3.zero;
        if (rider != null)
            rider.EndGuidedRide(this, releaseVelocity, false, playJumpSound);
        rider = null;
        waiting = false;
        carrierPosition = StartPosition;
    }

    public void ResetRide()
    {
        bool wasActive = rider != null || waiting;
        Release(Vector3.zero);
        if (wasActive)
            onReset.Invoke();
    }

    private void OnDisable()
    {
        ResetRide();
        SetVisualsActive(false);
    }

    private void OnEnable()
    {
        SetVisualsActive(true);
    }

    private void SetVisualsActive(bool active)
    {
        if (beam != null) beam.gameObject.SetActive(active);
        if (startVisual != null) startVisual.gameObject.SetActive(active);
        if (endVisual != null) endVisual.gameObject.SetActive(active);
        if (carrierVisual != null) carrierVisual.gameObject.SetActive(active && (rider != null || waiting));
    }

    private void CreateVisuals()
    {
        if (beamMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                generatedMaterial = new Material(shader);
        }
        beam = CreateLine("Purple Beam", false, beamWidth);
        startVisual = CreateMarker("Entrance Ring", startVisualPrefab, 0.65f, false);
        endVisual = CreateMarker("Exit Diamond", endVisualPrefab, 0.4f, true);
        carrierVisual = CreateMarker("Carrier", carrierVisualPrefab, 0.5f, false);
        UpdateVisuals();
    }

    private LineRenderer CreateLine(string objectName, bool loop, float width)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.sharedMaterial = beamMaterial != null ? beamMaterial : generatedMaterial;
        line.startColor = line.endColor = beamColor;
        line.startWidth = line.endWidth = width;
        line.loop = loop;
        line.useWorldSpace = true;
        line.positionCount = 2;
        return line;
    }

    private Transform CreateMarker(string objectName, GameObject prefab, float radius, bool diamond)
    {
        if (prefab != null)
            return Instantiate(prefab, transform).transform;

        LineRenderer line = CreateLine(objectName, true, beamWidth * 1.5f);
        line.useWorldSpace = false;
        int segments = diamond ? 4 : 32;
        line.positionCount = segments;
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            line.SetPosition(index, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
        return line.transform;
    }

    private void UpdateHookSpring()
    {
        if (maximumStretch <= 0f || stretchStrength <= 0f)
        {
            hookStretch = hookSpringVelocity = Vector3.zero;
            return;
        }

        float remainingTime = Mathf.Min(Time.deltaTime, 0.25f);
        while (remainingTime > 0f)
        {
            float step = Mathf.Min(remainingTime, 1f / 240f);
            Vector3 acceleration = -Mathf.Clamp(springStiffness, 1f, 500f) * hookStretch
                - Mathf.Clamp(springDamping, 0f, 100f) * hookSpringVelocity;
            hookSpringVelocity += acceleration * step;
            hookStretch += hookSpringVelocity * step;
            if (hookStretch.magnitude > maximumStretch)
            {
                hookStretch = hookStretch.normalized * maximumStretch;
                Vector3 outward = hookStretch.normalized;
                float outwardSpeed = Vector3.Dot(hookSpringVelocity, outward);
                if (outwardSpeed > 0f)
                    hookSpringVelocity -= outward * outwardSpeed;
            }
            remainingTime -= step;
        }
    }

    private void UpdateVisuals()
    {
        if (beam == null)
            return;
        Vector3 finish = endPoint != null ? endPoint.position : StartPosition;
        beam.SetPosition(0, StartPosition + visualOffset);
        beam.SetPosition(1, finish + visualOffset);
        Vector3 direction = finish - StartPosition;
        Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
        startVisual.SetPositionAndRotation(StartPosition + visualOffset, rotation);
        endVisual.SetPositionAndRotation(finish + visualOffset, rotation);
        carrierVisual.SetPositionAndRotation(
            (rider != null ? rider.transform.position : carrierPosition) + visualOffset + Vector3.up * carrierHeightOffset,
            rotation * Quaternion.Euler(carrierRotationOffset));
        carrierVisual.gameObject.SetActive(rider != null || waiting);
    }

    private void OnDestroy()
    {
        if (generatedMaterial != null)
            Destroy(generatedMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.65f, 0.15f, 1f);
        if (endPoint != null)
            Gizmos.DrawLine(StartPosition, endPoint.position);
        Gizmos.DrawWireSphere(Application.isPlaying ? carrierPosition : StartPosition, carrierGrabRadius);
    }
}
