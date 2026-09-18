using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemySiphonOverlay : MonoBehaviour
{
    private readonly List<Renderer> sources = new List<Renderer>();
    private readonly List<Renderer> overlays = new List<Renderer>();
    private float remainingTime;
    private Material currentMaterial;
    private bool initialized;

    public void Show(Material material, float duration)
    {
        if (material == null)
            return;
        if (!initialized)
            CreateOverlays();
        if (currentMaterial != material)
        {
            currentMaterial = material;
            foreach (Renderer overlay in overlays)
            {
                if (overlay == null) continue;
                Mesh mesh = overlay is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                    : overlay.GetComponent<MeshFilter>().sharedMesh;
                Material[] materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
                for (int index = 0; index < materials.Length; index++)
                    materials[index] = material;
                overlay.sharedMaterials = materials;
            }
        }
        remainingTime = Mathf.Max(0.001f, duration);
        RefreshVisibility();
    }

    private void CreateOverlays()
    {
        initialized = true;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        EnemyRadiusVisualizer[] auraVisuals = GetComponentsInChildren<EnemyRadiusVisualizer>(true);
        foreach (Renderer source in renderers)
        {
            bool isAura = false;
            foreach (EnemyRadiusVisualizer aura in auraVisuals)
                isAura |= aura.OwnsRenderer(source);
            if (isAura) continue;
            SkinnedMeshRenderer skinned = source as SkinnedMeshRenderer;
            MeshFilter filter = source.GetComponent<MeshFilter>();
            Mesh mesh = skinned != null ? skinned.sharedMesh : filter != null ? filter.sharedMesh : null;
            if (mesh == null || (skinned == null && !(source is MeshRenderer))) continue;

            GameObject visual = new GameObject("Siphon Overlay");
            visual.layer = source.gameObject.layer;
            visual.transform.SetParent(source.transform, false);
            Renderer overlay;
            if (skinned != null)
            {
                SkinnedMeshRenderer copy = visual.AddComponent<SkinnedMeshRenderer>();
                copy.sharedMesh = mesh;
                copy.bones = skinned.bones;
                copy.rootBone = skinned.rootBone;
                copy.localBounds = skinned.localBounds;
                copy.updateWhenOffscreen = skinned.updateWhenOffscreen;
                overlay = copy;
            }
            else
            {
                visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                overlay = visual.AddComponent<MeshRenderer>();
            }
            overlay.shadowCastingMode = ShadowCastingMode.Off;
            overlay.receiveShadows = false;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 1;
            overlay.enabled = false;
            sources.Add(source);
            overlays.Add(overlay);
        }
    }

    private void LateUpdate()
    {
        if (!PauseState.IsPaused)
            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        for (int index = 0; index < overlays.Count; index++)
        {
            Renderer overlay = overlays[index];
            Renderer source = sources[index];
            if (overlay == null) continue;
            overlay.enabled = remainingTime > 0f && source != null && source.enabled && source.gameObject.activeInHierarchy;
            if (overlay.enabled && source is SkinnedMeshRenderer skinned && overlay is SkinnedMeshRenderer copy)
                for (int shape = 0; shape < skinned.sharedMesh.blendShapeCount; shape++)
                    copy.SetBlendShapeWeight(shape, skinned.GetBlendShapeWeight(shape));
        }
    }

    private void OnDisable()
    {
        remainingTime = 0f;
        RefreshVisibility();
    }

    private void OnDestroy()
    {
        foreach (Renderer overlay in overlays)
            if (overlay != null)
                Destroy(overlay.gameObject);
    }
}
