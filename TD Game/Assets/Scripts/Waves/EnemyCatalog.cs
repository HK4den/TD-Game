using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Wizliens/Waves/Enemy Catalog", fileName = "Enemy Catalog")]
public class EnemyCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public string displayName;
        public EnemyAgent prefab;
        public string category;
        public float threatValue = 1f;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(displayName))
                    return displayName;

                if (prefab != null)
                    return prefab.name;

                if (!string.IsNullOrWhiteSpace(id))
                    return id;

                return "Enemy";
            }
        }
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public Entry GetEntry(int index)
    {
        if (index < 0 || index >= entries.Count)
            return null;

        return entries[index];
    }

    public Entry FindById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        string trimmedId = id.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry != null && string.Equals(entry.id, trimmedId, StringComparison.Ordinal))
                return entry;
        }

        return null;
    }

    public Entry FindByPrefab(EnemyAgent prefab)
    {
        if (prefab == null)
            return null;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry != null && entry.prefab == prefab)
                return entry;
        }

        return null;
    }

    public EnemyAgent ResolvePrefab(string id, EnemyAgent fallbackPrefab)
    {
        Entry entry = FindById(id);
        if (entry != null && entry.prefab != null)
            return entry.prefab;

        return fallbackPrefab;
    }

    private void OnValidate()
    {
        if (entries == null)
            entries = new List<Entry>();

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null)
                continue;

            if (string.IsNullOrWhiteSpace(entry.displayName) && entry.prefab != null)
                entry.displayName = entry.prefab.name;

            if (string.IsNullOrWhiteSpace(entry.id))
                entry.id = CreateStableId(entry.DisplayName, i);
        }
    }

    private string CreateStableId(string source, int index)
    {
        string safeSource = string.IsNullOrWhiteSpace(source) ? $"enemy_{index + 1}" : source.Trim();
        safeSource = safeSource.ToLowerInvariant();

        char[] chars = safeSource.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            bool valid =
                (chars[i] >= 'a' && chars[i] <= 'z') ||
                (chars[i] >= '0' && chars[i] <= '9');

            if (!valid)
                chars[i] = '_';
        }

        return new string(chars);
    }
}
