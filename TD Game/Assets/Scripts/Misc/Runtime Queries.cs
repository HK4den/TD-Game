using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneReferences
{
    private static readonly Dictionary<(int, Type), Component> references = new Dictionary<(int, Type), Component>();
    private static readonly List<GameObject> roots = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        references.Clear();
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(Scene scene) => references.Clear();

    public static T Find<T>(Component owner) where T : Component
    {
        var key = (owner.gameObject.scene.handle, typeof(T));
        if (references.TryGetValue(key, out Component cached) && cached != null && cached.gameObject.activeInHierarchy)
            return (T)cached;
        roots.Clear();
        owner.gameObject.scene.GetRootGameObjects(roots);
        foreach (GameObject root in roots)
        {
            T found = root.GetComponentInChildren<T>();
            if (found == null) continue;
            references[key] = found;
            return found;
        }
        return UnityEngine.Object.FindFirstObjectByType<T>();
    }
}

public static class GrowingOverlap
{
    public static int Sphere(Vector3 center, float radius, ref Collider[] results, int mask, QueryTriggerInteraction triggers)
    {
        while (true)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, results, mask, triggers);
            if (count < results.Length) return count;
            Array.Resize(ref results, checked(results.Length * 2));
        }
    }

    public static int Box(Vector3 center, Vector3 extents, ref Collider[] results, Quaternion rotation, int mask, QueryTriggerInteraction triggers)
    {
        while (true)
        {
            int count = Physics.OverlapBoxNonAlloc(center, extents, results, rotation, mask, triggers);
            if (count < results.Length) return count;
            Array.Resize(ref results, checked(results.Length * 2));
        }
    }
}
