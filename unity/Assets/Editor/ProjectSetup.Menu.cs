#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Menu entries for working in the Unity Editor. The game scene is not hand-made: it is BUILT by code (GreenfieldBuilder, RideablesBuilder, ForkliftBuilder, ...),
/// so edit those scripts (or the runtime scripts under Assets/Scripts) and rebuild the scene here. Hand edits to Greenfield.unity are lost on the next rebuild.
/// </summary>
public static partial class ProjectSetup
{
    [MenuItem("Who Broke It?/1 Sahneyi yeniden kur (Greenfield)")]
    static void MenuRebuildScene()
    {
        SetupRenderPipeline();
        SetupPlayerSettings();
        BuildGreenfieldScene();
        EditorSceneManager.OpenScene(ScenePath);
        UnityEngine.Debug.Log("[WBI] Greenfield sahnesi yeniden kuruldu. Play ile calistirabilirsin (Tek Kisilik).");
    }

    [MenuItem("Who Broke It?/2 Greenfield sahnesini ac")]
    static void MenuOpenScene() { EditorSceneManager.OpenScene(ScenePath); }

    [MenuItem("Who Broke It?/3 Windows exe derle")]
    static void MenuBuildWindows() { BuildWindows(); }
}
#endif
