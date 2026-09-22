using UnityEngine;
using UnityEngine.Rendering;

public class EnemyRadiusVisualizer : MonoBehaviour
{
    public enum SpinDirection { Clockwise, Counterclockwise, Random }

    [Header("Visual")]
    [SerializeField] private GameObject visualObject;
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material radiusMaterial;
    [SerializeField] private float radius = 2f;
    [SerializeField] private float yOffset = 0.02f;
    [Min(0.001f)] [SerializeField] private float groundThickness = 0.01f;

    [Header("Spin")]
    [SerializeField] private bool spin = false;
    [SerializeField] private SpinDirection spinDirection = SpinDirection.Clockwise;
    [SerializeField] private bool randomizeSpinSpeed = false;
    [Min(0f)] [SerializeField] private float spinSpeed = 6f;
    [Min(0f)] [SerializeField] private float minimumSpinSpeed = 5f;
    [Min(0f)] [SerializeField] private float maximumSpinSpeed = 8f;

    [Header("Behavior")]
    [SerializeField] private bool alwaysVisible = false;
    [SerializeField] private bool disableShadows = true;

    private float visibleUntilTime = -1f;
    private static long spawnCounter;
    private long layerIndex;
    private EnemyHealth ownerHealth;
    private Transform anchor;
    private GridManager grid;
    private Bounds meshBounds;
    private int flatAxis;
    private Quaternion flatRotation;
    private float chosenSpinSpeed;
    private float spinAngle;
    private bool ready;
    private float appliedRadius = float.NaN;
    private float appliedThickness = float.NaN;
    private Vector3 visualScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOrdering()
    {
        spawnCounter = 0;
    }

    private void Awake()
    {
        if (visualObject == null && targetRenderer != null)
            visualObject = targetRenderer.gameObject;

        if (targetRenderer == null && visualObject != null)
            targetRenderer = visualObject.GetComponent<Renderer>();

        if (visualObject == null || visualObject == gameObject || transform.IsChildOf(visualObject.transform))
        {
            Debug.LogWarning($"[{name}] Radius visual must be a separate visual child, not the enemy or visualizer object itself.", this);
            return;
        }

        ownerHealth = GetComponentInParent<EnemyHealth>();
        anchor = ownerHealth != null ? ownerHealth.transform : transform;
        grid = SceneReferences.Find<GridManager>(this);
        layerIndex = spawnCounter++;
        if (targetRenderer != null)
            targetRenderer.sortingOrder = 32767 - (int)System.Math.Min(65535L, layerIndex);
        MeshFilter meshFilter = visualObject.GetComponent<MeshFilter>();
        SpriteRenderer spriteRenderer = targetRenderer as SpriteRenderer;
        meshBounds = meshFilter != null && meshFilter.sharedMesh != null
            ? meshFilter.sharedMesh.bounds
            : spriteRenderer != null && spriteRenderer.sprite != null
                ? spriteRenderer.sprite.bounds : new Bounds(Vector3.zero, Vector3.one);
        Vector3 authoredScale = visualObject.transform.localScale;
        Vector3 size = Vector3.Scale(meshBounds.size, new Vector3(
            Mathf.Abs(authoredScale.x), Mathf.Abs(authoredScale.y), Mathf.Abs(authoredScale.z)));
        flatAxis = 1;
        if (size.z < Mathf.Min(size.x, size.y) * 0.1f)
            flatAxis = 2;
        else if (size.x < Mathf.Min(size.y, size.z) * 0.1f)
            flatAxis = 0;
        flatRotation = flatAxis == 2 ? Quaternion.Euler(90f, 0f, 0f)
            : flatAxis == 0 ? Quaternion.Euler(0f, 0f, 90f) : Quaternion.identity;
        visualObject.transform.SetParent(null, true);
        float minimum = Mathf.Max(0f, Mathf.Min(minimumSpinSpeed, maximumSpinSpeed));
        float maximum = Mathf.Max(minimum, Mathf.Max(minimumSpinSpeed, maximumSpinSpeed));
        chosenSpinSpeed = randomizeSpinSpeed ? Random.Range(minimum, maximum) : Mathf.Max(0f, spinSpeed);
        if (spinDirection == SpinDirection.Counterclockwise || (spinDirection == SpinDirection.Random && Random.value < 0.5f))
            chosenSpinSpeed = -chosenSpinSpeed;
        ready = true;
        ApplyMaterial();
        ApplyRadius();
        ApplyShadowSettings();
        RefreshVisibilityImmediate();
    }

    private void LateUpdate()
    {
        if (!ready)
            return;
        if (spin && !PauseState.IsPaused)
            spinAngle = Mathf.Repeat(spinAngle + chosenSpinSpeed * Time.deltaTime, 360f);
        RefreshVisibilityImmediate();
        if (visualObject != null && visualObject.activeSelf)
            ApplyRadius();
    }

    public void SetRadius(float newRadius)
    {
        newRadius = Mathf.Max(0f, newRadius);
        if (radius == newRadius)
            return;
        radius = newRadius;
        ApplyRadius();
    }

    public bool OwnsRenderer(Renderer candidate)
    {
        return visualObject != null && candidate != null
            && (candidate.transform == visualObject.transform || candidate.transform.IsChildOf(visualObject.transform));
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
        if (alwaysVisible == value)
            return;
        alwaysVisible = value;
        RefreshVisibilityImmediate();
    }

    private void ApplyMaterial()
    {
        if (targetRenderer == null || radiusMaterial == null)
            return;

        targetRenderer.sharedMaterial = radiusMaterial;
    }

    private void ApplyRadius()
    {
        if (!ready || visualObject == null)
            return;

        if (appliedRadius != radius || appliedThickness != groundThickness)
        {
            float diameter = Mathf.Max(0f, radius * 2f);
            Vector3 size = meshBounds.size;
            visualScale = new Vector3(diameter / Mathf.Max(0.0001f, size.x),
                diameter / Mathf.Max(0.0001f, size.y), diameter / Mathf.Max(0.0001f, size.z));
            visualScale[flatAxis] = size[flatAxis] > 0.0001f ? Mathf.Max(0.001f, groundThickness) / size[flatAxis] : 1f;
            visualObject.transform.localScale = visualScale;
            appliedRadius = radius;
            appliedThickness = groundThickness;
        }
        Quaternion rotation = Quaternion.AngleAxis(spinAngle, Vector3.up) * flatRotation;
        float layerDrop = (float)(0.005 * layerIndex / (250.0 + layerIndex));
        Vector3 center = anchor.position;
        center.y = (grid != null ? grid.transform.position.y : anchor.position.y)
            + Mathf.Max(0.006f, yOffset) - layerDrop + Mathf.Max(0.001f, groundThickness) * 0.5f;
        visualObject.transform.SetPositionAndRotation(center - rotation * Vector3.Scale(meshBounds.center, visualScale), rotation);
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
        if (!ready || visualObject == null)
            return;

        bool shouldShow = isActiveAndEnabled && (ownerHealth == null || ownerHealth.IsAlive)
            && (alwaysVisible || Time.time < visibleUntilTime);
        if (visualObject.activeSelf != shouldShow)
            visualObject.SetActive(shouldShow);
    }

    private void OnDisable()
    {
        if (ready && visualObject != null)
            visualObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ready && visualObject != null)
            Destroy(visualObject);
    }
}
