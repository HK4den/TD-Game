using System.Collections.Generic;
using System;
using UnityEditor;
using UnityEngine;

public class WaveEditorWindow : EditorWindow
{
    private const float LeftPanelWidth = 280f;
    private const float RightPanelWidth = 330f;
    private const float WaveRowHeight = 56f;
    private const float CompactWaveRowHeight = 24f;
    private const string UncategorizedCategory = "Uncategorized";
    private const string CollapsedStepPrefix = "▶";
    private const string ExpandedStepPrefix = "▼";

    private static readonly WaveSet.WaveStepType[] EditableStepTypes =
    {
        WaveSet.WaveStepType.SingleGroup,
        WaveSet.WaveStepType.Alternating,
        WaveSet.WaveStepType.Sequence,
        WaveSet.WaveStepType.RandomPool,
        WaveSet.WaveStepType.Delay
    };

    private static readonly string[] EditableStepTypeLabels =
    {
        "Single Enemy Group",
        "Pattern By Count",
        "Pattern Repeat",
        "Weighted Random",
        "Wait Only"
    };

    private WaveSet waveSet;
    private EnemyCatalog enemyCatalog;

    private SerializedObject waveSetObject;
    private SerializedObject catalogObject;

    private int selectedWaveIndex;
    private Vector2 waveListScroll;
    private Vector2 waveEditorScroll;
    private Vector2 catalogScroll;
    private bool autoSaveAssets = true;
    private bool compactWaveList;

    private readonly Dictionary<string, string> enemyPickerCategories = new Dictionary<string, string>();
    private static bool saveQueued;

    public WaveSet CurrentWaveSet => waveSet;
    public EnemyCatalog CurrentEnemyCatalog => enemyCatalog;
    public int SelectedWaveIndex => selectedWaveIndex;

    private class WaveSummary
    {
        public int enemyCount;
        public int killMoney;
        public bool moneyIsApproximate;
        public readonly Dictionary<string, int> enemyCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, int> categoryCounts = new Dictionary<string, int>();

        public string EnemyMixText => FormatCounts(enemyCounts);
        public string CategoryMixText => FormatCounts(categoryCounts);

        private static string FormatCounts(Dictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "None";

            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, int> pair in counts)
                parts.Add($"{pair.Key} x{pair.Value}");

            parts.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", parts);
        }
    }

    [MenuItem("Tools/Wizliens/Wave Editor")]
    public static void Open()
    {
        GetWindow<WaveEditorWindow>("Wave Editor");
    }

    public void SelectWave(int index)
    {
        if (waveSet == null)
            return;

        selectedWaveIndex = Mathf.Clamp(index, 0, Mathf.Max(0, waveSet.WaveCount - 1));
        Repaint();
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is WaveSet selectedWaveSet)
        {
            SetWaveSet(selectedWaveSet);
            Repaint();
            return;
        }

        if (Selection.activeObject is EnemyCatalog selectedCatalog)
        {
            enemyCatalog = selectedCatalog;
            catalogObject = new SerializedObject(enemyCatalog);
            Repaint();
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (waveSet == null)
        {
            DrawEmptyState();
            return;
        }

        EnsureSerializedObjects();

        waveSetObject.Update();
        if (catalogObject != null)
            catalogObject.Update();

        EditorGUILayout.BeginHorizontal();
        DrawWaveList();
        DrawSelectedWaveEditor();
        DrawEnemyCatalogPanel();
        EditorGUILayout.EndHorizontal();

        bool changed = waveSetObject.ApplyModifiedProperties();
        if (catalogObject != null)
            changed |= catalogObject.ApplyModifiedProperties();

        if (changed && autoSaveAssets)
            QueueAssetSave();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        WaveSet newWaveSet = (WaveSet)EditorGUILayout.ObjectField(waveSet, typeof(WaveSet), false, GUILayout.Width(260f));
        if (EditorGUI.EndChangeCheck())
            SetWaveSet(newWaveSet);

        EditorGUI.BeginChangeCheck();
        EnemyCatalog newCatalog = (EnemyCatalog)EditorGUILayout.ObjectField(enemyCatalog, typeof(EnemyCatalog), false, GUILayout.Width(260f));
        if (EditorGUI.EndChangeCheck())
        {
            enemyCatalog = newCatalog;
            catalogObject = enemyCatalog != null ? new SerializedObject(enemyCatalog) : null;
            AssignCatalogToWaveSetIfNeeded();
        }

        if (GUILayout.Button("Create Wave Set", EditorStyles.toolbarButton, GUILayout.Width(110f)))
            CreateWaveSetAsset();

        if (GUILayout.Button("Create Catalog", EditorStyles.toolbarButton, GUILayout.Width(105f)))
            CreateEnemyCatalogAsset();

        if (GUILayout.Button("Convert Selected Spawner", EditorStyles.toolbarButton, GUILayout.Width(155f)))
            ConvertSelectedSpawner();

        if (GUILayout.Button("Search", EditorStyles.toolbarButton, GUILayout.Width(65f)))
            WaveSearchWindow.Open(this);

        compactWaveList = GUILayout.Toggle(compactWaveList, "Compact Waves", EditorStyles.toolbarButton, GUILayout.Width(105f));

        autoSaveAssets = GUILayout.Toggle(autoSaveAssets, "Auto Save", EditorStyles.toolbarButton, GUILayout.Width(80f));

        if (GUILayout.Button("Save Now", EditorStyles.toolbarButton, GUILayout.Width(75f)))
            AssetDatabase.SaveAssets();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEmptyState()
    {
        EditorGUILayout.Space(16f);
        EditorGUILayout.LabelField("No Wave Set selected.", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Create or assign a WaveSet asset. The WaveSpawner can then reference that asset across scenes.", MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create Wave Set", GUILayout.Width(160f)))
            CreateWaveSetAsset();

        if (GUILayout.Button("Convert Selected WaveSpawner", GUILayout.Width(220f)))
            ConvertSelectedSpawner();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawWaveList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));

        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);

        SerializedProperty waves = waveSetObject.FindProperty("waves");
        waveListScroll = EditorGUILayout.BeginScrollView(waveListScroll);

        for (int i = 0; i < waves.arraySize; i++)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(i);
            string label = wave.FindPropertyRelative("editorLabel").stringValue;
            int enemyCount = waveSet.CountEstimatedEnemies(i);
            float duration = waveSet.EstimateSpawnDuration(i);

            string display = string.IsNullOrWhiteSpace(label)
                ? $"Wave {i + 1:00}"
                : $"Wave {i + 1:00} - {label}";

            GUIStyle style = CreateWaveRowStyle(selectedWaveIndex == i);
            float rowHeight = compactWaveList ? CompactWaveRowHeight : WaveRowHeight;
            Rect rowRect = GUILayoutUtility.GetRect(LeftPanelWidth - 16f, rowHeight, GUILayout.ExpandWidth(true));
            if (GUI.Button(rowRect, GUIContent.none, style))
            {
                selectedWaveIndex = i;
                GUI.FocusControl(null);
            }

            if (compactWaveList)
            {
                Rect compactRect = new Rect(rowRect.x + 6f, rowRect.y + 3f, rowRect.width - 12f, 18f);
                GUI.Label(compactRect, $"{display} | {enemyCount} enemies | {duration:0.#}s", EditorStyles.miniLabel);
            }
            else
            {
                Rect titleRect = new Rect(rowRect.x + 8f, rowRect.y + 7f, rowRect.width - 16f, 18f);
                Rect detailRect = new Rect(rowRect.x + 8f, rowRect.y + 29f, rowRect.width - 16f, 18f);
                GUI.Label(titleRect, display, EditorStyles.boldLabel);
                GUI.Label(detailRect, $"{enemyCount} enemies | {duration:0.#}s spawn", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add"))
            AddWave(waves);

        using (new EditorGUI.DisabledScope(waves.arraySize == 0 || selectedWaveIndex < 0 || selectedWaveIndex >= waves.arraySize))
        {
            if (GUILayout.Button("Duplicate"))
                DuplicateWave(waves);

            if (GUILayout.Button("Delete"))
                DeleteWave(waves);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(selectedWaveIndex <= 0))
        {
            if (GUILayout.Button("Move Up"))
                MoveWave(waves, -1);
        }

        using (new EditorGUI.DisabledScope(selectedWaveIndex < 0 || selectedWaveIndex >= waves.arraySize - 1))
        {
            if (GUILayout.Button("Move Down"))
                MoveWave(waves, 1);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Validate Wave Set"))
            ValidateWaveSet();

        EditorGUILayout.EndVertical();
    }

    private void DrawSelectedWaveEditor()
    {
        EditorGUILayout.BeginVertical();

        SerializedProperty waves = waveSetObject.FindProperty("waves");
        if (waves.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Add a wave to begin editing.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, waves.arraySize - 1);
        SerializedProperty wave = waves.GetArrayElementAtIndex(selectedWaveIndex);

        EditorGUILayout.LabelField(waveSet.GetWaveDisplayName(selectedWaveIndex), EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(wave.FindPropertyRelative("editorLabel"), new GUIContent("Designer Label"));
        EditorGUILayout.PropertyField(wave.FindPropertyRelative("completionReward"), new GUIContent("Completion Reward"));

        SerializedProperty steps = wave.FindPropertyRelative("steps");
        waveEditorScroll = EditorGUILayout.BeginScrollView(waveEditorScroll);

        EditorGUILayout.PropertyField(wave.FindPropertyRelative("notes"), new GUIContent("Notes"));
        DrawWaveSummaryPanel(wave, selectedWaveIndex);

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel);

        for (int i = 0; i < steps.arraySize; i++)
        {
            DrawStep(steps, i);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Single Group"))
            AddStep(steps, WaveSet.WaveStepType.SingleGroup);

        if (GUILayout.Button("+ Pattern Count"))
            AddStep(steps, WaveSet.WaveStepType.Alternating);

        if (GUILayout.Button("+ Pattern Repeat"))
            AddStep(steps, WaveSet.WaveStepType.Sequence);

        if (GUILayout.Button("+ Random"))
            AddStep(steps, WaveSet.WaveStepType.RandomPool);

        if (GUILayout.Button("+ Wait"))
            AddStep(steps, WaveSet.WaveStepType.Delay);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawStep(SerializedProperty steps, int index)
    {
        SerializedProperty step = steps.GetArrayElementAtIndex(index);
        SerializedProperty type = step.FindPropertyRelative("type");
        SerializedProperty collapsed = step.FindPropertyRelative("editorCollapsed");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(collapsed.boolValue ? CollapsedStepPrefix : ExpandedStepPrefix, EditorStyles.miniButton, GUILayout.Width(24f)))
            collapsed.boolValue = !collapsed.boolValue;

        EditorGUILayout.LabelField(GetStepSummary(step, index), EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Duplicate", GUILayout.Width(72f)))
        {
            DuplicateStep(steps, index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("Up", GUILayout.Width(36f)) && index > 0)
        {
            steps.MoveArrayElement(index, index - 1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("Down", GUILayout.Width(54f)) && index < steps.arraySize - 1)
        {
            steps.MoveArrayElement(index, index + 1);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("X", GUILayout.Width(28f)))
        {
            steps.DeleteArrayElementAtIndex(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.EndHorizontal();

        if (collapsed.boolValue)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        WaveSet.WaveStepType stepType = (WaveSet.WaveStepType)type.enumValueIndex;
        if (stepType == WaveSet.WaveStepType.Burst)
        {
            stepType = WaveSet.WaveStepType.SingleGroup;
            type.enumValueIndex = (int)stepType;
        }

        EditorGUILayout.PropertyField(step.FindPropertyRelative("label"));
        DrawStepTypePopup(type, stepType);
        stepType = (WaveSet.WaveStepType)type.enumValueIndex;
        EditorGUILayout.HelpBox(GetStepTypeHelp(stepType), MessageType.None);

        if (stepType == WaveSet.WaveStepType.Delay)
        {
            EditorGUILayout.PropertyField(step.FindPropertyRelative("delayDuration"), new GUIContent("Wait Time"));
        }
        else
        {
            if (stepType == WaveSet.WaveStepType.SingleGroup || stepType == WaveSet.WaveStepType.Burst)
                DrawEnemyRef(step.FindPropertyRelative("enemy"), "Enemy");
            else if (stepType == WaveSet.WaveStepType.Alternating || stepType == WaveSet.WaveStepType.Sequence)
                DrawEnemyRefList(step.FindPropertyRelative("enemies"));
            else if (stepType == WaveSet.WaveStepType.RandomPool)
                DrawRandomPool(step.FindPropertyRelative("randomPool"));

            if (stepType == WaveSet.WaveStepType.Sequence)
                EditorGUILayout.PropertyField(step.FindPropertyRelative("repeatCount"), new GUIContent("Pattern Repeats"));
            else
                EditorGUILayout.PropertyField(step.FindPropertyRelative("count"), new GUIContent("Total Spawns"));

            EditorGUILayout.PropertyField(step.FindPropertyRelative("spawnInterval"), new GUIContent("Time Between Spawns"));
        }

        if (stepType != WaveSet.WaveStepType.Delay)
            EditorGUILayout.PropertyField(step.FindPropertyRelative("delayAfterStep"), new GUIContent("Pause After Step"));

        EditorGUILayout.EndVertical();
    }

    private GUIStyle CreateWaveRowStyle(bool selected)
    {
        GUIStyle style = new GUIStyle(selected ? EditorStyles.helpBox : GUI.skin.button);
        style.alignment = TextAnchor.UpperLeft;
        style.padding = new RectOffset(0, 0, 0, 0);
        style.margin = new RectOffset(3, 3, 3, 5);
        return style;
    }

    private void DrawStepTypePopup(SerializedProperty type, WaveSet.WaveStepType currentType)
    {
        int currentIndex = 0;
        for (int i = 0; i < EditableStepTypes.Length; i++)
        {
            if (EditableStepTypes[i] == currentType)
            {
                currentIndex = i;
                break;
            }
        }

        int selectedIndex = EditorGUILayout.Popup("Step Type", currentIndex, EditableStepTypeLabels);
        type.enumValueIndex = (int)EditableStepTypes[Mathf.Clamp(selectedIndex, 0, EditableStepTypes.Length - 1)];
    }

    private string GetStepTypeHelp(WaveSet.WaveStepType type)
    {
        switch (type)
        {
            case WaveSet.WaveStepType.SingleGroup:
                return "Spawns one enemy type Total Spawns times. Time Between Spawns controls spacing inside this step.";

            case WaveSet.WaveStepType.Alternating:
                return "Cycles through the enemy pattern until Total Spawns is reached. Example: A, B, A, B, A.";

            case WaveSet.WaveStepType.Sequence:
                return "Spawns the full enemy pattern, then repeats that whole pattern Pattern Repeats times. Example: A, B, C, A, B, C.";

            case WaveSet.WaveStepType.RandomPool:
                return "Picks each spawn from the weighted list. Higher weight means more likely.";

            case WaveSet.WaveStepType.Delay:
                return "Waits without spawning enemies. Use this only when you want a pause that is its own step.";

            default:
                return "";
        }
    }

    private string GetStepTypeLabel(WaveSet.WaveStepType type)
    {
        for (int i = 0; i < EditableStepTypes.Length; i++)
        {
            if (EditableStepTypes[i] == type)
                return EditableStepTypeLabels[i];
        }

        return type.ToString();
    }

    private void DrawWaveSummaryPanel(SerializedProperty wave, int waveIndex)
    {
        WaveSummary summary = BuildWaveSummary(wave);
        int completionReward = wave.FindPropertyRelative("completionReward").intValue;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Wave Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            $"{summary.enemyCount} enemies | {waveSet.EstimateSpawnDuration(waveIndex):0.#}s spawn | Kill money {(summary.moneyIsApproximate ? "~" : "")}{summary.killMoney} | Completion {completionReward} | Total {(summary.moneyIsApproximate ? "~" : "")}{summary.killMoney + completionReward}");

        EditorGUILayout.LabelField("Enemy Mix", summary.EnemyMixText);
        EditorGUILayout.LabelField("Categories", summary.CategoryMixText);
        EditorGUILayout.EndVertical();
    }

    private WaveSummary BuildWaveSummary(SerializedProperty wave)
    {
        WaveSummary summary = new WaveSummary();
        SerializedProperty steps = wave.FindPropertyRelative("steps");

        for (int i = 0; i < steps.arraySize; i++)
            AddStepToSummary(steps.GetArrayElementAtIndex(i), summary);

        return summary;
    }

    private void AddStepToSummary(SerializedProperty step, WaveSummary summary)
    {
        WaveSet.WaveStepType type = (WaveSet.WaveStepType)step.FindPropertyRelative("type").enumValueIndex;
        if (type == WaveSet.WaveStepType.Delay)
            return;

        if (type == WaveSet.WaveStepType.Sequence)
        {
            int repeats = Mathf.Max(0, step.FindPropertyRelative("repeatCount").intValue);
            SerializedProperty enemies = step.FindPropertyRelative("enemies");
            for (int repeat = 0; repeat < repeats; repeat++)
            {
                for (int i = 0; i < enemies.arraySize; i++)
                    AddEnemyRefToSummary(enemies.GetArrayElementAtIndex(i), 1, summary);
            }
            return;
        }

        if (type == WaveSet.WaveStepType.Alternating)
        {
            int count = Mathf.Max(0, step.FindPropertyRelative("count").intValue);
            SerializedProperty enemies = step.FindPropertyRelative("enemies");
            if (enemies.arraySize == 0)
                return;

            for (int i = 0; i < count; i++)
                AddEnemyRefToSummary(enemies.GetArrayElementAtIndex(i % enemies.arraySize), 1, summary);
            return;
        }

        if (type == WaveSet.WaveStepType.RandomPool)
        {
            AddRandomPoolToSummary(step, summary);
            return;
        }

        AddEnemyRefToSummary(step.FindPropertyRelative("enemy"), Mathf.Max(0, step.FindPropertyRelative("count").intValue), summary);
    }

    private void AddRandomPoolToSummary(SerializedProperty step, WaveSummary summary)
    {
        SerializedProperty randomPool = step.FindPropertyRelative("randomPool");
        int count = Mathf.Max(0, step.FindPropertyRelative("count").intValue);
        int totalWeight = 0;

        for (int i = 0; i < randomPool.arraySize; i++)
            totalWeight += Mathf.Max(0, randomPool.GetArrayElementAtIndex(i).FindPropertyRelative("weight").intValue);

        if (totalWeight <= 0)
            return;

        summary.moneyIsApproximate = true;
        for (int i = 0; i < randomPool.arraySize; i++)
        {
            SerializedProperty option = randomPool.GetArrayElementAtIndex(i);
            int weight = Mathf.Max(0, option.FindPropertyRelative("weight").intValue);
            if (weight <= 0)
                continue;

            int expectedCount = Mathf.RoundToInt(count * (weight / (float)totalWeight));
            AddEnemyRefToSummary(option.FindPropertyRelative("enemy"), expectedCount, summary);
        }
    }

    private void AddEnemyRefToSummary(SerializedProperty enemyRef, int count, WaveSummary summary)
    {
        if (count <= 0)
            return;

        EnemyCatalog.Entry entry = FindCatalogEntry(
            enemyRef.FindPropertyRelative("enemyId").stringValue,
            enemyRef.FindPropertyRelative("prefab").objectReferenceValue as EnemyAgent);

        EnemyAgent prefab = entry != null && entry.prefab != null
            ? entry.prefab
            : enemyRef.FindPropertyRelative("prefab").objectReferenceValue as EnemyAgent;

        if (prefab == null)
            return;

        string enemyName = entry != null ? entry.DisplayName : prefab.name;
        string category = entry != null ? NormalizeCategory(entry.category) : UncategorizedCategory;

        summary.enemyCount += count;
        summary.killMoney += GetEnemyRewardMoney(prefab) * count;
        AddCount(summary.enemyCounts, enemyName, count);
        AddCount(summary.categoryCounts, category, count);
    }

    private int GetEnemyRewardMoney(EnemyAgent enemy)
    {
        if (enemy == null)
            return 0;

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health == null)
            health = enemy.GetComponentInChildren<EnemyHealth>();

        if (health == null)
            return 0;

        SerializedObject healthObject = new SerializedObject(health);
        SerializedProperty rewardMoney = healthObject.FindProperty("rewardMoney");
        return rewardMoney != null ? Mathf.Max(0, rewardMoney.intValue) : 0;
    }

    private void AddCount(Dictionary<string, int> counts, string key, int amount)
    {
        if (counts.ContainsKey(key))
            counts[key] += amount;
        else
            counts.Add(key, amount);
    }

    private void DrawEnemyRefList(SerializedProperty list)
    {
        EditorGUILayout.LabelField("Enemy Pattern");
        EditorGUI.indentLevel++;

        for (int i = 0; i < list.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            DrawEnemyRef(list.GetArrayElementAtIndex(i), $"#{i + 1}");
            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                list.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Enemy To Pattern"))
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            ClearEnemyRef(list.GetArrayElementAtIndex(list.arraySize - 1));
        }

        EditorGUI.indentLevel--;
    }

    private void DrawRandomPool(SerializedProperty randomPool)
    {
        EditorGUILayout.LabelField("Random Pool");
        EditorGUI.indentLevel++;

        for (int i = 0; i < randomPool.arraySize; i++)
        {
            SerializedProperty option = randomPool.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginHorizontal();
            DrawEnemyRef(option.FindPropertyRelative("enemy"), $"Option {i + 1}");
            EditorGUILayout.PropertyField(option.FindPropertyRelative("weight"), GUIContent.none, GUILayout.Width(60f));
            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                randomPool.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+ Add Random Option"))
        {
            randomPool.InsertArrayElementAtIndex(randomPool.arraySize);
            ClearWeightedEnemyRef(randomPool.GetArrayElementAtIndex(randomPool.arraySize - 1));
        }

        EditorGUI.indentLevel--;
    }

    private void DrawEnemyRef(SerializedProperty enemyRef, string label)
    {
        SerializedProperty enemyId = enemyRef.FindPropertyRelative("enemyId");
        SerializedProperty prefab = enemyRef.FindPropertyRelative("prefab");

        if (enemyCatalog != null && enemyCatalog.Entries.Count > 0)
        {
            EnemyCatalog.Entry currentEntry = FindCatalogEntry(enemyId.stringValue, prefab.objectReferenceValue as EnemyAgent);
            string key = GetEnemyRefKey(enemyRef);
            string currentCategory = currentEntry != null
                ? NormalizeCategory(currentEntry.category)
                : GetStoredCategory(key);

            List<string> categories = BuildCategoryList();
            int categoryIndex = Mathf.Max(0, categories.IndexOf(currentCategory));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(82f));

            EditorGUI.BeginChangeCheck();
            int selectedCategoryIndex = EditorGUILayout.Popup(categoryIndex, categories.ToArray(), GUILayout.MinWidth(110f));
            if (EditorGUI.EndChangeCheck())
            {
                currentCategory = categories[Mathf.Clamp(selectedCategoryIndex, 0, categories.Count - 1)];
                enemyPickerCategories[key] = currentCategory;
                ClearEnemyRef(enemyRef);
                currentEntry = null;
            }

            List<EnemyCatalog.Entry> entries = BuildEntriesForCategory(currentCategory);
            string[] enemyNames = BuildEnemyNames(entries);
            int enemyIndex = FindEntryIndex(entries, currentEntry);
            int selectedEnemyIndex = EditorGUILayout.Popup(enemyIndex, enemyNames, GUILayout.MinWidth(130f));

            if (selectedEnemyIndex <= 0)
            {
                enemyId.stringValue = "";
                prefab.objectReferenceValue = null;
            }
            else if (selectedEnemyIndex - 1 < entries.Count)
            {
                EnemyCatalog.Entry entry = entries[selectedEnemyIndex - 1];
                if (entry != null)
                {
                    enemyId.stringValue = entry.id;
                    prefab.objectReferenceValue = entry.prefab;
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.PropertyField(prefab, new GUIContent(label));
        }
    }

    private void DrawEnemyCatalogPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(RightPanelWidth));
        EditorGUILayout.LabelField("Enemy Catalog", EditorStyles.boldLabel);

        if (enemyCatalog == null)
        {
            EditorGUILayout.HelpBox("Assign or create an EnemyCatalog. It gives the wave editor dropdowns instead of raw prefab hunting.", MessageType.Info);
            if (GUILayout.Button("Create Catalog"))
                CreateEnemyCatalogAsset();

            EditorGUILayout.EndVertical();
            return;
        }

        SerializedProperty entries = catalogObject.FindProperty("entries");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Entry"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            ClearCatalogEntry(entries.GetArrayElementAtIndex(entries.arraySize - 1));
        }

        if (GUILayout.Button("Add Selected Prefabs"))
            AddSelectedEnemyPrefabs(entries);

        if (GUILayout.Button("Scan Enemy Folder"))
            AddEnemiesFromProjectFolder(entries);

        EditorGUILayout.EndHorizontal();

        catalogScroll = EditorGUILayout.BeginScrollView(catalogScroll);

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Entry {i + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("X", GUILayout.Width(28f)))
            {
                entries.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(entry.FindPropertyRelative("id"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"), new GUIContent("Display Name (Optional)"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("prefab"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("category"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("threatValue"));
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private string GetStepSummary(SerializedProperty step, int index)
    {
        WaveSet.WaveStepType type = (WaveSet.WaveStepType)step.FindPropertyRelative("type").enumValueIndex;
        string label = step.FindPropertyRelative("label").stringValue;
        string prefix = string.IsNullOrWhiteSpace(label) ? $"Step {index + 1}" : $"Step {index + 1} - {label}";

        if (type == WaveSet.WaveStepType.Delay)
            return $"{prefix}: Wait {step.FindPropertyRelative("delayDuration").floatValue:0.##}s";

        if (type == WaveSet.WaveStepType.Sequence)
        {
            int repeats = step.FindPropertyRelative("repeatCount").intValue;
            int enemies = step.FindPropertyRelative("enemies").arraySize;
            return $"{prefix}: Pattern Repeat | {repeats * enemies} spawns";
        }

        int count = step.FindPropertyRelative("count").intValue;
        return $"{prefix}: {GetStepTypeLabel(type)} | {count} spawns";
    }

    private void AddWave(SerializedProperty waves)
    {
        Undo.RecordObject(waveSet, "Add Wave");
        waves.InsertArrayElementAtIndex(waves.arraySize);

        SerializedProperty wave = waves.GetArrayElementAtIndex(waves.arraySize - 1);
        wave.FindPropertyRelative("editorLabel").stringValue = "";
        wave.FindPropertyRelative("completionReward").intValue = waveSet.DefaultCompletionReward;
        wave.FindPropertyRelative("steps").ClearArray();

        selectedWaveIndex = waves.arraySize - 1;
    }

    private void DuplicateWave(SerializedProperty waves)
    {
        Undo.RecordObject(waveSet, "Duplicate Wave");
        waves.InsertArrayElementAtIndex(selectedWaveIndex);
        selectedWaveIndex++;
    }

    private void DeleteWave(SerializedProperty waves)
    {
        if (!EditorUtility.DisplayDialog("Delete Wave", "Delete the selected wave?", "Delete", "Cancel"))
            return;

        Undo.RecordObject(waveSet, "Delete Wave");
        waves.DeleteArrayElementAtIndex(selectedWaveIndex);
        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waves.arraySize - 1));
    }

    private void MoveWave(SerializedProperty waves, int direction)
    {
        int newIndex = Mathf.Clamp(selectedWaveIndex + direction, 0, waves.arraySize - 1);
        Undo.RecordObject(waveSet, "Move Wave");
        waves.MoveArrayElement(selectedWaveIndex, newIndex);
        selectedWaveIndex = newIndex;
    }

    private void DuplicateStep(SerializedProperty steps, int index)
    {
        Undo.RecordObject(waveSet, "Duplicate Wave Step");
        steps.InsertArrayElementAtIndex(index);
        steps.MoveArrayElement(index, index + 1);
        SerializedProperty duplicatedStep = steps.GetArrayElementAtIndex(index + 1);
        duplicatedStep.FindPropertyRelative("editorCollapsed").boolValue = false;
    }

    private void AddStep(SerializedProperty steps, WaveSet.WaveStepType type)
    {
        Undo.RecordObject(waveSet, "Add Wave Step");
        steps.InsertArrayElementAtIndex(steps.arraySize);

        SerializedProperty step = steps.GetArrayElementAtIndex(steps.arraySize - 1);
        step.FindPropertyRelative("label").stringValue = "";
        step.FindPropertyRelative("editorCollapsed").boolValue = false;
        step.FindPropertyRelative("type").enumValueIndex = (int)type;
        ClearEnemyRef(step.FindPropertyRelative("enemy"));
        step.FindPropertyRelative("enemies").ClearArray();
        step.FindPropertyRelative("randomPool").ClearArray();
        step.FindPropertyRelative("count").intValue = 10;
        step.FindPropertyRelative("repeatCount").intValue = 1;
        step.FindPropertyRelative("spawnInterval").floatValue = waveSet.DefaultSpawnInterval;
        step.FindPropertyRelative("delayBeforeStep").floatValue = 0f;
        step.FindPropertyRelative("delayAfterStep").floatValue = 0f;
        step.FindPropertyRelative("delayDuration").floatValue = 1f;
    }

    private void SetWaveSet(WaveSet newWaveSet)
    {
        waveSet = newWaveSet;
        waveSetObject = waveSet != null ? new SerializedObject(waveSet) : null;
        selectedWaveIndex = 0;

        if (waveSet != null && waveSet.EnemyCatalog != null)
        {
            enemyCatalog = waveSet.EnemyCatalog;
            catalogObject = new SerializedObject(enemyCatalog);
        }
    }

    private void EnsureSerializedObjects()
    {
        if (waveSet != null && waveSetObject == null)
            waveSetObject = new SerializedObject(waveSet);

        if (enemyCatalog != null && catalogObject == null)
            catalogObject = new SerializedObject(enemyCatalog);
    }

    private void QueueAssetSave()
    {
        if (saveQueued)
            return;

        saveQueued = true;
        EditorApplication.delayCall += () =>
        {
            saveQueued = false;
            AssetDatabase.SaveAssets();
        };
    }

    private void AssignCatalogToWaveSetIfNeeded()
    {
        if (waveSet == null || enemyCatalog == null)
            return;

        EnsureSerializedObjects();
        waveSetObject.Update();
        waveSetObject.FindProperty("enemyCatalog").objectReferenceValue = enemyCatalog;
        waveSetObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(waveSet);
    }

    private List<string> BuildCategoryList()
    {
        List<string> categories = new List<string>();

        for (int i = 0; i < enemyCatalog.Entries.Count; i++)
        {
            EnemyCatalog.Entry entry = enemyCatalog.GetEntry(i);
            if (entry == null)
                continue;

            string category = NormalizeCategory(entry.category);
            if (!categories.Contains(category))
                categories.Add(category);
        }

        if (categories.Count == 0)
            categories.Add(UncategorizedCategory);

        categories.Sort(StringComparer.OrdinalIgnoreCase);
        return categories;
    }

    private List<EnemyCatalog.Entry> BuildEntriesForCategory(string category)
    {
        string normalizedCategory = NormalizeCategory(category);
        List<EnemyCatalog.Entry> entries = new List<EnemyCatalog.Entry>();

        for (int i = 0; i < enemyCatalog.Entries.Count; i++)
        {
            EnemyCatalog.Entry entry = enemyCatalog.GetEntry(i);
            if (entry == null)
                continue;

            if (NormalizeCategory(entry.category) == normalizedCategory)
                entries.Add(entry);
        }

        entries.Sort(CompareCatalogEntriesForPicker);
        return entries;
    }

    private string[] BuildEnemyNames(List<EnemyCatalog.Entry> entries)
    {
        string[] names = new string[entries.Count + 1];
        names[0] = "None";

        for (int i = 0; i < entries.Count; i++)
        {
            EnemyCatalog.Entry entry = entries[i];
            names[i + 1] = $"{entry.DisplayName} (Threat {entry.threatValue:0.##})";
        }

        return names;
    }

    private int FindEntryIndex(List<EnemyCatalog.Entry> entries, EnemyCatalog.Entry currentEntry)
    {
        if (currentEntry == null)
            return 0;

        int index = entries.IndexOf(currentEntry);
        return index >= 0 ? index + 1 : 0;
    }

    private EnemyCatalog.Entry FindCatalogEntry(string enemyId, EnemyAgent prefab)
    {
        if (enemyCatalog == null)
            return null;

        if (!string.IsNullOrWhiteSpace(enemyId))
        {
            EnemyCatalog.Entry entry = enemyCatalog.FindById(enemyId);
            if (entry != null)
                return entry;
        }

        return enemyCatalog.FindByPrefab(prefab);
    }

    private string GetEnemyRefKey(SerializedProperty enemyRef)
    {
        return $"{enemyRef.serializedObject.targetObject.GetInstanceID()}:{enemyRef.propertyPath}";
    }

    private string GetStoredCategory(string key)
    {
        if (enemyPickerCategories.TryGetValue(key, out string category))
            return category;

        return BuildCategoryList()[0];
    }

    private string NormalizeCategory(string category)
    {
        return string.IsNullOrWhiteSpace(category) ? UncategorizedCategory : category.Trim();
    }

    private int CompareCatalogEntriesForPicker(EnemyCatalog.Entry left, EnemyCatalog.Entry right)
    {
        int threatComparison = left.threatValue.CompareTo(right.threatValue);
        if (threatComparison != 0)
            return threatComparison;

        return string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
    }

    private void ClearEnemyRef(SerializedProperty enemyRef)
    {
        enemyRef.FindPropertyRelative("enemyId").stringValue = "";
        enemyRef.FindPropertyRelative("prefab").objectReferenceValue = null;
    }

    private void ClearWeightedEnemyRef(SerializedProperty weightedEnemyRef)
    {
        ClearEnemyRef(weightedEnemyRef.FindPropertyRelative("enemy"));
        weightedEnemyRef.FindPropertyRelative("weight").intValue = 1;
    }

    private void ClearCatalogEntry(SerializedProperty entry)
    {
        entry.FindPropertyRelative("id").stringValue = "";
        entry.FindPropertyRelative("displayName").stringValue = "";
        entry.FindPropertyRelative("prefab").objectReferenceValue = null;
        entry.FindPropertyRelative("category").stringValue = UncategorizedCategory;
        entry.FindPropertyRelative("threatValue").floatValue = 1f;
    }

    private void CreateWaveSetAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Wave Set", "Wave Set", "asset", "Choose where to save the WaveSet.");
        if (string.IsNullOrWhiteSpace(path))
            return;

        WaveSet asset = CreateInstance<WaveSet>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        SetWaveSet(asset);
        Selection.activeObject = asset;
    }

    private void CreateEnemyCatalogAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Enemy Catalog", "Enemy Catalog", "asset", "Choose where to save the EnemyCatalog.");
        if (string.IsNullOrWhiteSpace(path))
            return;

        EnemyCatalog asset = CreateInstance<EnemyCatalog>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        enemyCatalog = asset;
        catalogObject = new SerializedObject(enemyCatalog);
        AssignCatalogToWaveSetIfNeeded();
        Selection.activeObject = asset;
    }

    private void AddSelectedEnemyPrefabs(SerializedProperty entries)
    {
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            GameObject gameObject = selected as GameObject;
            if (gameObject == null)
                continue;

            EnemyAgent enemy = gameObject.GetComponent<EnemyAgent>();
            if (enemy == null)
                enemy = gameObject.GetComponentInChildren<EnemyAgent>();

            if (enemy != null)
                AddEnemyPrefabToCatalog(entries, enemy);
        }
    }

    private void AddEnemiesFromProjectFolder(SerializedProperty entries)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Enemies" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            EnemyAgent enemy = prefab.GetComponent<EnemyAgent>();
            if (enemy == null)
                enemy = prefab.GetComponentInChildren<EnemyAgent>();

            if (enemy != null)
                AddEnemyPrefabToCatalog(entries, enemy);
        }
    }

    private void AddEnemyPrefabToCatalog(SerializedProperty entries, EnemyAgent enemy)
    {
        if (enemy == null || CatalogAlreadyContains(enemy))
            return;

        Undo.RecordObject(enemyCatalog, "Add Enemy To Catalog");
        entries.InsertArrayElementAtIndex(entries.arraySize);

        SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
        entry.FindPropertyRelative("id").stringValue = CreateId(enemy.name);
        entry.FindPropertyRelative("displayName").stringValue = enemy.name;
        entry.FindPropertyRelative("prefab").objectReferenceValue = enemy;
        entry.FindPropertyRelative("category").stringValue = UncategorizedCategory;
        entry.FindPropertyRelative("threatValue").floatValue = 1f;
    }

    private bool CatalogAlreadyContains(EnemyAgent enemy)
    {
        if (enemyCatalog == null)
            return false;

        return enemyCatalog.FindByPrefab(enemy) != null;
    }

    private string CreateId(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "enemy";

        char[] chars = source.Trim().ToLowerInvariant().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            bool valid = (chars[i] >= 'a' && chars[i] <= 'z') || (chars[i] >= '0' && chars[i] <= '9');
            if (!valid)
                chars[i] = '_';
        }

        return new string(chars);
    }

    private void ConvertSelectedSpawner()
    {
        WaveSpawner spawner = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<WaveSpawner>() : null;
        if (spawner == null)
            spawner = FindFirstObjectByType<WaveSpawner>();

        if (spawner == null)
        {
            EditorUtility.DisplayDialog("No WaveSpawner", "Select a WaveSpawner or open a scene containing one.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject("Create Wave Set From Spawner", $"{spawner.name} Wave Set", "asset", "Choose where to save the converted WaveSet.");
        if (string.IsNullOrWhiteSpace(path))
            return;

        WaveSet created = CreateInstance<WaveSet>();
        AssetDatabase.CreateAsset(created, path);

        SerializedObject source = new SerializedObject(spawner);
        SerializedObject target = new SerializedObject(created);
        SerializedProperty sourceWaves = source.FindProperty("waves");
        SerializedProperty targetWaves = target.FindProperty("waves");

        targetWaves.ClearArray();

        for (int w = 0; w < sourceWaves.arraySize; w++)
        {
            SerializedProperty oldWave = sourceWaves.GetArrayElementAtIndex(w);
            targetWaves.InsertArrayElementAtIndex(targetWaves.arraySize);

            SerializedProperty newWave = targetWaves.GetArrayElementAtIndex(targetWaves.arraySize - 1);
            newWave.FindPropertyRelative("editorLabel").stringValue = oldWave.FindPropertyRelative("name").stringValue;
            newWave.FindPropertyRelative("completionReward").intValue = oldWave.FindPropertyRelative("completionReward").intValue;

            SerializedProperty oldGroups = oldWave.FindPropertyRelative("groups");
            SerializedProperty newSteps = newWave.FindPropertyRelative("steps");
            newSteps.ClearArray();

            for (int g = 0; g < oldGroups.arraySize; g++)
            {
                SerializedProperty oldGroup = oldGroups.GetArrayElementAtIndex(g);
                newSteps.InsertArrayElementAtIndex(newSteps.arraySize);

                SerializedProperty newStep = newSteps.GetArrayElementAtIndex(newSteps.arraySize - 1);
                EnemyAgent enemy = oldGroup.FindPropertyRelative("enemyPrefab").objectReferenceValue as EnemyAgent;

                newStep.FindPropertyRelative("label").stringValue = enemy != null ? enemy.name : "";
                newStep.FindPropertyRelative("type").enumValueIndex = (int)WaveSet.WaveStepType.SingleGroup;
                newStep.FindPropertyRelative("count").intValue = oldGroup.FindPropertyRelative("count").intValue;
                newStep.FindPropertyRelative("repeatCount").intValue = 1;
                newStep.FindPropertyRelative("spawnInterval").floatValue = oldGroup.FindPropertyRelative("spawnInterval").floatValue;
                newStep.FindPropertyRelative("delayBeforeStep").floatValue = 0f;
                newStep.FindPropertyRelative("delayAfterStep").floatValue = oldGroup.FindPropertyRelative("delayAfterGroup").floatValue;
                newStep.FindPropertyRelative("delayDuration").floatValue = 1f;

                SerializedProperty enemyRef = newStep.FindPropertyRelative("enemy");
                enemyRef.FindPropertyRelative("prefab").objectReferenceValue = enemy;
                if (enemyCatalog != null)
                {
                    EnemyCatalog.Entry entry = enemyCatalog.FindByPrefab(enemy);
                    enemyRef.FindPropertyRelative("enemyId").stringValue = entry != null ? entry.id : "";
                }
            }
        }

        if (enemyCatalog != null)
            target.FindProperty("enemyCatalog").objectReferenceValue = enemyCatalog;

        target.ApplyModifiedProperties();
        EditorUtility.SetDirty(created);
        AssetDatabase.SaveAssets();

        SetWaveSet(created);
        Selection.activeObject = created;
    }

    private void ValidateWaveSet()
    {
        List<string> problems = new List<string>();
        EnemyCatalog catalog = waveSet.EnemyCatalog != null ? waveSet.EnemyCatalog : enemyCatalog;

        ValidateCatalog(catalog, problems);

        for (int w = 0; w < waveSet.WaveCount; w++)
        {
            WaveSet.WaveDefinition wave = waveSet.GetWave(w);
            if (wave == null || wave.steps == null || wave.steps.Count == 0)
            {
                problems.Add(waveSet.GetWaveDisplayName(w) + ": no steps.");
                continue;
            }

            bool hasSpawningStep = false;
            for (int s = 0; s < wave.steps.Count; s++)
            {
                WaveSet.WaveStep step = wave.steps[s];
                if (step == null)
                {
                    problems.Add($"{waveSet.GetWaveDisplayName(w)}, step {s + 1}: missing step data.");
                    continue;
                }

                if (step.type == WaveSet.WaveStepType.Delay)
                {
                    if (step.delayDuration <= 0f)
                        problems.Add($"{waveSet.GetWaveDisplayName(w)}, step {s + 1}: wait step has no wait time.");

                    continue;
                }

                hasSpawningStep = true;

                if (waveSet.CountEstimatedEnemies(step) <= 0)
                    problems.Add($"{waveSet.GetWaveDisplayName(w)}, step {s + 1}: no enemies scheduled.");

                if (!StepHasUsableEnemy(step))
                    problems.Add($"{waveSet.GetWaveDisplayName(w)}, step {s + 1}: no usable enemy assigned.");

                if (step.type == WaveSet.WaveStepType.RandomPool)
                    ValidateRandomPoolStep(w, s, step, problems);
            }

            if (!hasSpawningStep)
                problems.Add($"{waveSet.GetWaveDisplayName(w)}: wave has only wait steps.");
        }

        if (problems.Count == 0)
        {
            string message = $"{waveSet.name}: validation passed. {waveSet.WaveCount} waves checked; no issues found.";
            Debug.Log("[Wave Editor] " + message, waveSet);
            EditorUtility.DisplayDialog("Wave Set Valid", message, "OK");
            return;
        }

        string report = $"{waveSet.name}: validation found {problems.Count} issue(s).\n- " + string.Join("\n- ", problems);
        Debug.LogWarning("[Wave Editor] " + report, waveSet);
        EditorUtility.DisplayDialog("Wave Set Issues", report, "OK");
    }

    private void ValidateCatalog(EnemyCatalog catalog, List<string> problems)
    {
        if (catalog == null)
        {
            problems.Add("No EnemyCatalog assigned.");
            return;
        }

        HashSet<string> ids = new HashSet<string>();
        for (int i = 0; i < catalog.Entries.Count; i++)
        {
            EnemyCatalog.Entry entry = catalog.GetEntry(i);
            if (entry == null)
            {
                problems.Add($"EnemyCatalog entry {i + 1}: missing entry data.");
                continue;
            }

            if (entry.prefab == null)
                problems.Add($"EnemyCatalog entry {i + 1} ({entry.DisplayName}): no prefab assigned.");

            if (string.IsNullOrWhiteSpace(entry.id))
            {
                problems.Add($"EnemyCatalog entry {i + 1} ({entry.DisplayName}): no id assigned.");
            }
            else if (!ids.Add(entry.id))
            {
                problems.Add($"EnemyCatalog entry {i + 1} ({entry.DisplayName}): duplicate id '{entry.id}'.");
            }

            if (string.IsNullOrWhiteSpace(entry.category))
                problems.Add($"EnemyCatalog entry {i + 1} ({entry.DisplayName}): no category assigned.");
        }
    }

    private void ValidateRandomPoolStep(int waveIndex, int stepIndex, WaveSet.WaveStep step, List<string> problems)
    {
        int positiveWeightCount = 0;
        if (step.randomPool != null)
        {
            for (int i = 0; i < step.randomPool.Count; i++)
            {
                WaveSet.WeightedEnemyRef option = step.randomPool[i];
                if (option != null && option.weight > 0)
                    positiveWeightCount++;
            }
        }

        if (positiveWeightCount == 0)
            problems.Add($"{waveSet.GetWaveDisplayName(waveIndex)}, step {stepIndex + 1}: random pool has no positive weights.");
    }

    private bool StepHasUsableEnemy(WaveSet.WaveStep step)
    {
        EnemyCatalog catalog = waveSet.EnemyCatalog != null ? waveSet.EnemyCatalog : enemyCatalog;

        switch (step.type)
        {
            case WaveSet.WaveStepType.SingleGroup:
            case WaveSet.WaveStepType.Burst:
                return EnemyRefResolves(step.enemy, catalog);

            case WaveSet.WaveStepType.Alternating:
            case WaveSet.WaveStepType.Sequence:
                if (step.enemies == null || step.enemies.Count == 0)
                    return false;

                for (int i = 0; i < step.enemies.Count; i++)
                {
                    if (EnemyRefResolves(step.enemies[i], catalog))
                        return true;
                }

                return false;

            case WaveSet.WaveStepType.RandomPool:
                if (step.randomPool == null || step.randomPool.Count == 0)
                    return false;

                for (int i = 0; i < step.randomPool.Count; i++)
                {
                    WaveSet.WeightedEnemyRef option = step.randomPool[i];
                    if (option != null && option.weight > 0 && EnemyRefResolves(option.enemy, catalog))
                        return true;
                }

                return false;

            default:
                return true;
        }
    }

    private bool EnemyRefResolves(WaveSet.EnemyRef enemyRef, EnemyCatalog catalog)
    {
        return enemyRef != null && enemyRef.Resolve(catalog) != null;
    }
}

public class WaveSearchWindow : EditorWindow
{
    private WaveEditorWindow owner;
    private string textSearch = "";
    private string enemySearch = "";
    private string categorySearch = "";
    private Vector2 scroll;

    private class SearchResult
    {
        public int waveIndex;
        public string title;
        public string notes;
        public string enemyMix;
        public string categoryMix;
    }

    public static void Open(WaveEditorWindow owner)
    {
        WaveSearchWindow window = GetWindow<WaveSearchWindow>("Wave Search");
        window.owner = owner;
        window.Show();
    }

    private void OnGUI()
    {
        if (owner == null || owner.CurrentWaveSet == null)
        {
            EditorGUILayout.HelpBox("Open a WaveSet in the Wave Editor first.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField(owner.CurrentWaveSet.name, EditorStyles.boldLabel);
        textSearch = EditorGUILayout.TextField(new GUIContent("Label / Notes"), textSearch);
        enemySearch = EditorGUILayout.TextField(new GUIContent("Enemies"), enemySearch);
        categorySearch = EditorGUILayout.TextField(new GUIContent("Categories"), categorySearch);
        EditorGUILayout.HelpBox("Separate multiple enemies or categories with commas. Results must match every term in each filled search field.", MessageType.None);

        List<SearchResult> results = BuildResults();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField($"{results.Count} matching wave(s)", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < results.Count; i++)
            DrawResult(results[i]);

        EditorGUILayout.EndScrollView();
    }

    private void DrawResult(SearchResult result)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(result.title, EditorStyles.boldLabel);
        if (GUILayout.Button("Select", GUILayout.Width(70f)))
        {
            owner.SelectWave(result.waveIndex);
            owner.Focus();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrWhiteSpace(result.notes))
            EditorGUILayout.LabelField("Notes", result.notes);

        EditorGUILayout.LabelField("Enemies", result.enemyMix);
        EditorGUILayout.LabelField("Categories", result.categoryMix);
        EditorGUILayout.EndVertical();
    }

    private List<SearchResult> BuildResults()
    {
        List<SearchResult> results = new List<SearchResult>();
        WaveSet waveSet = owner.CurrentWaveSet;
        EnemyCatalog catalog = owner.CurrentEnemyCatalog != null ? owner.CurrentEnemyCatalog : waveSet.EnemyCatalog;

        string[] textTerms = ParseTerms(textSearch);
        string[] enemyTerms = ParseTerms(enemySearch);
        string[] categoryTerms = ParseTerms(categorySearch);

        for (int i = 0; i < waveSet.WaveCount; i++)
        {
            WaveSet.WaveDefinition wave = waveSet.GetWave(i);
            if (wave == null)
                continue;

            List<string> enemies = new List<string>();
            List<string> categories = new List<string>();
            CollectWaveSearchData(wave, catalog, enemies, categories);

            string searchableText = $"{waveSet.GetWaveDisplayName(i)} {wave.notes}";
            if (!MatchesAllTerms(searchableText, textTerms))
                continue;

            if (!MatchesAnyItemForEveryTerm(enemies, enemyTerms))
                continue;

            if (!MatchesAnyItemForEveryTerm(categories, categoryTerms))
                continue;

            results.Add(new SearchResult
            {
                waveIndex = i,
                title = waveSet.GetWaveDisplayName(i),
                notes = TrimSnippet(wave.notes),
                enemyMix = FormatUniqueItems(enemies),
                categoryMix = FormatUniqueItems(categories)
            });
        }

        return results;
    }

    private void CollectWaveSearchData(WaveSet.WaveDefinition wave, EnemyCatalog catalog, List<string> enemies, List<string> categories)
    {
        if (wave.steps == null)
            return;

        for (int i = 0; i < wave.steps.Count; i++)
        {
            WaveSet.WaveStep step = wave.steps[i];
            if (step == null || step.type == WaveSet.WaveStepType.Delay)
                continue;

            if (step.type == WaveSet.WaveStepType.SingleGroup || step.type == WaveSet.WaveStepType.Burst)
            {
                AddEnemyRefSearchData(step.enemy, catalog, enemies, categories);
            }
            else if (step.type == WaveSet.WaveStepType.Alternating || step.type == WaveSet.WaveStepType.Sequence)
            {
                if (step.enemies == null)
                    continue;

                for (int e = 0; e < step.enemies.Count; e++)
                    AddEnemyRefSearchData(step.enemies[e], catalog, enemies, categories);
            }
            else if (step.type == WaveSet.WaveStepType.RandomPool)
            {
                if (step.randomPool == null)
                    continue;

                for (int r = 0; r < step.randomPool.Count; r++)
                {
                    WaveSet.WeightedEnemyRef option = step.randomPool[r];
                    if (option != null)
                        AddEnemyRefSearchData(option.enemy, catalog, enemies, categories);
                }
            }
        }
    }

    private void AddEnemyRefSearchData(WaveSet.EnemyRef enemyRef, EnemyCatalog catalog, List<string> enemies, List<string> categories)
    {
        if (enemyRef == null)
            return;

        EnemyCatalog.Entry entry = catalog != null ? catalog.FindById(enemyRef.enemyId) : null;
        if (entry == null && catalog != null)
            entry = catalog.FindByPrefab(enemyRef.prefab);

        EnemyAgent prefab = entry != null && entry.prefab != null ? entry.prefab : enemyRef.prefab;
        if (entry != null)
        {
            enemies.Add(entry.DisplayName);
            categories.Add(NormalizeSearchCategory(entry.category));
            return;
        }

        if (prefab != null)
        {
            enemies.Add(prefab.name);
            categories.Add(NormalizeSearchCategory(""));
        }
    }

    private string[] ParseTerms(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<string>();

        string[] rawTerms = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        List<string> terms = new List<string>();
        for (int i = 0; i < rawTerms.Length; i++)
        {
            string term = rawTerms[i].Trim();
            if (!string.IsNullOrWhiteSpace(term))
                terms.Add(term);
        }

        return terms.ToArray();
    }

    private bool MatchesAllTerms(string source, string[] terms)
    {
        for (int i = 0; i < terms.Length; i++)
        {
            if (source == null || source.IndexOf(terms[i], StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        return true;
    }

    private bool MatchesAnyItemForEveryTerm(List<string> items, string[] terms)
    {
        for (int t = 0; t < terms.Length; t++)
        {
            bool found = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].IndexOf(terms[t], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                return false;
        }

        return true;
    }

    private string FormatUniqueItems(List<string> items)
    {
        List<string> unique = new List<string>();
        for (int i = 0; i < items.Count; i++)
        {
            string item = items[i].Trim();
            if (!string.IsNullOrWhiteSpace(item) && !unique.Contains(item))
                unique.Add(item);
        }

        if (unique.Count == 0)
            return "None";

        unique.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", unique);
    }

    private string TrimSnippet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string trimmed = text.Trim().Replace("\n", " ");
        return trimmed.Length <= 140 ? trimmed : trimmed.Substring(0, 140) + "...";
    }

    private string NormalizeSearchCategory(string category)
    {
        return string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category.Trim();
    }
}

