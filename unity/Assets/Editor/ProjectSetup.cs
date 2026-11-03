using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;

/// <summary>Batchmode entry points: builds the P0 test yard scene and a Windows player. Run via -executeMethod ProjectSetup.BuildWindows.</summary>
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/TestYard.unity";

    public static void BuildWindows()
    {
        SetupRenderPipeline();
        SetupPlayerSettings();
        BuildTestYardScene();

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Windows/WhoBrokeIt.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[WBI] Build result: " + report.summary.result + ", size " + (report.summary.totalSize / (1024 * 1024)) + " MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    static void SetupRenderPipeline()
    {
        Directory.CreateDirectory("Assets/Settings");
        const string assetPath = "Assets/Settings/WBI-URP.asset";
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
        if (asset == null)
        {
            var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rd, "Assets/Settings/WBI-URP-Renderer.asset");
            asset = UniversalRenderPipelineAsset.Create(rd);
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        GraphicsSettings.defaultRenderPipeline = asset;
        for (var i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.renderPipeline = asset;
        }
        AssetDatabase.SaveAssets();
    }

    static void SetupPlayerSettings()
    {
        PlayerSettings.companyName = "CiciByte";
        PlayerSettings.productName = "Who Broke It";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;
    }

    static Material MakeMaterial(string name, Color color)
    {
        Directory.CreateDirectory("Assets/Materials");
        var path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    static void BuildTestYardScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var ground = MakeMaterial("Ground", new Color(0.42f, 0.55f, 0.35f));
        var wall = MakeMaterial("Wall", new Color(0.85f, 0.78f, 0.62f));
        var crate = MakeMaterial("Crate", new Color(0.72f, 0.5f, 0.25f));
        var metal = MakeMaterial("Metal", new Color(0.35f, 0.4f, 0.45f));
        var playerMat = MakeMaterial("Player", new Color(0.95f, 0.75f, 0.1f));

        Prim(PrimitiveType.Cube, "Ground", new Vector3(0, -0.25f, 0), new Vector3(60, 0.5f, 60), ground);
        Prim(PrimitiveType.Cube, "Workshop_Back", new Vector3(0, 2.5f, 14), new Vector3(14, 5, 0.5f), wall);
        Prim(PrimitiveType.Cube, "Workshop_Left", new Vector3(-7, 2.5f, 10.5f), new Vector3(0.5f, 5, 7), wall);
        Prim(PrimitiveType.Cube, "Workshop_Right", new Vector3(7, 2.5f, 10.5f), new Vector3(0.5f, 5, 7), wall);
        Prim(PrimitiveType.Cube, "Ramp", new Vector3(-6, 0.6f, -4), new Vector3(4, 0.3f, 6), metal).transform.rotation = Quaternion.Euler(-12, 0, 0);

        for (var i = 0; i < 12; i++)
        {
            var c = Prim(PrimitiveType.Cube, "Crate_" + i, new Vector3(-3 + (i % 4) * 1.4f, 0.5f + (i / 4) * 1.05f, 3), Vector3.one, crate);
            c.AddComponent<Rigidbody>().mass = 4f;
        }
        for (var i = 0; i < 4; i++)
        {
            var b = Prim(PrimitiveType.Sphere, "Barrel_" + i, new Vector3(4 + i * 1.1f, 0.6f, 2), Vector3.one * 1.1f, metal);
            b.AddComponent<Rigidbody>().mass = 6f;
        }

        var sun = new GameObject("Sun");
        var sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 0.8f;
        sunLight.color = new Color(1f, 0.92f, 0.78f);
        sun.transform.rotation = Quaternion.Euler(45, -30, 0);

        var yardLights = new Light[3];
        for (var i = 0; i < 3; i++)
        {
            var lamp = new GameObject("YardLamp_" + i);
            lamp.transform.position = new Vector3(-4 + i * 4, 3.2f, 10);
            var l = lamp.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 9f;
            l.intensity = 3f;
            l.color = new Color(1f, 0.85f, 0.55f);
            yardLights[i] = l;
        }

        var box = Prim(PrimitiveType.Cube, "FuseBox", new Vector3(0, 1.2f, 13.6f), new Vector3(1f, 1.4f, 0.4f), metal);
        var ind = Prim(PrimitiveType.Sphere, "FuseIndicator", new Vector3(0.25f, 1.7f, 13.35f), Vector3.one * 0.2f, metal);
        var fuse = box.AddComponent<FuseBox>();
        fuse.lights = yardLights;
        fuse.indicator = ind.GetComponent<Renderer>();

        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        player.transform.position = new Vector3(0, 1.1f, -6);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero;
        var pc = player.AddComponent<PlayerController>();
        player.AddComponent<InteractionSystem>();

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
        camGo.AddComponent<AudioListener>();
        var tpc = camGo.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        pc.cameraTransform = camGo.transform;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }
}
