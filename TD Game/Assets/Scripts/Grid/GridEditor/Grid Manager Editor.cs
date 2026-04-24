#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    private enum ToolMode
    {
        None,
        PaintTerrain,
        ReplaceTile
    }

    private ToolMode toolMode = ToolMode.PaintTerrain;

    private TerrainType currentPaint = TerrainType.Fire;

    private GridTile replaceWithPrefab;
    private bool keepOldTerrain = true;

    private GridTile hoveredTile;
    private GridManager gm;

    private bool enforcePath = true;
    private Vector2Int startCoord = new Vector2Int(0, 0);
    private Vector2Int goalCoord = new Vector2Int(19, 19);

    private void OnEnable()
    {
        gm = (GridManager)target;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Grid Tools (Editor)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
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
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(12);
        EditorGUILayout.LabelField("Editor Tool Mode", EditorStyles.boldLabel);
        toolMode = (ToolMode)EditorGUILayout.EnumPopup("Tool", toolMode);

        GUILayout.Space(8);

        if (toolMode == ToolMode.PaintTerrain)
        {
            EditorGUILayout.LabelField("Terrain Painter", EditorStyles.boldLabel);
            currentPaint = (TerrainType)EditorGUILayout.EnumPopup("Paint Terrain", currentPaint);

            GUILayout.Space(8);
            EditorGUILayout.LabelField("Path Constraint", EditorStyles.boldLabel);
            enforcePath = EditorGUILayout.Toggle("Enforce Path (No Full Block)", enforcePath);
            startCoord = EditorGUILayout.Vector2IntField("Start (x,z)", startCoord);
            goalCoord = EditorGUILayout.Vector2IntField("Goal (x,z)", goalCoord);

            EditorGUILayout.HelpBox(
                "Scene View Controls:\n" +
                "• Hover a tile to preview outline\n" +
                "• Left Click: paint selected terrain\n" +
                "• Right Click: set Normal\n" +
                "• 1 Normal, 2 Swamp, 3 Fire, 4 Energy, 5 Blocked, 6 Beam, 7 Brush, 8 Thick Brush, 9 Rubble\n" +
                "• Hold Shift to paint Normal (eraser)\n\n" +
                "If Enforce Path is ON, painting Blocked is denied if it breaks the path.",
                MessageType.Info
            );
        }
        else if (toolMode == ToolMode.ReplaceTile)
        {
            EditorGUILayout.LabelField("Tile Replacer", EditorStyles.boldLabel);

            replaceWithPrefab = (GridTile)EditorGUILayout.ObjectField(
                "Replace With Prefab",
                replaceWithPrefab,
                typeof(GridTile),
                false
            );

            keepOldTerrain = EditorGUILayout.Toggle("Keep Old Terrain", keepOldTerrain);

            EditorGUILayout.HelpBox(
                "Scene View Controls:\n" +
                "• Hover a tile to preview outline\n" +
                "• Left Click: replace hovered tile with the prefab\n" +
                "• Undo works (Ctrl+Z)\n\n" +
                "Prefab must have GridTile + Collider + Renderer.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox("Tool disabled. No Scene View editing.", MessageType.None);
        }
    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        if (toolMode == ToolMode.None)
            return;

        Event e = Event.current;
        if (e == null)
            return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        UpdateHoveredTile(e.mousePosition);
        DrawHoverOutline();

        if (toolMode == ToolMode.PaintTerrain)
        {
            HandlePaintHotkeys(e);
            HandlePainting(e);
        }
        else if (toolMode == ToolMode.ReplaceTile)
        {
            HandleReplacing(e);
        }
    }

    private void UpdateHoveredTile(Vector2 guiMousePos)
    {
        hoveredTile = null;

        Ray ray = HandleUtility.GUIPointToWorldRay(guiMousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, 9999f, ~0, QueryTriggerInteraction.Ignore))
            hoveredTile = hit.collider.GetComponent<GridTile>();
    }

    private void DrawHoverOutline()
    {
        if (hoveredTile == null)
            return;

        Renderer r = hoveredTile.GetComponent<Renderer>();
        if (r == null)
            return;

        Bounds b = r.bounds;
        float y = b.max.y + 0.01f;

        Vector3 p1 = new Vector3(b.min.x, y, b.min.z);
        Vector3 p2 = new Vector3(b.max.x, y, b.min.z);
        Vector3 p3 = new Vector3(b.max.x, y, b.max.z);
        Vector3 p4 = new Vector3(b.min.x, y, b.max.z);

        Handles.color = new Color(1f, 1f, 1f, 0.9f);
        Handles.DrawAAPolyLine(3f, p1, p2, p3, p4, p1);
    }

    private void HandlePaintHotkeys(Event e)
    {
        if (e.type != EventType.KeyDown)
            return;

        if (e.keyCode == KeyCode.Alpha1) { currentPaint = TerrainType.Normal; e.Use(); }
        if (e.keyCode == KeyCode.Alpha2) { currentPaint = TerrainType.Swamp; e.Use(); }
        if (e.keyCode == KeyCode.Alpha3) { currentPaint = TerrainType.Fire; e.Use(); }
        if (e.keyCode == KeyCode.Alpha4) { currentPaint = TerrainType.Energy; e.Use(); }
        if (e.keyCode == KeyCode.Alpha5) { currentPaint = TerrainType.Blocked; e.Use(); }
        if (e.keyCode == KeyCode.Alpha6) { currentPaint = TerrainType.Beam; e.Use(); }
        if (e.keyCode == KeyCode.Alpha7) { currentPaint = TerrainType.Brush; e.Use(); }
        if (e.keyCode == KeyCode.Alpha8) { currentPaint = TerrainType.ThickBrush; e.Use(); }
        if (e.keyCode == KeyCode.Alpha9) { currentPaint = TerrainType.Rubble; e.Use(); }
    }

    private void HandlePainting(Event e)
    {
        if (hoveredTile == null)
            return;

        bool leftClick = e.type == EventType.MouseDown && e.button == 0;
        bool rightClick = e.type == EventType.MouseDown && e.button == 1;
        if (!leftClick && !rightClick)
            return;

        TerrainType paint = currentPaint;

        if (e.shift)
            paint = TerrainType.Normal;

        if (rightClick)
            paint = TerrainType.Normal;

        if (enforcePath && paint == TerrainType.Blocked)
        {
            if (!WouldStillHavePathIfBlocked(hoveredTile))
            {
                Debug.LogWarning("Denied: blocking that tile would remove all paths from Start to Goal.");
                e.Use();
                return;
            }
        }

        Undo.RecordObject(hoveredTile, "Paint Terrain");
        hoveredTile.SetTerrain(paint);
        EditorUtility.SetDirty(hoveredTile);

        e.Use();
    }

    private bool WouldStillHavePathIfBlocked(GridTile tileToBlock)
    {
        GridPathfinder pf = Object.FindFirstObjectByType<GridPathfinder>();
        if (pf == null)
        {
            Debug.LogWarning("No GridPathfinder found in scene. Can't enforce path rule.");
            return true;
        }

        GridManager grid = (GridManager)target;
        if (grid == null)
            return true;

        grid.RebuildLookupFromChildren();

        GridTile startTile = grid.GetTile(startCoord.x, startCoord.y);
        GridTile goalTile = grid.GetTile(goalCoord.x, goalCoord.y);

        if (startTile == null || goalTile == null)
        {
            Debug.LogWarning("Start/Goal coords invalid. Can't enforce path rule.");
            return true;
        }

        TerrainType old = tileToBlock.Terrain;
        tileToBlock.SetTerrain(TerrainType.Blocked);

        List<GridTile> path = pf.FindPathAStar(startTile, goalTile);

        tileToBlock.SetTerrain(old);

        return path != null && path.Count > 0;
    }

    private void HandleReplacing(Event e)
    {
        if (hoveredTile == null || replaceWithPrefab == null)
            return;

        bool leftClick = e.type == EventType.MouseDown && e.button == 0;
        if (!leftClick)
            return;

        ReplaceTilePrefab(hoveredTile, replaceWithPrefab, keepOldTerrain);
        e.Use();
    }

    private void ReplaceTilePrefab(GridTile oldTile, GridTile newPrefab, bool keepTerrain)
    {
        int x = oldTile.X;
        int z = oldTile.Z;
        TerrainType terrain = oldTile.Terrain;

        Transform parent = oldTile.transform.parent;
        Vector3 pos = oldTile.transform.position;
        Quaternion rot = oldTile.transform.rotation;

        Undo.DestroyObjectImmediate(oldTile.gameObject);

        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab.gameObject, parent);
        Undo.RegisterCreatedObjectUndo(newObj, "Replace Tile Prefab");

        newObj.transform.position = pos;
        newObj.transform.rotation = rot;

        GridTile newTile = newObj.GetComponent<GridTile>();
        if (newTile == null)
        {
            Debug.LogError("Replacement prefab must have a GridTile component.");
            return;
        }

        newTile.Initialize(x, z);

        if (keepTerrain)
            newTile.SetTerrain(terrain);

        EditorUtility.SetDirty(newTile);
        Selection.activeGameObject = newObj;
    }
}
#endif