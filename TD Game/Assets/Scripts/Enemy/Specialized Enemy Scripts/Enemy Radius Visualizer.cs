using UnityEngine;
using UnityEngine.Rendering;

public class EnemyRadiusVisualizer : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material radiusMaterial;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float yOffset = 0.02f;

    [Header("Behavior")]
    [SerializeField] private bool alwaysVisible = false;
    [SerializeField] private bool disableShadows = true;

    private float visibleUntilTime = -1f;

    private void Awake()
    {
        if (visualObject == null && targetRenderer != null)
            visualObject = targetRenderer.gameObject;

        if (targetRenderer == null && visualObject != null)
            targetRenderer = visualObject.GetComponent<Renderer>();

        ApplyMaterial();
        ApplyRadius();
        ApplyShadowSettings();
        RefreshVisibilityImmediate();
    }

    private void LateUpdate()
    {
        RefreshVisibilityImmediate();
    }

    public void SetRadius(float newRadius)
    {
        radius = Mathf.Max(0f, newRadius);
        ApplyRadius();
    }

    public void SetMaterial(Material newMaterial)
    {
        radiusMaterial = newMaterial;
        ApplyMaterial();
    }

    public void ShowForDuration(float duration)
    {
        if (alwaysVisible)
            return;

        visibleUntilTime = Mathf.Max(visibleUntilTime, Time.time + Mathf.Max(0.01f, duration));
        RefreshVisibilityImmediate();
    }

    public void SetAlwaysVisible(bool value)
    {
        alwaysVisible = value;
        RefreshVisibilityImmediate();
    }

    private void ApplyMaterial()
    {
        if (targetRenderer == null || radiusMaterial == null)
            return;

        targetRenderer.material = radiusMaterial;
    }

    private void ApplyRadius()
    {
        if (visualObject == null)
            return;

        visualObject.transform.localPosition = new Vector3(0f, yOffset, 0f);
        float diameter = Mathf.Max(0f, radius * 2f);
        visualObject.transform.localScale = new Vector3(diameter, 1f, diameter);
    }

    private void ApplyShadowSettings()
    {
        if (targetRenderer == null || !disableShadows)
            return;

        targetRenderer.shadowCastingMode = ShadowCastingMode.Off;
        targetRenderer.receiveShadows = false;
    }

    private void RefreshVisibilityImmediate()
    {
        if (visualObject == null)
            return;

        bool shouldShow = alwaysVisible || Time.time < visibleUntilTime;
        if (visualObject.activeSelf != shouldShow)
            visualObject.SetActive(shouldShow);
    }
}