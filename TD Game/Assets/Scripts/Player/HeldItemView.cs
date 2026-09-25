using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Displays and animates the currently equipped inventory item in front of the camera.
/// Assign a model prefab on each ToolDefinition; missing prefabs use a small colored cube.
/// </summary>
[DefaultExecutionOrder(100)]
public class HeldItemView : MonoBehaviour
{
    private const string HeldItemLayerName = "HeldItem";

    [Header("References")]
    [SerializeField] private ToolHotbar hotbar;
    [SerializeField] private PlayerLook playerLook;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Look Response")]
    [SerializeField] private float lookOffsetPerDegreePerSecond = 0.00055f;
    [SerializeField] private float maxLookOffset = 0.11f;
    [SerializeField] private float lookPositionResponse = 23f;
    [SerializeField] private float lookRotationPerDegreePerSecond = 0.025f;
    [SerializeField] private float rotationResponse = 22f;

    [Header("Movement Response")]
    [SerializeField] private float strafeSwayPerSpeed = 0.011f;
    [SerializeField] private float forwardSwayPerSpeed = 0.007f;
    [SerializeField] private float movementResponse = 20f;
    [SerializeField] private float strafeRollPerSpeed = 0.55f;
    [SerializeField] private float walkingBobAmount = 0.012f;
    [SerializeField] private float walkingBobRatePerSpeed = 1.7f;

    [Header("Idle Motion")]
    [SerializeField] private float idleCycleSpeed = 1.5f;
    [SerializeField] private float idleVerticalAmount = 0.006f;
    [SerializeField] private float idleHorizontalAmount = 0.003f;
    [SerializeField] private float idlePitchDegrees = 0.25f;
    [SerializeField] private float idleRollDegrees = 0.35f;
    [SerializeField] private float idleFadeInSpeed = 3f;
    [SerializeField] private float idleFadeOutSpeed = 14f;

    [Header("Guided Ride Motion")]
    [SerializeField] private float rideHorizontalSwayPerSpeed = 0.007f;
    [SerializeField] private float rideVerticalSwayPerSpeed = 0.009f;
    [SerializeField] private float rideAccelerationSway = 0.00045f;
    [SerializeField] private float maxRideSway = 0.12f;
    [SerializeField] private float rideResponse = 18f;
    [SerializeField] private float rideIdleWeight = 0.45f;

    [Header("Air Motion")]
    [SerializeField] private float jumpDip = 0.095f;
    [SerializeField] private float fallLift = 0.075f;
    [SerializeField] private float airborneResponse = 23f;
    [SerializeField] private float groundedResponse = 17f;

    [Header("Transitions")]
    [SerializeField] private float equipTransitionDuration = 0.12f;
    [SerializeField] private float landingKickPerSpeed = 0.004f;
    [SerializeField] private float maxLandingKick = 0.045f;
    [SerializeField] private float landingKickDuration = 0.16f;

    private Transform viewTransform;
    private Transform viewPivot;
    private Camera overlayCamera;
    private UniversalAdditionalCameraData baseCameraData;
    private GameObject heldModel;
    private ToolDefinition currentDefinition;
    private ToolHotbar.ToolKind currentKind;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;
    private Vector3 smoothedLookLag;
    private Vector3 smoothedMovementSway;
    private Vector3 smoothedRideSway;
    private Vector3 previousPlayerPosition;
    private Vector3 previousRideVelocity;
    private Vector3 targetLookRotation;
    private Vector3 localHorizontalVelocity;
    private Vector3 previousViewEuler;
    private float airOffset;
    private float landingKickStrength;
    private float landingKickAge;
    private float airborneTime;
    private float walkingBobPhase;
    private float walkingBobWeight;
    private float idlePhase;
    private float idleWeight;
    private float equipAge;
    private bool hasPreviousViewEuler;
    private bool hasPreviousPlayerPosition;
    private bool hasJumpedSinceLanding;

    private void OnValidate()
    {
        if (hotbar == null)
            hotbar = GetComponent<ToolHotbar>();
        if (playerLook == null)
            playerLook = GetComponent<PlayerLook>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Awake()
    {
        if (hotbar == null)
            hotbar = GetComponent<ToolHotbar>();
        if (hotbar == null)
            hotbar = FindFirstObjectByType<ToolHotbar>();

        if (playerLook == null)
            playerLook = GetComponent<PlayerLook>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerLook != null && playerLook.CameraPivot != null)
        {
            viewPivot = playerLook.CameraPivot;
            Camera worldCamera = playerLook.PlayerCamera;
            if (worldCamera == null)
                worldCamera = playerLook.GetComponentInChildren<Camera>();
            if (worldCamera == null)
                worldCamera = Camera.main;

            GameObject viewRoot = new GameObject("Held Item View");
            viewTransform = viewRoot.transform;
            viewTransform.SetParent(worldCamera != null ? worldCamera.transform : viewPivot, false);
            ConfigureOverlayCamera(worldCamera);
        }
    }

    private void OnEnable()
    {
        if (hotbar != null)
            hotbar.OnHotbarChanged += RefreshHeldItem;

        if (playerMovement != null)
        {
            playerMovement.Jumped += OnPlayerJumped;
            playerMovement.Landed += OnPlayerLanded;
        }
    }

    private void Start()
    {
        RefreshHeldItem();

        if (viewPivot != null)
        {
            previousViewEuler = viewPivot.eulerAngles;
            hasPreviousViewEuler = true;
        }
    }

    private void OnDisable()
    {
        if (hotbar != null)
            hotbar.OnHotbarChanged -= RefreshHeldItem;

        if (playerMovement != null)
        {
            playerMovement.Jumped -= OnPlayerJumped;
            playerMovement.Landed -= OnPlayerLanded;
        }
    }

    private void OnDestroy()
    {
        if (baseCameraData != null && overlayCamera != null)
            baseCameraData.cameraStack.Remove(overlayCamera);

        if (overlayCamera != null)
            Destroy(overlayCamera.gameObject);

        if (heldModel != null)
            Destroy(heldModel);

        if (viewTransform != null)
            Destroy(viewTransform.gameObject);
    }

    private void LateUpdate()
    {
        if (PauseState.IsPaused || viewTransform == null || heldModel == null)
            return;

        float deltaTime = Time.deltaTime;
        UpdateAirborneTime();
        UpdateLookMotion(deltaTime);
        UpdateMovementMotion(deltaTime);
        UpdateGuidedRideMotion(deltaTime);
        UpdateAirMotion(deltaTime);
        UpdateIdleMotion(deltaTime);

        equipAge += deltaTime;
        float equipProgress = equipTransitionDuration <= 0f
            ? 1f
            : Mathf.Clamp01(equipAge / equipTransitionDuration);
        float easedEquipProgress = equipProgress * equipProgress * (3f - 2f * equipProgress);
        Vector3 equipOffset = Vector3.Lerp(new Vector3(0f, -0.14f, -0.09f), Vector3.zero, easedEquipProgress);

        float landingOffset = GetLandingOffset(deltaTime);
        Vector3 motionOffset = smoothedLookLag + smoothedMovementSway + smoothedRideSway;
        motionOffset.y += airOffset + landingOffset + Mathf.Sin(walkingBobPhase) * walkingBobAmount * walkingBobWeight;
        motionOffset.x += Mathf.Cos(walkingBobPhase * 0.5f) * walkingBobAmount * 0.35f * walkingBobWeight;
        float idleBreath = Mathf.Sin(idlePhase) * idleWeight;
        float idleDrift = Mathf.Sin(idlePhase * 0.5f + 0.7f) * idleWeight;
        motionOffset.y += idleBreath * idleVerticalAmount;
        motionOffset.x += idleDrift * idleHorizontalAmount;
        viewTransform.localPosition = baseLocalPosition + motionOffset + equipOffset;

        Vector3 rotation = targetLookRotation;
        rotation.x += idleBreath * idlePitchDegrees;
        rotation.x -= (smoothedRideSway.y + smoothedRideSway.z) * 16f;
        rotation.z -= localHorizontalVelocity.x * strafeRollPerSpeed;
        rotation.z -= smoothedRideSway.x * 18f;
        rotation.z += idleDrift * idleRollDegrees;
        Quaternion targetRotation = baseLocalRotation * Quaternion.Euler(rotation);
        float rotationBlend = ResponseBlend(rotationResponse, deltaTime);
        viewTransform.localRotation = Quaternion.Slerp(viewTransform.localRotation, targetRotation, rotationBlend);
    }

    private void RefreshHeldItem()
    {
        if (viewTransform == null)
            return;

        if (heldModel != null)
        {
            heldModel.SetActive(false);
            Destroy(heldModel);
            heldModel = null;
        }

        ToolHotbar.Slot slot = hotbar != null ? hotbar.CurrentSlot : default;
        currentKind = slot.kind;
        currentDefinition = slot.definition;

        bool hasHeldItem = currentKind != ToolHotbar.ToolKind.Empty;
        viewTransform.gameObject.SetActive(hasHeldItem);
        if (!hasHeldItem)
            return;

        baseLocalPosition = currentDefinition != null
            ? currentDefinition.heldItemLocalPosition
            : new Vector3(0.28f, -0.22f, 0.58f);
        baseLocalRotation = currentDefinition != null
            ? Quaternion.Euler(currentDefinition.heldItemLocalEulerAngles)
            : Quaternion.Euler(0f, 12f, 0f);
        baseLocalScale = currentDefinition != null
            ? currentDefinition.heldItemLocalScale
            : Vector3.one;

        if (currentDefinition != null && currentDefinition.heldItemPrefab != null)
        {
            heldModel = Instantiate(currentDefinition.heldItemPrefab, viewTransform, false);
            heldModel.transform.localPosition = Vector3.zero;
            heldModel.transform.localRotation = Quaternion.identity;

            if (heldModel.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                Debug.LogWarning($"Held model for {currentDefinition.displayName} has no renderer; showing a placeholder.", currentDefinition);
                heldModel.SetActive(false);
                Destroy(heldModel);
                heldModel = null;
            }
        }

        if (heldModel == null)
        {
            heldModel = CreatePlaceholderModel(currentKind, currentDefinition);
            heldModel.transform.SetParent(viewTransform, false);
            heldModel.transform.localPosition = Vector3.zero;
            heldModel.transform.localRotation = Quaternion.identity;
            heldModel.transform.localScale = new Vector3(0.12f, 0.12f, 0.38f);
        }

        SetLayerRecursively(heldModel, LayerMask.NameToLayer(HeldItemLayerName));
        PrepareVisualModel(heldModel);
        heldModel.SetActive(true);

        viewTransform.localPosition = baseLocalPosition + new Vector3(0f, -0.14f, -0.09f);
        viewTransform.localRotation = baseLocalRotation;
        viewTransform.localScale = baseLocalScale;

        smoothedLookLag = Vector3.zero;
        smoothedMovementSway = Vector3.zero;
        smoothedRideSway = Vector3.zero;
        previousRideVelocity = Vector3.zero;
        previousPlayerPosition = transform.position;
        hasPreviousPlayerPosition = true;
        targetLookRotation = Vector3.zero;
        localHorizontalVelocity = Vector3.zero;
        airOffset = 0f;
        landingKickStrength = 0f;
        landingKickAge = landingKickDuration;
        walkingBobPhase = 0f;
        walkingBobWeight = 0f;
        idlePhase = 0f;
        idleWeight = 0f;
        equipAge = 0f;

        if (viewPivot != null)
        {
            previousViewEuler = viewPivot.eulerAngles;
            hasPreviousViewEuler = true;
        }
    }

    private void ConfigureOverlayCamera(Camera worldCamera)
    {
        int heldItemLayer = LayerMask.NameToLayer(HeldItemLayerName);
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (heldItemLayer < 0 || worldCamera == null)
        {
            Debug.LogWarning("Held item overlay needs the HeldItem layer and a player camera.", this);
            return;
        }

        worldCamera.cullingMask &= ~(1 << heldItemLayer);
        baseCameraData = worldCamera.GetComponent<UniversalAdditionalCameraData>();
        if (baseCameraData == null)
            baseCameraData = worldCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        baseCameraData.renderType = CameraRenderType.Base;

        GameObject overlayObject = new GameObject("Held Item Overlay Camera");
        overlayObject.transform.SetParent(worldCamera.transform, false);
        overlayCamera = overlayObject.AddComponent<Camera>();
        overlayCamera.CopyFrom(worldCamera);
        overlayCamera.transform.localPosition = Vector3.zero;
        overlayCamera.transform.localRotation = Quaternion.identity;
        overlayCamera.cullingMask = 1 << heldItemLayer;
        overlayCamera.clearFlags = CameraClearFlags.Depth;
        overlayCamera.depth = worldCamera.depth + 1f;
        overlayCamera.tag = "Untagged";

        UniversalAdditionalCameraData overlayData = overlayObject.AddComponent<UniversalAdditionalCameraData>();
        overlayData.renderType = CameraRenderType.Overlay;
        overlayData.renderShadows = false;
        overlayData.renderPostProcessing = false;
        overlayData.requiresDepthTexture = false;
        overlayData.requiresColorTexture = false;

        if (!baseCameraData.cameraStack.Contains(overlayCamera))
            baseCameraData.cameraStack.Add(overlayCamera);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private static void PrepareVisualModel(GameObject model)
    {
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
        {
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private GameObject CreatePlaceholderModel(ToolHotbar.ToolKind kind, ToolDefinition definition)
    {
        GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        placeholder.name = (definition != null && !string.IsNullOrWhiteSpace(definition.displayName))
            ? definition.displayName + " Placeholder"
            : kind + " Placeholder";

        Collider itemCollider = placeholder.GetComponent<Collider>();
        if (itemCollider != null)
        {
            itemCollider.enabled = false;
            Destroy(itemCollider);
        }

        Renderer itemRenderer = placeholder.GetComponent<Renderer>();
        if (itemRenderer != null)
        {
            Color tint = definition != null ? definition.heldItemTint : GetDefaultTint(kind);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            itemRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", tint);
            block.SetColor("_Color", tint);
            itemRenderer.SetPropertyBlock(block);
        }

        return placeholder;
    }

    private static Color GetDefaultTint(ToolHotbar.ToolKind kind)
    {
        switch (kind)
        {
            case ToolHotbar.ToolKind.Placement: return new Color(1f, 0.58f, 0.14f);
            case ToolHotbar.ToolKind.Inspect: return new Color(0.16f, 0.78f, 1f);
            case ToolHotbar.ToolKind.Mining: return new Color(0.48f, 0.88f, 0.26f);
            case ToolHotbar.ToolKind.Targeting: return new Color(0.84f, 0.3f, 1f);
            default: return Color.white;
        }
    }

    private void UpdateLookMotion(float deltaTime)
    {
        if (viewPivot == null)
            return;

        Vector3 currentEuler = viewPivot.eulerAngles;
        if (!hasPreviousViewEuler)
        {
            previousViewEuler = currentEuler;
            hasPreviousViewEuler = true;
            return;
        }

        float pitchRate = Mathf.Clamp(Mathf.DeltaAngle(previousViewEuler.x, currentEuler.x) / Mathf.Max(deltaTime, 0.0001f), -600f, 600f);
        float yawRate = Mathf.Clamp(Mathf.DeltaAngle(previousViewEuler.y, currentEuler.y) / Mathf.Max(deltaTime, 0.0001f), -600f, 600f);
        previousViewEuler = currentEuler;

        Vector3 targetLag = new Vector3(
            -yawRate * lookOffsetPerDegreePerSecond,
            pitchRate * lookOffsetPerDegreePerSecond,
            0f);
        targetLag = Vector3.ClampMagnitude(targetLag, maxLookOffset);
        smoothedLookLag = Vector3.Lerp(smoothedLookLag, targetLag, ResponseBlend(lookPositionResponse, deltaTime));

        targetLookRotation = new Vector3(
            pitchRate * lookRotationPerDegreePerSecond,
            -yawRate * lookRotationPerDegreePerSecond * 0.5f,
            -yawRate * lookRotationPerDegreePerSecond);
    }

    private void UpdateAirborneTime()
    {
        if (playerMovement == null || playerMovement.IsGrounded || playerMovement.IsGuidedRideActive)
        {
            airborneTime = 0f;
            return;
        }

        airborneTime += Time.deltaTime;
    }

    private void UpdateMovementMotion(float deltaTime)
    {
        Vector3 worldVelocity = playerMovement != null && !playerMovement.IsGuidedRideActive
            ? playerMovement.HorizontalVelocity
            : Vector3.zero;
        localHorizontalVelocity = transform.InverseTransformDirection(worldVelocity);

        Vector3 targetSway = new Vector3(
            -localHorizontalVelocity.x * strafeSwayPerSpeed,
            0f,
            -localHorizontalVelocity.z * forwardSwayPerSpeed);
        smoothedMovementSway = Vector3.Lerp(smoothedMovementSway, targetSway, ResponseBlend(movementResponse, deltaTime));

        bool walking = playerMovement != null && playerMovement.IsGrounded && !playerMovement.IsGuidedRideActive;
        float speed = worldVelocity.magnitude;
        float targetBobWeight = walking ? Mathf.Clamp01(speed / 5f) : 0f;
        walkingBobWeight = Mathf.Lerp(walkingBobWeight, targetBobWeight, ResponseBlend(movementResponse, deltaTime));
        if (walking && speed > 0.15f)
            walkingBobPhase = Mathf.Repeat(walkingBobPhase + deltaTime * speed * walkingBobRatePerSpeed, Mathf.PI * 4f);
    }

    private void UpdateAirMotion(float deltaTime)
    {
        bool airborne = playerMovement != null && !playerMovement.IsGrounded && !playerMovement.IsGuidedRideActive;
        float targetOffset = 0f;
        if (airborne)
        {
            float takeoffSpeed = Mathf.Max(1f, playerMovement.JumpTakeoffSpeed);
            float normalizedSpeed = Mathf.Clamp(playerMovement.VerticalVelocity / takeoffSpeed, -1f, 1f);
            targetOffset = normalizedSpeed >= 0f
                ? -normalizedSpeed * jumpDip
                : -normalizedSpeed * fallLift;
        }

        float response = airborne ? airborneResponse : groundedResponse;
        airOffset = Mathf.Lerp(airOffset, targetOffset, ResponseBlend(response, deltaTime));
    }

    private void UpdateGuidedRideMotion(float deltaTime)
    {
        Vector3 currentPosition = transform.position;
        bool riding = playerMovement != null && playerMovement.IsGuidedRideActive;
        Vector3 targetSway = Vector3.zero;

        // The zipline moves the rider in LateUpdate, before this view updates.
        if (riding && hasPreviousPlayerPosition && deltaTime > 0.0001f)
        {
            Vector3 worldVelocity = Vector3.ClampMagnitude(
                (currentPosition - previousPlayerPosition) / deltaTime, 30f);
            Vector3 worldAcceleration = Vector3.ClampMagnitude(
                (worldVelocity - previousRideVelocity) / deltaTime, 200f);
            Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
            Vector3 localAcceleration = transform.InverseTransformDirection(worldAcceleration);

            targetSway = new Vector3(
                -localVelocity.x * rideHorizontalSwayPerSpeed,
                -localVelocity.y * rideVerticalSwayPerSpeed,
                -localVelocity.z * rideHorizontalSwayPerSpeed);
            targetSway -= localAcceleration * rideAccelerationSway;
            targetSway = Vector3.ClampMagnitude(targetSway, maxRideSway);
            previousRideVelocity = worldVelocity;
        }
        else
        {
            previousRideVelocity = Vector3.zero;
        }

        smoothedRideSway = Vector3.Lerp(smoothedRideSway, targetSway, ResponseBlend(rideResponse, deltaTime));
        previousPlayerPosition = currentPosition;
        hasPreviousPlayerPosition = true;
    }

    private void UpdateIdleMotion(float deltaTime)
    {
        bool riding = playerMovement != null && playerMovement.IsGuidedRideActive;
        bool standingStill = playerMovement == null ||
            (playerMovement.IsGrounded && playerMovement.HorizontalVelocity.sqrMagnitude < 0.04f);
        float targetWeight = riding ? Mathf.Clamp01(rideIdleWeight) : standingStill ? 1f : 0f;
        float response = riding ? rideResponse : targetWeight > idleWeight ? idleFadeInSpeed : idleFadeOutSpeed;
        idleWeight = Mathf.Lerp(idleWeight, targetWeight, ResponseBlend(response, deltaTime));
        idlePhase = Mathf.Repeat(idlePhase + deltaTime * idleCycleSpeed, Mathf.PI * 4f);
    }

    private float GetLandingOffset(float deltaTime)
    {
        if (landingKickAge >= landingKickDuration || landingKickDuration <= 0f)
            return 0f;

        landingKickAge += deltaTime;
        float progress = Mathf.Clamp01(landingKickAge / landingKickDuration);
        return -landingKickStrength * Mathf.Sin(progress * Mathf.PI);
    }

    private static float ResponseBlend(float response, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0f, response) * deltaTime);
    }

    private void OnPlayerJumped()
    {
        hasJumpedSinceLanding = true;
        landingKickAge = landingKickDuration;
        landingKickStrength = 0f;
    }

    private void OnPlayerLanded(float landingSpeed)
    {
        bool meaningfulLanding = hasJumpedSinceLanding ||
            (airborneTime >= 0.08f && landingSpeed >= 2f);

        if (meaningfulLanding)
        {
            landingKickStrength = Mathf.Min(maxLandingKick, landingSpeed * landingKickPerSpeed);
            landingKickAge = 0f;
        }

        hasJumpedSinceLanding = false;
        airborneTime = 0f;
    }
}
