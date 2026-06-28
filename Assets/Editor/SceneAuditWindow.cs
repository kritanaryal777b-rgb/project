using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

// Editor-only utility. Put this script inside a folder named "Editor" anywhere in Assets
// (e.g. Assets/Scripts/Editor/SceneAuditWindow.cs) so Unity excludes it from builds.
// Open it via the menu: Tools > Scene Audit > Find Duplicate Scripts
public class SceneAuditWindow : EditorWindow
{
    [MenuItem("Tools/Scene Audit/Find OnGUI + PlayerController Scripts")]
    public static void ShowWindow()
    {
        GetWindow<SceneAuditWindow>("Scene Audit");
    }

    private List<(GameObject go, MonoBehaviour script)> onGuiResults = new();
    private List<(GameObject go, MonoBehaviour script)> playerControllerResults = new();

    void OnGUI()
    {
        if (GUILayout.Button("Scan Scene", GUILayout.Height(30)))
            Scan();

        GUILayout.Space(10);
        GUILayout.Label($"Scripts with OnGUI() found: {onGuiResults.Count}", EditorStyles.boldLabel);
        foreach (var (go, script) in onGuiResults)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            GUILayout.Label(script.GetType().Name, GUILayout.Width(150));
            if (GUILayout.Button("Select", GUILayout.Width(60)))
                Selection.activeGameObject = go;
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                Undo.DestroyObjectImmediate(script);
                Scan();
            }
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label($"PlayerController components found: {playerControllerResults.Count}", EditorStyles.boldLabel);
        foreach (var (go, script) in playerControllerResults)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
                Selection.activeGameObject = go;
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                Undo.DestroyObjectImmediate(script);
                Scan();
            }
            EditorGUILayout.EndHorizontal();
        }

        if (onGuiResults.Count == 0 && playerControllerResults.Count == 0)
            GUILayout.Label("Click 'Scan Scene' to check for duplicates.");
    }

    void Scan()
    {
        onGuiResults.Clear();
        playerControllerResults.Clear();

        MonoBehaviour[] allScripts = GameObject.FindObjectsOfType<MonoBehaviour>(true);

        foreach (var script in allScripts)
        {
            if (script == null) continue;

            MethodInfo onGuiMethod = script.GetType().GetMethod(
                "OnGUI",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (onGuiMethod != null && onGuiMethod.DeclaringType != typeof(MonoBehaviour))
                onGuiResults.Add((script.gameObject, script));

            if (script.GetType().Name == "PlayerController")
                playerControllerResults.Add((script.gameObject, script));
        }
    }
}
