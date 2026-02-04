#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridManager gm = (GridManager)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Grid (Editor)"))
        {
            gm.GenerateGridInEditor();
            EditorUtility.SetDirty(gm);
        }

        if (GUILayout.Button("Clear Grid (Editor)"))
        {
            gm.ClearGridInEditor();
            EditorUtility.SetDirty(gm);
        }
    }
}
#endif
