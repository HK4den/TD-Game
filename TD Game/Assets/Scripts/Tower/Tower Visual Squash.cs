using UnityEngine;

public class TowerVisualSquash : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform visualTarget;

    [Header("Fire Pulse")]
    [SerializeField]
    private AnimationCurve fireCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.12f, -1f),
        new Keyframe(0.32f, 0.65f),
        new Keyframe(0.55f, -0.2f),
        new Keyframe(1f, 0f)
    );
    [SerializeField] private float fireDuration = 0.18f;
    [SerializeField] private float fireIntensity = 1f;

    [Header("Money Pulse")]
    [SerializeField]
    private AnimationCurve moneyCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.18f, -0.8f),
        new Keyframe(0.42f, 0.9f),
        new Keyframe(0.7f, -0.15f),
        new Keyframe(1f, 0f)
    );
    [SerializeField] private float moneyDuration = 0.28f;
    [SerializeField] private float moneyIntensity = 1f;

    [Header("Shape Strength")]
    [Tooltip("How much X/Z changes from the curve value.")]
    [SerializeField] private float horizontalScaleStrength = 0.18f;

    [Tooltip("How much Y changes from the curve value.")]
    [SerializeField] private float verticalScaleStrength = 0.22f;

    [Header("Retriggering")]
    [Tooltip("How much extra intensity gets added if triggered again while already animating.")]
    [SerializeField] private float retriggerIntensityAdd = 0.2f;

    [Tooltip("Maximum intensity clamp.")]
    [SerializeField] private float maxIntensity = 2f;

    [Header("Bottom Anchor")]
    [Tooltip("If 0 or less, height is auto-calculated from child renderers.")]
    [SerializeField] private float visualHeight = 0f;

    private Vector3 baseLocalScale;
    private Vector3 baseLocalPosition;
    private float resolvedHeight = 1f;

    private AnimationCurve activeCurve;
    private float activeDuration = 0.2f;
    private float activeIntensity = 1f;
    private float animTimer = 0f;
    private bool isAnimating = false;

    private void Awake()
    {
        if (visualTarget == null)
            visualTarget = transform;

        baseLocalScale = visualTarget.localScale;
        baseLocalPosition = visualTarget.localPosition;

        resolvedHeight = visualHeight > 0f ? visualHeight : CalculateVisualHeight();
        if (resolvedHeight <= 0.0001f)
            resolvedHeight = 1f;
    }

    private void Update()
    {
        if (PauseState.IsPaused)
            return;

        if (!isAnimating)
        {
            ResetVisual();
            return;
        }

        animTimer += Time.deltaTime;
        float normalizedTime = activeDuration <= 0.0001f ? 1f : Mathf.Clamp01(animTimer / activeDuration);

        float curveValue = activeCurve != null ? activeCurve.Evaluate(normalizedTime) : 0f;
        ApplyCurveValue(curveValue * activeIntensity);

        if (normalizedTime >= 1f)
        {
            isAnimating = false;
            animTimer = 0f;
            activeIntensity = 1f;
            ResetVisual();
        }
    }

    public void TriggerFirePulse()
    {
        PlayPulse(fireCurve, fireDuration, fireIntensity);
    }

    public void TriggerMoneyPulse()
    {
        PlayPulse(moneyCurve, moneyDuration, moneyIntensity);
    }

    public void TriggerCustomPulse(AnimationCurve curve, float duration, float intensity)
    {
        PlayPulse(curve, duration, intensity);
    }

    private void PlayPulse(AnimationCurve curve, float duration, float intensity)
    {
        intensity = Mathf.Max(0f, intensity);
        duration = Mathf.Max(0.01f, duration);

        if (!isAnimating)
        {
            activeCurve = curve;
            activeDuration = duration;
            activeIntensity = Mathf.Min(intensity, maxIntensity);
            animTimer = 0f;
            isAnimating = true;
            return;
        }

        activeCurve = curve;
        activeDuration = duration;
        activeIntensity = Mathf.Min(Mathf.Max(activeIntensity, intensity) + retriggerIntensityAdd, maxIntensity);

        float restartPoint = duration * 0.08f;
        animTimer = Mathf.Clamp(restartPoint, 0f, activeDuration);
        isAnimating = true;
    }

    private void ApplyCurveValue(float value)
    {
        float yMultiplier = 1f + (value * verticalScaleStrength);

        // invert based on Y so they are tightly linked
        float xzMultiplier = Mathf.Pow(1f / Mathf.Sqrt(yMultiplier), horizontalScaleStrength);

        yMultiplier = Mathf.Max(0.05f, yMultiplier);
        xzMultiplier = Mathf.Max(0.05f, xzMultiplier);

        float xScale = baseLocalScale.x * xzMultiplier;
        float yScale = baseLocalScale.y * yMultiplier;
        float zScale = baseLocalScale.z * xzMultiplier;

        visualTarget.localScale = new Vector3(xScale, yScale, zScale);

        float localScaleYRatio = baseLocalScale.y <= 0.0001f ? 1f : (yScale / baseLocalScale.y);
        float yOffset = resolvedHeight * (1f - localScaleYRatio) * 0.5f;

        visualTarget.localPosition = baseLocalPosition + new Vector3(0f, yOffset, 0f);
    }

    private void ResetVisual()
    {
        visualTarget.localScale = baseLocalScale;
        visualTarget.localPosition = baseLocalPosition;
    }

    private float CalculateVisualHeight()
    {
        Renderer[] renderers = visualTarget.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return 1f;

        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        float worldHeight = combined.size.y;
        float parentLossyY = Mathf.Max(0.0001f, visualTarget.lossyScale.y);
        return worldHeight / parentLossyY;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fireDuration = Mathf.Max(0.01f, fireDuration);
        moneyDuration = Mathf.Max(0.01f, moneyDuration);
        fireIntensity = Mathf.Max(0f, fireIntensity);
        moneyIntensity = Mathf.Max(0f, moneyIntensity);
        horizontalScaleStrength = Mathf.Max(0f, horizontalScaleStrength);
        verticalScaleStrength = Mathf.Max(0f, verticalScaleStrength);
        retriggerIntensityAdd = Mathf.Max(0f, retriggerIntensityAdd);
        maxIntensity = Mathf.Max(0.01f, maxIntensity);
    }
#endif
}