using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlightManager : MonoBehaviour
{
    [Header("Highlight Material")]
    [SerializeField] private Material highlightMaterial;

    private readonly Dictionary<Renderer, Material[]> towerOriginals = new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<Renderer, Material[]> tileOriginals = new Dictionary<Renderer, Material[]>();

    private GameObject currentTowerRoot;
    private GameObject currentTileRoot;

    public void SetSelection(GameObject towerRoot, GameObject tileRoot)
    {
        if (currentTowerRoot == towerRoot && currentTileRoot == tileRoot)
            return;

        ClearSelection();
        ApplySelection(towerRoot, tileRoot);
    }

    public void ClearSelection()
    {
        RestoreDictionary(towerOriginals);
        RestoreDictionary(tileOriginals);

        towerOriginals.Clear();
        tileOriginals.Clear();

        currentTowerRoot = null;
        currentTileRoot = null;
    }

    private void ApplySelection(GameObject towerRoot, GameObject tileRoot)
    {
        currentTowerRoot = towerRoot;
        currentTileRoot = tileRoot;

        if (towerRoot != null)
            ApplyToRoot(towerRoot, towerOriginals);

        if (tileRoot != null)
            ApplyToRoot(tileRoot, tileOriginals);
    }

    private void ApplyToRoot(GameObject root, Dictionary<Renderer, Material[]> storage)
    {
        if (root == null || highlightMaterial == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;

            Material[] original = r.sharedMaterials;
            if (original == null) continue;

            storage[r] = original;

            // Prevent double-adding if somehow already present
            bool alreadyPresent = false;
            for (int j = 0; j < original.Length; j++)
            {
                if (original[j] == highlightMaterial)
                {
                    alreadyPresent = true;
                    break;
                }
            }

            if (alreadyPresent)
                continue;

            Material[] expanded = new Material[original.Length + 1];
            for (int j = 0; j < original.Length; j++)
                expanded[j] = original[j];

            expanded[original.Length] = highlightMaterial;
            r.sharedMaterials = expanded;
        }
    }

    private void RestoreDictionary(Dictionary<Renderer, Material[]> storage)
    {
        foreach (var pair in storage)
        {
            Renderer r = pair.Key;
            if (r == null) continue;

            r.sharedMaterials = pair.Value;
        }
    }
}