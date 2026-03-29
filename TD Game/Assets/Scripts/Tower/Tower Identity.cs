using System.Collections.Generic;
using UnityEngine;

public class TowerIdentity : MonoBehaviour
{
    [Header("UI Identity")]
    [SerializeField] private string displayName = "Tower";
    [SerializeField] private Sprite icon;

    [Header("Family / Tags")]
    [Tooltip("Main family key used by systems like slow-family anti-stacking. Example: Frost, Acid, Wind.")]
    [SerializeField] private string towerFamilyKey = "";

    [Tooltip("Simple extra tags for future systems. Not used yet, but available so future-you can hook into them later.")]
    [SerializeField] private List<string> extraTags = new List<string>();

    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string TowerFamilyKey => towerFamilyKey;
    public IReadOnlyList<string> ExtraTags => extraTags;

    public bool HasExtraTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        for (int i = 0; i < extraTags.Count; i++)
        {
            if (string.Equals(extraTags[i], tag, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (towerFamilyKey == null)
            towerFamilyKey = "";

        for (int i = extraTags.Count - 1; i >= 0; i--)
        {
            if (string.IsNullOrWhiteSpace(extraTags[i]))
                extraTags.RemoveAt(i);
        }
    }
#endif
}