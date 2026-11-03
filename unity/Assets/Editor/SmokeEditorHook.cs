using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Runs the SmokeRunner scenario inside the editor (batchmode PlayMode) and exits with its result.
/// Usage: unity-editor -batchmode -nographics -projectPath ... -executeMethod SmokeEditorHook.Run -smoke host1
/// </summary>
[InitializeOnLoad]
public static class SmokeEditorHook
{
    const string ResultFile = "smoke-result.txt";
    static readonly double Timeout = 240;
    static double started;

    static SmokeEditorHook()
    {
        if (!System.Environment.GetCommandLineArgs().Contains("-smoke")) return;
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.update += OnUpdate;
        started = EditorApplication.timeSinceStartup;
    }

    static bool Contains(this string[] a, string v) { return System.Array.IndexOf(a, v) >= 0; }

    public static void Run()
    {
        if (File.Exists(ResultFile)) File.Delete(ResultFile);
        EditorSceneManager.OpenScene("Assets/Scenes/Workshop.unity");
        EditorApplication.EnterPlaymode();
    }

    static void OnState(PlayModeStateChange s)
    {
        if (s != PlayModeStateChange.EnteredEditMode) return;
        var code = 2;
        if (File.Exists(ResultFile) && int.TryParse(File.ReadAllText(ResultFile).Trim(), out var r)) code = r;
        Debug.Log("[SMOKE] editor exit code " + code);
        EditorApplication.Exit(code);
    }

    static void OnUpdate()
    {
        // Application.Quit does not end play mode in a batchmode editor, so poll the result file written by SmokeRunner.
        if (File.Exists(ResultFile) && EditorApplication.isPlaying)
        {
            var code = 2;
            if (int.TryParse(File.ReadAllText(ResultFile).Trim(), out var r)) code = r;
            Debug.Log("[SMOKE] editor exit code " + code);
            EditorApplication.Exit(code);
        }
        if (EditorApplication.timeSinceStartup - started > Timeout + 120)
        {
            Debug.Log("[SMOKE] timeout");
            EditorApplication.Exit(3);
        }
    }
}
