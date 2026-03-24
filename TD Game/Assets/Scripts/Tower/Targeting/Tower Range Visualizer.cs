using System.Collections.Generic;
using UnityEngine;

public class TowerRangeVisualizer : MonoBehaviour
{
    [Header("Visual Material")]
    [SerializeField] private Material rangeMaterial;

    [Header("Vertical Offset")]
    [SerializeField] private float yOffset = 0.05f;

    private GameObject hoveredTower;
    private GameObject selectedTower;
    private GameObject currentShownTower;

    private readonly List<GameObject> visuals = new List<GameObject>();

    private void LateUpdate()
    {
        if (PauseState.IsPaused)
        {
            HideAll();
            return;
        }

        GameObject desired = hoveredTower != null ? hoveredTower : selectedTower;

        if (desired == currentShownTower)
        {
            if (desired != null)
                RefreshVisuals(desired);

            return;
        }

        currentShownTower = desired;

        if (currentShownTower == null)
        {
            HideAll();
            return;
        }

        RebuildVisuals(currentShownTower);
    }

    public void SetHoveredTower(GameObject tower)
    {
        hoveredTower = tower;
    }

    public void SetSelectedTower(GameObject tower)
    {
        selectedTower = tower;
    }

    public void ClearSelectedTower()
    {
        selectedTower = null;
    }

    private void RebuildVisuals(GameObject tower)
    {
        HideAll();

        if (tower == null)
            return;

        TowerCombatStats stats = tower.GetComponent<TowerCombatStats>();
        if (stats == null) stats = tower.GetComponentInChildren<TowerCombatStats>();

        TowerRangeProfile profile = tower.GetComponent<TowerRangeProfile>();
        if (profile == null) profile = tower.GetComponentInChildren<TowerRangeProfile>();

        if (stats == null || profile == null)
            return;

        switch (profile.Shape)
        {
            case TowerRangeProfile.RangeShape.Sphere:
                BuildSphereVisual(tower, stats.Range);
                break;

            case TowerRangeProfile.RangeShape.SingleBox:
                BuildSingleBoxVisual(tower, profile, stats.Range);
                break;

            case TowerRangeProfile.RangeShape.MultiBox:
                BuildMultiBoxVisuals(tower, profile, stats.Range);
                break;
        }
    }

    private void RefreshVisuals(GameObject tower)
    {
        if (tower == null)
        {
            HideAll();
            return;
        }

        RebuildVisuals(tower);
    }

    private void BuildSphereVisual(GameObject tower, float radius)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "RangeVisual_Sphere";
        ring.transform.position = tower.transform.position + Vector3.up * yOffset;
        ring.transform.rotation = Quaternion.identity;
        ring.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

        PrepareVisualObject(ring);
        visuals.Add(ring);
    }

    private void BuildSingleBoxVisual(GameObject tower, TowerRangeProfile profile, float effectiveRange)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "RangeVisual_SingleBox";

        Vector3 worldCenter = tower.transform.TransformPoint(profile.SingleBoxLocalCenter);
        Vector3 size = profile.GetExtendedSingleBoxSize(effectiveRange);

        box.transform.position = worldCenter + Vector3.up * yOffset;
        box.transform.rotation = tower.transform.rotation;
        box.transform.localScale = size;

        PrepareVisualObject(box);
        visuals.Add(box);
    }

    private void BuildMultiBoxVisuals(GameObject tower, TowerRangeProfile profile, float effectiveRange)
    {
        IReadOnlyList<TowerRangeProfile.BoxRangeDefinition> defs = profile.MultiBoxDefinitions;
        for (int i = 0; i < defs.Count; i++)
        {
            TowerRangeProfile.BoxRangeDefinition def = defs[i];
            if (def == null)
                continue;

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"RangeVisual_MultiBox_{i}";

            Vector3 localCenter = profile.GetExtendedMultiBoxCenter(def, effectiveRange);
            Vector3 size = profile.GetExtendedMultiBoxSize(def, effectiveRange);

            box.transform.position = tower.transform.TransformPoint(localCenter) + Vector3.up * yOffset;
            box.transform.rotation = tower.transform.rotation;
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
        if (rend != null && rangeMaterial != null)
            rend.sharedMaterial = rangeMaterial;
    }

    private void HideAll()
    {
        for (int i = 0; i < visuals.Count; i++)
        {
            if (visuals[i] != null)
                Destroy(visuals[i]);
        }

        visuals.Clear();
        currentShownTower = null;
    }
}