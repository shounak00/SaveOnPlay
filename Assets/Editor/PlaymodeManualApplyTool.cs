// Assets/Editor/PlaymodeManualApplyTool.cs
// Records ONLY manual playmode edits (Undo-driven) + manually added components,
// clears the list at the start of each Play session,
// and applies changes back to Edit Mode.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PlaymodeManualApplyTool : EditorWindow
{
    [Serializable]
    private class ComponentSnapshot
    {
        public string componentType;   // AssemblyQualifiedName
        public string json;            // EditorJsonUtility snapshot
        public bool addedInPlayMode;   // true if created via Add Component in Play
    }

    [Serializable]
    private class ObjectRecord
    {
        public string globalId; // GlobalObjectId.ToString()
        public string objectName;
        public List<ComponentSnapshot> components = new List<ComponentSnapshot>();
        public long lastTouchedTicks;
    }

    [Serializable]
    private class RecordStore
    {
        public bool recordingEnabled = true;
        public List<ObjectRecord> records = new List<ObjectRecord>();

        // NEW: reset list automatically at each Play session start (default true)
        public bool resetOnPlay = true;
    }

    private const string SessionKey = "PlaymodeManualApplyTool_RecordStore_JSON";

    private Vector2 _scroll;
    private RecordStore _store;

    [MenuItem("Tools/Playmode Manual Apply")]
    public static void Open() => GetWindow<PlaymodeManualApplyTool>("Playmode Manual Apply");

    private void OnEnable()
    {
        LoadStore();
        RecorderHooks.EnsureInstalled();

        RecorderHooks.OnRecordChanged -= OnExternalRecordChanged;
        RecorderHooks.OnRecordChanged += OnExternalRecordChanged;

        EditorApplication.update -= Repaint;
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        RecorderHooks.OnRecordChanged -= OnExternalRecordChanged;
        EditorApplication.update -= Repaint;
        SaveStore();
    }

    private void OnExternalRecordChanged()
    {
        _store = null;
        LoadStore();
        Repaint();
    }

    private void OnGUI()
    {
        LoadStore();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Playmode Manual Apply", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "Records ONLY manual edits made in Play Mode that go through Undo (Inspector edits, gizmo moves) " +
            "and components added via the Add Component button.\n\n" +
            "It intentionally does NOT detect automatic/script-driven movement (unless a script uses Undo).",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();
        _store.recordingEnabled = EditorGUILayout.ToggleLeft("Enable recording while in Play Mode", _store.recordingEnabled);
        if (EditorGUI.EndChangeCheck())
        {
            SaveStore();
            RecorderHooks.NotifyChanged();
        }

        EditorGUI.BeginChangeCheck();
        _store.resetOnPlay = EditorGUILayout.ToggleLeft("Reset list at the start of each Play session", _store.resetOnPlay);
        if (EditorGUI.EndChangeCheck())
        {
            SaveStore();
            RecorderHooks.NotifyChanged();
        }

        EditorGUILayout.Space(8);

        // === TOP CONTROLS ===
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = !EditorApplication.isPlaying && (_store.records?.Count > 0);

            if (GUILayout.Button("Apply All", GUILayout.Height(30)))
                ApplyAll();

            if (GUILayout.Button("Discard All", GUILayout.Height(30)))
                DiscardAll();

            GUI.enabled = true;
        }

        EditorGUILayout.Space(10);
        DrawRecords();
    }

    private void DrawRecords()
    {
        if (_store.records == null || _store.records.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No recorded manual changes.\n\n" +
                "Steps:\n" +
                "1) Enter Play Mode\n" +
                "2) Move objects / tweak colliders / change script fields / Add Component\n" +
                "3) Exit Play Mode\n" +
                "4) Apply or Discard from here",
                MessageType.None);
            return;
        }

        var ordered = _store.records
            .OrderByDescending(r => r.lastTouchedTicks)
            .ToList();

        EditorGUILayout.LabelField($"Touched Objects: {ordered.Count}", EditorStyles.boldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var rec in ordered.ToList())
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField(rec.objectName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Components captured: {rec.components?.Count ?? 0}", EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !EditorApplication.isPlaying;

                    if (GUILayout.Button("Apply", GUILayout.Width(90)))
                        ApplyRecord(rec);

                    if (GUILayout.Button("Discard", GUILayout.Width(90)))
                        DiscardRecord(rec.globalId);

                    GUI.enabled = true;

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Ping", GUILayout.Width(90)))
                        PingInEditMode(rec);
                }

                if (rec.components != null && rec.components.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    foreach (var c in rec.components)
                    {
                        var tag = c.addedInPlayMode ? " (added)" : "";
                        EditorGUILayout.LabelField("• " + SimplifyTypeName(c.componentType) + tag, EditorStyles.miniLabel);
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DiscardAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Discard blocked", "Exit Play Mode before discarding records.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Discard all?", "This will remove all recorded playmode changes.", "Discard All", "Cancel"))
            return;

        _store.records.Clear();
        SaveStore();
        RecorderHooks.NotifyChanged();
    }

    private void DiscardRecord(string globalId)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Discard blocked", "Exit Play Mode before discarding records.", "OK");
            return;
        }

        _store.records.RemoveAll(r => r.globalId == globalId);
        SaveStore();
        RecorderHooks.NotifyChanged();
    }

    private void PingInEditMode(ObjectRecord rec)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Ping unavailable", "Exit Play Mode to ping the Edit Mode object.", "OK");
            return;
        }

        if (!TryResolveEditObject(rec.globalId, out var obj))
        {
            EditorUtility.DisplayDialog("Not found", "Could not find the target object in Edit Mode (deleted / moved scenes / prefab changes).", "OK");
            return;
        }

        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
    }

    private void ApplyAll()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Apply blocked", "Exit Play Mode before applying changes.", "OK");
            return;
        }

        int applied = 0;
        foreach (var rec in _store.records.ToList())
        {
            if (ApplyRecord(rec, silent: true))
                applied++;
        }

        EditorUtility.DisplayDialog("Apply Complete", $"Applied changes to {applied} object(s).", "OK");
        SaveStore();
        RecorderHooks.NotifyChanged();
    }

    private void ApplyRecord(ObjectRecord rec) => ApplyRecord(rec, silent: false);

    private bool ApplyRecord(ObjectRecord rec, bool silent)
    {
        if (EditorApplication.isPlaying)
        {
            if (!silent) EditorUtility.DisplayDialog("Apply blocked", "Exit Play Mode before applying changes.", "OK");
            return false;
        }

        if (!TryResolveEditObject(rec.globalId, out var targetObj))
        {
            if (!silent) EditorUtility.DisplayDialog("Not found", "Could not find the target object in Edit Mode.", "OK");
            return false;
        }

        var go = GetOwningGameObject(targetObj);
        if (go == null)
        {
            if (!silent) EditorUtility.DisplayDialog("Invalid", "Resolved object is not a scene GameObject/component.", "OK");
            return false;
        }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        bool any = false;

        foreach (var snap in rec.components)
        {
            var type = Type.GetType(snap.componentType);
            if (type == null) continue;

            Component component = go.GetComponent(type);
            if (component == null)
            {
                component = Undo.AddComponent(go, type);
            }

            if (component == null) continue;

            Undo.RecordObject(component, "Apply Playmode Manual Changes");
            EditorJsonUtility.FromJsonOverwrite(snap.json, component);
            EditorUtility.SetDirty(component);
            any = true;
        }

        if (any)
        {
            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(go.scene);
            Undo.CollapseUndoOperations(group);

            if (!silent)
                EditorUtility.DisplayDialog("Applied", $"Applied recorded changes to:\n{go.name}", "OK");
        }
        else
        {
            if (!silent)
                EditorUtility.DisplayDialog("Nothing applied", "No valid snapshots were applied (types missing/unresolved).", "OK");
        }

        return any;
    }

    private static bool TryResolveEditObject(string globalIdStr, out UnityEngine.Object obj)
    {
        obj = null;
        if (string.IsNullOrEmpty(globalIdStr)) return false;

        if (!GlobalObjectId.TryParse(globalIdStr, out var gid)) return false;

        obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
        return obj != null;
    }

    private static GameObject GetOwningGameObject(UnityEngine.Object obj)
    {
        if (obj is GameObject go) return go;
        if (obj is Component c) return c.gameObject;
        return null;
    }

    private static string SimplifyTypeName(string assemblyQualified)
    {
        if (string.IsNullOrEmpty(assemblyQualified)) return "(null)";
        try
        {
            var t = Type.GetType(assemblyQualified);
            return t != null ? t.Name : assemblyQualified.Split(',')[0];
        }
        catch
        {
            return assemblyQualified.Split(',')[0];
        }
    }

    private void LoadStore()
    {
        if (_store != null) return;

        string json = SessionState.GetString(SessionKey, "");
        if (string.IsNullOrEmpty(json))
        {
            _store = new RecordStore();
            return;
        }

        try
        {
            _store = JsonUtility.FromJson<RecordStore>(json) ?? new RecordStore();
        }
        catch
        {
            _store = new RecordStore();
        }
    }

    private void SaveStore()
    {
        if (_store == null) return;
        SessionState.SetString(SessionKey, JsonUtility.ToJson(_store));
    }

    // ===== Recorder (static) =====
    [InitializeOnLoad]
    private static class RecorderHooks
    {
        public static event Action OnRecordChanged;
        private const string SessionKeyStatic = SessionKey;

        static RecorderHooks()
        {
            EnsureInstalled();
        }

        public static void EnsureInstalled()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;

#if UNITY_2020_1_OR_NEWER
            UnityEditor.ObjectFactory.componentWasAdded -= OnComponentWasAdded;
            UnityEditor.ObjectFactory.componentWasAdded += OnComponentWasAdded;
#endif

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void NotifyChanged() => OnRecordChanged?.Invoke();

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // IMPORTANT: Reset list at the start of each Play session
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var store = LoadStore();
                if (store.resetOnPlay)
                {
                    store.records?.Clear();
                    SaveStore(store);
                }
            }

            // Refresh UI after transitions
            NotifyChanged();
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!EditorApplication.isPlaying) return modifications;

            var store = LoadStore();
            if (!store.recordingEnabled) return modifications;

            foreach (var m in modifications)
            {
                var target = m.currentValue.target;
                if (target == null) continue;

                var go = TargetToGameObject(target);
                if (go == null) continue;

                if (!IsRelevantTarget(target)) continue;

                CaptureSnapshot(store, go, target, addedInPlay: false);
            }

            SaveStore(store);
            NotifyChanged();
            return modifications;
        }

#if UNITY_2020_1_OR_NEWER
        private static void OnComponentWasAdded(Component c)
        {
            if (!EditorApplication.isPlaying) return;

            var store = LoadStore();
            if (!store.recordingEnabled) return;

            if (c == null || c.gameObject == null) return;

            if (!IsRelevantComponent(c)) return;

            CaptureSnapshot(store, c.gameObject, c, addedInPlay: true);

            SaveStore(store);
            NotifyChanged();
        }
#endif

        private static bool IsRelevantTarget(UnityEngine.Object target)
        {
            if (target is Transform) return true;     // includes RectTransform
            if (target is Collider) return true;      // all collider types
            if (target is MeshRenderer) return true;
            if (target is MeshFilter) return true;
            if (target is MonoBehaviour) return true; // scripts changed manually in inspector
            return false;
        }

        private static bool IsRelevantComponent(Component c)
        {
            if (c is Transform) return true;
            if (c is Collider) return true;
            if (c is MeshRenderer) return true;
            if (c is MeshFilter) return true;
            if (c is MonoBehaviour) return true;
            return false;
        }

        private static GameObject TargetToGameObject(UnityEngine.Object target)
        {
            if (target is GameObject go) return go;
            if (target is Component c) return c.gameObject;
            return null;
        }

        private static void CaptureSnapshot(RecordStore store, GameObject go, UnityEngine.Object target, bool addedInPlay)
        {
            if (store.records == null) store.records = new List<ObjectRecord>();

            var gid = GlobalObjectId.GetGlobalObjectIdSlow(go);
            var gidStr = gid.ToString();

            // Basic invalid check (varies by Unity version but this prevents empty ids)
            if (string.IsNullOrEmpty(gidStr) || gidStr.Contains("00000000000000000000000000000000"))
                return;

            var rec = store.records.FirstOrDefault(r => r.globalId == gidStr);
            if (rec == null)
            {
                rec = new ObjectRecord
                {
                    globalId = gidStr,
                    objectName = go.name,
                    components = new List<ComponentSnapshot>(),
                    lastTouchedTicks = DateTime.UtcNow.Ticks
                };
                store.records.Add(rec);
            }
            else
            {
                rec.objectName = go.name;
                rec.lastTouchedTicks = DateTime.UtcNow.Ticks;
            }

            if (!(target is Component component)) return;

            string typeName = component.GetType().AssemblyQualifiedName;
            if (string.IsNullOrEmpty(typeName)) return;

            string json = EditorJsonUtility.ToJson(component);
            if (string.IsNullOrEmpty(json)) return;

            var existing = rec.components.FirstOrDefault(x => x.componentType == typeName);
            if (existing == null)
            {
                rec.components.Add(new ComponentSnapshot
                {
                    componentType = typeName,
                    json = json,
                    addedInPlayMode = addedInPlay
                });
            }
            else
            {
                existing.json = json;
                existing.addedInPlayMode = existing.addedInPlayMode || addedInPlay;
            }
        }

        private static RecordStore LoadStore()
        {
            string json = SessionState.GetString(SessionKeyStatic, "");
            if (string.IsNullOrEmpty(json)) return new RecordStore();

            try
            {
                return JsonUtility.FromJson<RecordStore>(json) ?? new RecordStore();
            }
            catch
            {
                return new RecordStore();
            }
        }

        private static void SaveStore(RecordStore store)
        {
            SessionState.SetString(SessionKeyStatic, JsonUtility.ToJson(store));
        }
    }
}
#endif
