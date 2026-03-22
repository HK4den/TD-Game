using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerRangeProfile : MonoBehaviour
{
    public enum RangeShape
    {
        Sphere = 0,
        SingleBox = 1,
        MultiBox = 2
    }

    [Serializable]
    public class BoxRangeDefinition
    {
        [Tooltip("Local center before extension is applied.")]
        public Vector3 localCenter = new Vector3(0f, 0f, 2f);

        [Tooltip("Base box size before range extension is applied.")]
        public Vector3 baseSize = new Vector3(1f, 1f, 4f);

        [Tooltip("Local axis this box should extend along when range increases.")]
        public Vector3 extensionAxis = Vector3.forward;
    }

    [Header("Shape")]
    [SerializeField] private RangeShape shape = RangeShape.Sphere;

    [Header("Sphere")]
    [SerializeField] private float baseSphereRadius = 4f;

    [Header("Single Box")]
    [SerializeField] private Vector3 singleBoxLocalCenter = new Vector3(0f, 0f, 2f);
    [SerializeField] private Vector3 singleBoxBaseSize = new Vector3(1f, 1f, 4f);

    [Header("Multi Box")]
    [SerializeField] private List<BoxRangeDefinition> multiBoxDefinitions = new List<BoxRangeDefinition>();

    public RangeShape Shape => shape;
    public float BaseSphereRadius => Mathf.Max(0.01f, baseSphereRadius);
    public Vector3 SingleBoxLocalCenter => singleBoxLocalCenter;
    public Vector3 SingleBoxBaseSize => singleBoxBaseSize;
    public IReadOnlyList<BoxRangeDefinition> MultiBoxDefinitions => multiBoxDefinitions;

    public Vector3 GetExtendedSingleBoxSize(float effectiveRange)
    {
        float extension = Mathf.Max(0f, effectiveRange - baseSphereRadius);

        Vector3 size = singleBoxBaseSize;
        size.z += extension;
        return size;
    }

    public Vector3 GetExtendedMultiBoxCenter(BoxRangeDefinition def, float effectiveRange)
    {
        Vector3 axis = GetNormalizedAxis(def.extensionAxis);
        float extension = Mathf.Max(0f, effectiveRange - baseSphereRadius);
        return def.localCenter + axis * (extension * 0.5f);
    }

    public Vector3 GetExtendedMultiBoxSize(BoxRangeDefinition def, float effectiveRange)
    {
        Vector3 axis = GetNormalizedAxis(def.extensionAxis);
        float extension = Mathf.Max(0f, effectiveRange - baseSphereRadius);

        Vector3 size = def.baseSize;

        Vector3 absAxis = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
        size += new Vector3(absAxis.x * extension, absAxis.y * extension, absAxis.z * extension);

        size.x = Mathf.Max(0.01f, size.x);
        size.y = Mathf.Max(0.01f, size.y);
        size.z = Mathf.Max(0.01f, size.z);

        return size;
    }

    private Vector3 GetNormalizedAxis(Vector3 axis)
    {
        if (axis.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return axis.normalized;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        baseSphereRadius = Mathf.Max(0.01f, baseSphereRadius);

        singleBoxBaseSize.x = Mathf.Max(0.01f, singleBoxBaseSize.x);
        singleBoxBaseSize.y = Mathf.Max(0.01f, singleBoxBaseSize.y);
        singleBoxBaseSize.z = Mathf.Max(0.01f, singleBoxBaseSize.z);

        if (multiBoxDefinitions == null)
            return;

        for (int i = 0; i < multiBoxDefinitions.Count; i++)
        {
            if (multiBoxDefinitions[i] == null)
                continue;

            Vector3 size = multiBoxDefinitions[i].baseSize;
            size.x = Mathf.Max(0.01f, size.x);
            size.y = Mathf.Max(0.01f, size.y);
            size.z = Mathf.Max(0.01f, size.z);
            multiBoxDefinitions[i].baseSize = size;

            if (multiBoxDefinitions[i].extensionAxis.sqrMagnitude <= 0.0001f)
                multiBoxDefinitions[i].extensionAxis = Vector3.forward;
        }
    }
#endif
}