using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TowerRangeVisualizer : MonoBehaviour
{
    private enum SourceKind
    {
        None = 0,
        PlacementPreview = 1,
        TargetingHover = 2,
        InspectSelection = 3
    }

    [Header("References")]
    [SerializeField] private InspectPanelUI inspectPanel;

    [Header("Visual Material")]
    [SerializeField] private Material rangeMaterial;

    [Header("Visual Settings")]
    [SerializeField] private float yOffset = 0.05f;

    private GameObject hoveredTower;

    private GameObject previewPrefab;
    private Vector3 previewPosition;
    private Quaternion previewRotation;
    private bool hasPlacementPreview;

    private SourceKind currentSource = SourceKind.None;
    private readonly List<GameObject> visuals = new List<GameObject>();

    private GameObject lastRuntimeTower;
    private GameObject lastPreviewPrefab;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float lastRange = -999f;
    private float lastBaseRange = -999f;
    private TowerRangeProfile.RangeShape lastShape;
    private bool lastWasPreview;

    private void Awake()
    {
        if (inspectPanel == null)
            inspectPanel = FindFirstObjectByType<InspectPanelUI>();
    }

    private void LateUpdate()
    {
        if (PauseState.IsPaused)
        {
            ClearVisuals();
            currentSource = SourceKind.None;
            return;
        }

        if (TryHandlePlacementPreview())
            return;

        if (TryHandleTargetingHover())
            return;

        if (TryHandleInspectSelection())
            return;

        ClearVisuals();
        currentSource = SourceKind.None;
    }

    public void SetHoveredTower(GameObject tower)
    {
        hoveredTower = tower;
    }

    public void ClearHoveredTower()
    {
        hoveredTower = null;
    }

    public void SetPlacementPreview(GameObject towerPrefab, Vector3 worldPosition, Quaternion worldRotation)
    {
        previewPrefab = towerPrefab;
        previewPosition = worldPosition;
        previewRotation = worldRotation;
        hasPlacementPreview = towerPrefab != null;
    }

    public void ClearPlacementPreview()
    {
        hasPlacementPreview = false;
        previewPrefab = null;
    }

    private bool TryHandlePlacementPreview()
    {
        if (!hasPlacementPreview || previewPrefab == null)
            return false;

        TowerCombatStats stats = previewPrefab.GetComponent<TowerCombatStats>();
        if (stats == null) stats = previewPrefab.GetComponentInChildren<TowerCombatStats>();

        TowerRangeProfile profile = previewPrefab.GetComponent<TowerRangeProfile>();
        if (profile == null) profile = previewPrefab.GetComponentInChildren<TowerRangeProfile>();

        if (stats == null || profile == null)
        {
            ClearVisuals();
            currentSource = SourceKind.None;
            return true;
        }

        RebuildIfNeeded(
            null,
            previewPrefab,
            previewPosition,
            previewRotation,
            stats.BaseRange,
            stats.BaseRange,
            profile,
            true);

        currentSource = SourceKind.PlacementPreview;
        return true;
    }

    private bool TryHandleTargetingHover()
    {
        if (hoveredTower == null)
            return false;

        TowerCombatStats stats = hoveredTower.GetComponent<TowerCombatStats>();
        if (stats == null) stats = hoveredTower.GetComponentInChildren<TowerCombatStats>();

        TowerRangeProfile profile = hoveredTower.GetComponent<TowerRangeProfile>();
        if (profile == null) profile = hoveredTower.GetComponentInChildren<TowerRangeProfile>();

        if (stats == null || profile == null)
        {
            ClearVisuals();
            currentSource = SourceKind.None;
            return true;
        }

        RebuildIfNeeded(
            hoveredTower,
            null,
            hoveredTower.transform.position,
            hoveredTower.transform.rotation,
            stats.Range,
            stats.BaseRange,
            profile,
            false);

        currentSource = SourceKind.TargetingHover;
        return true;
    }

    private bool TryHandleInspectSelection()
    {
        if (inspectPanel == null)
            return false;

        if (!inspectPanel.TryGetSelectedTower(out TowerIdentity towerIdentity, out _, out GridTile tile))
            return false;

        if (tile == null || tile.OccupiedTower == null)
            return false;

        GameObject tower = tile.OccupiedTower;
        if (towerIdentity != null)
            tower = towerIdentity.gameObject.transform.root.gameObject;

        TowerCombatStats stats = tower.GetComponent<TowerCombatStats>();
        if (stats == null) stats = tower.GetComponentInChildren<TowerCombatStats>();

        TowerRangeProfile profile = tower.GetComponent<TowerRangeProfile>();
        if (profile == null) profile = tower.GetComponentInChildren<TowerRangeProfile>();

        if (stats == null || profile == null)
        {
            ClearVisuals();
            currentSource = SourceKind.None;
            return true;
        }

        RebuildIfNeeded(
            tower,
            null,
            tower.transform.position,
            tower.transform.rotation,
            stats.Range,
            stats.BaseRange,
            profile,
            false);

        currentSource = SourceKind.InspectSelection;
        return true;
    }

    private void RebuildIfNeeded(
        GameObject runtimeTower,
        GameObject previewPrefabSource,
        Vector3 worldPosition,
        Quaternion worldRotation,
        float rangeValue,
        float baseRange,
        TowerRangeProfile profile,
        bool isPreview)
    {
        bool changed =
            runtimeTower != lastRuntimeTower ||
            previewPrefabSource != lastPreviewPrefab ||
            Vector3.Distance(worldPosition, lastPosition) > 0.001f ||
            Quaternion.Angle(worldRotation, lastRotation) > 0.1f ||
            Mathf.Abs(rangeValue - lastRange) > 0.001f ||
            Mathf.Abs(baseRange - lastBaseRange) > 0.001f ||
            lastShape != profile.Shape ||
            lastWasPreview != isPreview;

        if (!changed)
            return;

        ClearVisuals();

        switch (profile.Shape)
        {
            case TowerRangeProfile.RangeShape.Sphere:
                BuildSphereVisual(worldPosition, rangeValue);
                break;

            case TowerRangeProfile.RangeShape.SingleBox:
                BuildSingleBoxVisual(worldPosition, worldRotation, profile, rangeValue, baseRange);
                break;

            case TowerRangeProfile.RangeShape.MultiBox:
                BuildMultiBoxVisuals(worldPosition, worldRotation, profile, rangeValue, baseRange);
                break;
        }

        lastRuntimeTower = runtimeTower;
        lastPreviewPrefab = previewPrefabSource;
        lastPosition = worldPosition;
        lastRotation = worldRotation;
        lastRange = rangeValue;
        lastBaseRange = baseRange;
        lastShape = profile.Shape;
        lastWasPreview = isPreview;
    }

    private void BuildSphereVisual(Vector3 worldPosition, float radius)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "RangeVisual_Sphere";
        ring.transform.position = worldPosition + Vector3.up * yOffset;
        ring.transform.rotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

        PrepareVisualObject(ring);
        visuals.Add(ring);
    }

    private void BuildSingleBoxVisual(Vector3 towerPosition, Quaternion towerRotation, TowerRangeProfile profile, float effectiveRange, float baseRange)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "RangeVisual_SingleBox";

        Vector3 localCenter = profile.GetExtendedSingleBoxCenter(effectiveRange, baseRange);
        Vector3 worldCenter = towerPosition + (towerRotation * localCenter);
        Vector3 size = profile.GetExtendedSingleBoxSize(effectiveRange, baseRange);

        box.transform.position = worldCenter + Vector3.up * yOffset;
        box.transform.rotation = towerRotation;
        box.transform.localScale = size;

        PrepareVisualObject(box);
        visuals.Add(box);
    }

    private void BuildMultiBoxVisuals(Vector3 towerPosition, Quaternion towerRotation, TowerRangeProfile profile, float effectiveRange, float baseRange)
    {
        IReadOnlyList<TowerRangeProfile.BoxRangeDefinition> defs = profile.MultiBoxDefinitions;
        for (int i = 0; i < defs.Count; i++)
        {
            TowerRangeProfile.BoxRangeDefinition def = defs[i];
            if (def == null)
                continue;

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"RangeVisual_MultiBox_{i}";

            Vector3 localCenter = profile.GetExtendedMultiBoxCenter(def, effectiveRange, baseRange);
            Vector3 size = profile.GetExtendedMultiBoxSize(def, effectiveRange, baseRange);

            box.transform.position = towerPosition + (towerRotation * localCenter) + Vector3.up * yOffset;
            box.transform.rotation = towerRotation;
            box.transform.localScale = size;

            PrepareVisualObject(box);
            visuals.Add(box);
        }
    }

    private void PrepareVisualObject(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            if (rangeMaterial != null)
                rend.sharedMaterial = rangeMaterial;

            rend.shadowCastingMode = ShadowCastingMode.Off;
            rend.receiveShadows = false;
            rend.lightProbeUsage = LightProbeUsage.Off;
            rend.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                Destroy(visuals[i]);
        }

        visuals.Clear();

        lastRuntimeTower = null;
        lastPreviewPrefab = null;
        lastRange = -999f;
        lastBaseRange = -999f;
    }
}