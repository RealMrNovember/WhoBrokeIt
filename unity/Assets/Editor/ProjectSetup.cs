using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Net;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;
using WhoBrokeIt.Vehicles;

/// <summary>Batchmode entry points: builds the P1 workshop scene and a Windows player. Run via -executeMethod ProjectSetup.BuildWindows.</summary>
public static partial class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Greenfield.unity";   // the game scene (the old Workshop test yard: BuildWorkshopScene, no longer built by default)

    public static void BuildWindows()
    {
        SetupRenderPipeline();
        SetupPlayerSettings();
        BuildGreenfieldScene();

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

    /// <summary>Builds the scene and a Linux player only (fast): used for screenshots and server tests.</summary>
    public static void BuildLinuxShots()
    {
        SetupRenderPipeline();
        SetupPlayerSettings();
        BuildGreenfieldScene();
        BuildLinuxServer();
    }

    /// <summary>Linux dedicated-server build (headless, no rendering). The scene must already exist (BuildWindows creates it).</summary>
    public static void BuildLinuxServer()
    {
        // The Unity editor image has no Dedicated Server module and the player crashes on the null graphics device (-nographics),
        // so the server is the normal Linux player run under a virtual display (Xvfb); cameras are disabled at runtime.
        SetupRenderPipeline();
        SetupPlayerSettings();
        // the server talks to the matchmaker over the private docker network (plain http)
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Server/wbi-server.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[WBI] Server build: " + report.summary.result + ", " + (report.summary.totalSize / (1024 * 1024)) + " MB");
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    /// <summary>Linux player used only for the headless multiplayer smoke test (scene must already exist).</summary>
    public static void BuildLinuxSmoke()
    {
        // URP crashes the null graphics device used by -nographics; the smoke test never renders, so build it without a pipeline.
        GraphicsSettings.defaultRenderPipeline = null;
        for (var i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i);
            QualitySettings.renderPipeline = null;
        }
        SetupPlayerSettings();
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Linux/wbi.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[WBI] Linux build: " + report.summary.result);
        SetupRenderPipeline();   // restore URP for the real (Windows) builds
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
        // quality: soft shadows, 4x MSAA, HDR (needed for bloom), shadows out to 60 m
        var so = new SerializedObject(asset);
        void SetF(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
        void SetI(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
        void SetB(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
        SetF("m_ShadowDistance", 60f);
        SetI("m_MainLightShadowmapResolution", 2048);
        SetB("m_SoftShadowsSupported", true);
        SetI("m_MSAA", 4);
        SetB("m_SupportsHDR", true);
        SetI("m_ShadowCascadeCount", 3);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);

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
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        var brandIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Brand/icon.png");
        if (brandIcon != null) PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { brandIcon }, IconKind.Application);
        PlayerSettings.runInBackground = true;
        PlayerSettings.insecureHttpOption = InsecureHttpOption.NotAllowed;   // game clients only use https
        var versionFile = Path.Combine(Application.dataPath, "..", "VERSION");
        PlayerSettings.bundleVersion = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "0.0.1";
    }

    // ---------- helpers ----------
    static Material Mat(string name, Color color)
    {
        Directory.CreateDirectory("Assets/Materials");
        var path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        if (parent != null) go.transform.localPosition = pos; else go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    /// <summary>Server-authoritative synced object (NetworkObject + NetworkTransform [+ NetworkRigidbody]).</summary>
    static void Net(GameObject go, bool withRigidbody)
    {
        go.AddComponent<NetworkObject>();
        var nt = go.AddComponent<NetworkTransform>();
        nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
        nt.Interpolate = true;
        if (withRigidbody) go.AddComponent<NetworkRigidbody>();
    }

    static Rigidbody Body(GameObject go, float mass)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.6f;
        Net(go, true);
        return rb;
    }

    static void Carry(GameObject go, float mass)
    {
        Body(go, mass);
        go.AddComponent<Carryable>().prompt = "Al";
        go.AddComponent<Resettable>();
    }

    static Clue MakeClue(GameObject go, int index, string title, string text)
    {
        go.AddComponent<NetworkObject>();
        var c = go.AddComponent<Clue>();
        c.index = index;
        c.title = title;
        c.text = text;
        return c;
    }

    // ---------- procedural PBR-ish textures (original, generated here; no third-party art) ----------
    static float N(float x, float y, float scale) { return Mathf.PerlinNoise(x * scale + 17.3f, y * scale + 91.7f); }
    static float Fbm(float x, float y, float s)
    {
        return N(x, y, s) * 0.5f + N(x, y, s * 2.1f) * 0.3f + N(x, y, s * 4.3f) * 0.2f;
    }

    static Texture2D ProcTex(string name, int size, System.Func<float, float, Color> f)
    {
        Directory.CreateDirectory("Assets/Textures/Proc");
        var path = "Assets/Textures/Proc/" + name + ".png";
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) return existing;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, f(x / (float)size, y / (float)size));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.wrapMode = TextureWrapMode.Repeat;
        imp.anisoLevel = 8;
        imp.maxTextureSize = 512;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material MatTex(string name, Texture2D tex, Color tint, Vector2 tiling, float smoothness, float metallic = 0f)
    {
        Directory.CreateDirectory("Assets/Materials");
        var path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", tiling);
        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Color Mix(Color a, Color b, float t) { return Color.Lerp(a, b, Mathf.Clamp01(t)); }

    static Texture2D TexGrass() { return ProcTex("grass", 512, (u, v) => Mix(new Color(0.20f, 0.42f, 0.16f), new Color(0.40f, 0.62f, 0.22f), Fbm(u, v, 14f)) * (0.88f + 0.12f * N(u, v, 90f))); }
    static Texture2D TexConcrete() { return ProcTex("concrete", 512, (u, v) => Mix(new Color(0.50f, 0.50f, 0.49f), new Color(0.72f, 0.71f, 0.68f), Fbm(u, v, 9f)) * (0.93f + 0.07f * N(u, v, 140f))); }
    static Texture2D TexPlaster() { return ProcTex("plaster", 512, (u, v) => Mix(new Color(0.82f, 0.72f, 0.55f), new Color(0.92f, 0.84f, 0.68f), Fbm(u, v, 7f)) * (1f - 0.18f * Mathf.Pow(1f - v, 3f))); }
    static Texture2D TexMetal() { return ProcTex("metal", 512, (u, v) => { var s = 0.55f + 0.2f * N(u * 0.2f, v, 160f) + 0.1f * N(u, v, 14f); return new Color(s, s * 1.02f, s * 1.06f); }); }
    static Texture2D TexTile() { return ProcTex("tile", 512, (u, v) => { var gx = Mathf.Abs(((u * 6f) % 1f) - 0.5f); var gy = Mathf.Abs(((v * 6f) % 1f) - 0.5f); var grout = (gx > 0.47f || gy > 0.47f) ? 0.55f : 1f; var chk = (Mathf.FloorToInt(u * 6f) + Mathf.FloorToInt(v * 6f)) % 2 == 0 ? 0.78f : 0.68f; var s = chk * grout * (0.92f + 0.08f * Fbm(u, v, 20f)); return new Color(s, s * 0.98f, s * 0.94f); }); }
    static Texture2D TexWood()
    {
        return ProcTex("wood", 512, (u, v) =>
        {
            var plank = Mathf.Floor(u * 5f);
            var local = (u * 5f) % 1f;
            var tone = 0.85f + 0.3f * Mathf.PerlinNoise(plank * 3.7f, 1.3f);
            var grain = 0.5f + 0.5f * Mathf.Sin((v * 9f + Fbm(u, v, 6f) * 5f + plank) * 6.28f);
            var c = Mix(new Color(0.42f, 0.27f, 0.14f), new Color(0.62f, 0.43f, 0.24f), 0.35f + 0.4f * grain) * tone;
            if (local < 0.03f || local > 0.97f) c *= 0.45f;
            return c;
        });
    }

    static void SetupPostProcessing()
    {
        Directory.CreateDirectory("Assets/Settings");
        const string path = "Assets/Settings/WBI-Post.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile != null) return;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.28f);
        bloom.threshold.Override(1.05f);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);
        var grade = profile.Add<ColorAdjustments>(true);
        grade.saturation.Override(4f);
        grade.contrast.Override(10f);
        grade.postExposure.Override(-0.1f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.20f);
        vignette.smoothness.Override(0.5f);
        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }

    // ---------- scene ----------
    static void BuildWorkshopScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // sky + image based ambient light instead of a flat colour
        var skyMat = new Material(Shader.Find("Skybox/Procedural"));
        skyMat.SetFloat("_SunSize", 0.05f);
        skyMat.SetFloat("_AtmosphereThickness", 1.05f);
        skyMat.SetColor("_SkyTint", new Color(0.55f, 0.62f, 0.78f));
        skyMat.SetColor("_GroundColor", new Color(0.38f, 0.40f, 0.36f));
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(skyMat, "Assets/Materials/Sky.mat");
        RenderSettings.skybox = skyMat;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1.1f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.66f, 0.74f, 0.84f);
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 280f;
        SetupPostProcessing();
        Debug.Log("[WBI] karakter prefabi (yapay zeka modeli): " + AiAssets.BuildCharacterPrefabs());

        var ground = MatTex("GroundGrass", TexGrass(), new Color(0.66f, 0.76f, 0.52f), new Vector2(120f, 120f), 0.05f);
        var wall = MatTex("WallPlaster", TexPlaster(), Color.white, new Vector2(3f, 2f), 0.1f);
        var crateM = MatTex("WoodPlanks", TexWood(), Color.white, new Vector2(1f, 1f), 0.25f);
        var metal = MatTex("MetalBrushed", TexMetal(), new Color(0.78f, 0.82f, 0.88f), new Vector2(2f, 2f), 0.55f, 0.6f);
        var yellow = Mat("Yellow", new Color(0.97f, 0.76f, 0.1f));
        var dark = Mat("Dark", new Color(0.12f, 0.12f, 0.14f));
        var scrap = Mat("Scrap", new Color(0.55f, 0.35f, 0.3f));
        var mud = Mat("Mud", new Color(0.25f, 0.17f, 0.1f));
        var floorM = MatTex("WorkshopTiles", TexTile(), new Color(0.78f, 0.78f, 0.82f), new Vector2(2.2f, 1.1f), 0.35f);
        var zoneM = Mat("Zone", new Color(0.2f, 0.7f, 0.9f));
        var red = Mat("Red", new Color(0.85f, 0.2f, 0.2f));
        var boreg = Mat("Boreg", new Color(0.85f, 0.65f, 0.3f));
        var playerMat = Mat("Player", new Color(0.95f, 0.75f, 0.1f));

        // ground + workshop shell (open to the south, z=7)
        Prim(PrimitiveType.Cube, "Ground", new Vector3(0, -0.25f, 0), new Vector3(320, 0.5f, 320), ground);
        Prim(PrimitiveType.Cube, "WorkshopFloor", new Vector3(0, 0.01f, 10.5f), new Vector3(13.5f, 0.06f, 7f), floorM);
        Prim(PrimitiveType.Cube, "Workshop_Back", new Vector3(0, 2.5f, 14), new Vector3(14, 5, 0.5f), wall);
        Prim(PrimitiveType.Cube, "Workshop_Left", new Vector3(-7, 2.5f, 10.5f), new Vector3(0.5f, 5, 7), wall);
        Prim(PrimitiveType.Cube, "Workshop_Right", new Vector3(7, 2.5f, 10.5f), new Vector3(0.5f, 5, 7), wall);
        Prim(PrimitiveType.Cube, "Workshop_Roof", new Vector3(0, 5.2f, 10.5f), new Vector3(14.5f, 0.4f, 7.5f), metal);
        Prim(PrimitiveType.Cube, "Ramp", new Vector3(-6, 0.6f, -4), new Vector3(4, 0.3f, 6), metal).transform.rotation = Quaternion.Euler(-12, 0, 0);

        // water tower with the famous forklift on top (decor)
        Prim(PrimitiveType.Cylinder, "WaterTower", new Vector3(-17, 6, 16), new Vector3(4, 6, 4), metal);
        Prim(PrimitiveType.Cylinder, "WaterTowerTank", new Vector3(-17, 12.5f, 16), new Vector3(6, 1.2f, 6), wall);
        Prim(PrimitiveType.Cube, "TowerForklift", new Vector3(-17, 14.4f, 16), new Vector3(1.4f, 1.1f, 2.2f), yellow, null, false);

        // collection zone (outside, west)
        var zone = Prim(PrimitiveType.Cube, "CollectionZone", new Vector3(-12, 0.04f, 4), new Vector3(5, 0.05f, 5), zoneM, null, true);
        zone.GetComponent<Collider>().isTrigger = true;
        zone.GetComponent<BoxCollider>().size = new Vector3(1f, 40f, 1f);
        var collection = zone.AddComponent<CollectionZone>();

        // delivery zone inside the workshop
        var del = Prim(PrimitiveType.Cube, "DeliveryZone", new Vector3(4.2f, 0.05f, 11), new Vector3(3.2f, 0.05f, 3.2f), zoneM, null, true);
        del.GetComponent<Collider>().isTrigger = true;
        del.GetComponent<BoxCollider>().size = new Vector3(1f, 120f, 1f);
        var delivery = del.AddComponent<DeliveryZone>();

        // debris at the workshop entrance
        var rnd = new System.Random(7);
        var debrisModels = new[] { "prop_sandik", "prop_varil", "prop_cop_kutusu" };
        for (var i = 0; i < 22; i++)
        {
            var dpos = new Vector3(-4.5f + (float)rnd.NextDouble() * 9f, 0.45f, 3f + (float)rnd.NextDouble() * 4f);
            var dsize = 0.38f + (float)rnd.NextDouble() * 0.14f;
            var d = AiAssets.Model(debrisModels[i % 3], dsize, false, (float)rnd.NextDouble() * 360f, true);
            if (d != null) d.transform.position = dpos;
            else
            {
                var type = i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube;
                d = Prim(type, "Debris_" + i, dpos, Vector3.one * (dsize - 0.03f), i % 3 == 0 ? metal : crateM);
            }
            d.name = "Debris_" + i;
            d.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 90f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 90f);
            Body(d, 1f);
            d.AddComponent<Debris>();
            d.AddComponent<Carryable>().prompt = "Al";
        }

        // props: a few crates and barrels in the yard
        for (var i = 0; i < 8; i++)
        {
            var cpos = new Vector3(-9 + (i % 4) * 1.25f, 0.05f + (i / 4) * 0.98f, -9);
            var c = AiAssets.Model("prop_sandik", 0.9f, false, i * 17f, true);
            if (c != null) c.transform.position = cpos;
            else c = Prim(PrimitiveType.Cube, "Crate_" + i, cpos + Vector3.up * 0.45f, Vector3.one * 0.9f, crateM);
            c.name = "Crate_" + i;
            Carry(c, 4f);
        }
        for (var i = 0; i < 3; i++)
        {
            var bpos = new Vector3(-6 + i * 1.2f, 0.05f, -13);
            var b = AiAssets.Model(i == 1 ? "prop_cop_kutusu" : "prop_varil", i == 1 ? 0.95f : 1.0f, true, i * 40f, true);
            if (b != null) b.transform.position = bpos;
            else b = Prim(PrimitiveType.Cylinder, "Barrel_" + i, bpos + Vector3.up * 0.5f, new Vector3(0.8f, 0.5f, 0.8f), red);
            b.name = "Barrel_" + i;
            Carry(b, 5f);
        }

        // static scenery (no physics): dumpster, benches, hydrant, street lamps, bins
        void Decor(string id, float size, bool byHeight, Vector3 pos, float yaw)
        {
            var m = AiAssets.Model(id, size, byHeight, yaw, true);
            if (m == null) return;
            m.transform.position = pos;
        }
        Decor("prop_konteyner", 1.5f, true, new Vector3(-14f, 0f, -3f), 90f);
        Decor("prop_bank", 1.9f, false, new Vector3(-5f, 0f, -17f), 0f);
        Decor("prop_bank", 1.9f, false, new Vector3(6f, 0f, -17.5f), 180f);
        Decor("prop_muslugu", 0.8f, true, new Vector3(3.8f, 0f, -3.5f), 0f);
        Decor("prop_cop_kutusu", 0.95f, true, new Vector3(-11f, 0f, -8f), 30f);
        Decor("prop_cop_kutusu", 0.95f, true, new Vector3(12f, 0f, -5f), -20f);
        foreach (var lp in new[] { new Vector3(-8f, 0f, -11f), new Vector3(11f, 0f, -12f) })
        {
            Decor("prop_lamba", 4.0f, true, lp, 0f);
            if (AiAssets.Has("prop_lamba"))
            {
                var lg = new GameObject("StreetLampLight");
                lg.transform.position = lp + new Vector3(0f, 3.7f, 0f);
                var ll = lg.AddComponent<Light>();
                ll.type = LightType.Point;
                ll.range = 13f;
                ll.intensity = 2.5f;
                ll.color = new Color(1f, 0.82f, 0.55f);
            }
        }

        // hand trolley (push it; loads ride on it)
        var trolley = Prim(PrimitiveType.Cube, "Trolley", new Vector3(-3, 0.4f, -7), new Vector3(1.3f, 0.12f, 0.9f), metal);
        Body(trolley, 12f).linearDamping = 1.8f;
        trolley.AddComponent<Resettable>();
        Prim(PrimitiveType.Cube, "TrolleyHandle", new Vector3(0f, 0.5f, -0.5f), new Vector3(1.0f, 0.08f, 0.08f), dark, trolley.transform, false);
        for (var i = 0; i < 4; i++)
            Prim(PrimitiveType.Cylinder, "Wheel_" + i, new Vector3(i % 2 == 0 ? -0.55f : 0.55f, -0.18f, i < 2 ? -0.35f : 0.35f), new Vector3(0.22f, 0.04f, 0.22f), dark, trolley.transform, false)
                .transform.localRotation = Quaternion.Euler(0, 0, 90);

        // pallet + tool cabinet (heavy: needs the forklift)
        var pallet = new GameObject("Pallet");
        pallet.transform.position = new Vector3(8.5f, 0, -1f);
        Prim(PrimitiveType.Cube, "Top", new Vector3(0, 0.34f, 0), new Vector3(1.4f, 0.08f, 1.4f), crateM, pallet.transform);
        for (var i = -1; i <= 1; i++)
            Prim(PrimitiveType.Cube, "Runner" + i, new Vector3(i * 0.55f, 0.15f, 0), new Vector3(0.15f, 0.3f, 1.4f), crateM, pallet.transform);
        Body(pallet, 15f);
        pallet.AddComponent<Resettable>();

        var cabinet = AiAssets.Model("prop_dolap", 1.8f, true, 0f, true);
        if (cabinet != null) cabinet.transform.position = new Vector3(8.5f, 0.4f, -1f);
        else cabinet = Prim(PrimitiveType.Cube, "ToolCabinet", new Vector3(8.5f, 1.3f, -1f), new Vector3(1f, 1.8f, 0.7f), red);
        cabinet.name = "ToolCabinet";
        Body(cabinet, 70f);
        cabinet.AddComponent<Resettable>();
        delivery.target = cabinet.GetComponent<Rigidbody>();

        BuildForklift(new Vector3(8.5f, 0.02f, -7f));
        BuildSkateboard(new Vector3(2.4f, 0.02f, -10.2f), 20f);
        BuildBicycle(new Vector3(4.8f, 0.02f, -10.8f), -15f);

        // lights + fuse box
        var sun = new GameObject("Sun");
        var sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.15f;
        sunLight.color = new Color(1f, 0.93f, 0.80f);
        sunLight.shadows = LightShadows.Soft;
        sunLight.shadowStrength = 0.9f;
        RenderSettings.sun = sunLight;
        sun.transform.rotation = Quaternion.Euler(50, -30, 0);

        var lamps = new Light[3];
        for (var i = 0; i < 3; i++)
        {
            var lamp = new GameObject("WorkshopLamp_" + i);
            lamp.transform.position = new Vector3(-4 + i * 4, 4.4f, 10.5f);
            var l = lamp.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 11f;
            l.intensity = 6f;
            l.color = new Color(1f, 0.85f, 0.55f);
            lamps[i] = l;
        }

        var box = Prim(PrimitiveType.Cube, "FuseBox", new Vector3(0, 1.3f, 13.65f), new Vector3(1f, 1.4f, 0.4f), metal);
        var ind = Prim(PrimitiveType.Sphere, "FuseIndicator", new Vector3(0.25f, 1.8f, 13.4f), Vector3.one * 0.22f, metal, null, false);
        box.AddComponent<NetworkObject>();
        var fuse = box.AddComponent<FuseBox>();
        fuse.lights = lamps;
        fuse.indicator = ind.GetComponent<Renderer>();

        // clues
        var boreg1 = Prim(PrimitiveType.Capsule, "Boreg", new Vector3(1.2f, 0.25f, 13.0f), new Vector3(0.28f, 0.2f, 0.28f), boreg);
        boreg1.transform.rotation = Quaternion.Euler(0, 0, 90);
        MakeClue(boreg1, 0, "Sıcak börek", "Sigorta kutusunun dibinde yarım yenmiş, hâlâ sıcak bir börek duruyor. Kimse bu kadar tedbirsiz olamaz.");
        for (var i = 0; i < 5; i++)
            Prim(PrimitiveType.Cube, "Footprint_" + i, new Vector3(-1.4f + i * 0.2f, 0.06f, 8.2f + i * 1.0f), new Vector3(0.18f, 0.02f, 0.32f), mud, null, false);
        var print = Prim(PrimitiveType.Cube, "FootprintClue", new Vector3(-0.6f, 0.1f, 10.4f), new Vector3(0.9f, 0.1f, 1.2f), mud, null, true);
        print.GetComponent<Renderer>().enabled = false;
        MakeClue(print, 1, "Küçük patili izler", "Çamurlu, küçük, çok hızlı bırakılmış izler. Bir çocuk ya da bir şey... tüylü bir şey.");
        var cable = Prim(PrimitiveType.Cylinder, "ChewedCable", new Vector3(-3.4f, 1.1f, 13.6f), new Vector3(0.08f, 0.5f, 0.08f), dark);
        MakeClue(cable, 2, "Kemirilmiş kablo", "Kablo temiz kesilmemiş: minik dişlerin izi var. Sigortanın atmasının sebebi bu.");

        WireDestructibles();
        BuildPlayerAndRuntime(scene, ScenePath, crateM, scrap, metal, collection, delivery, new Vector3(0, 1.1f, -14));
    }

    /// <summary>Player prefab, camera + post processing, case manager, NetworkManager with all runtime components, NetworkObject hashes, scene save. Shared by every scene.</summary>
    static void BuildPlayerAndRuntime(UnityEngine.SceneManagement.Scene scene, string scenePath, Material crateM, Material scrap, Material metal, CollectionZone collection, DeliveryZone delivery, Vector3 spawnBase)
    {
        // player prefab (spawned by the NetworkManager for each client)
        // the character model is built at runtime from CharacterCatalog (6 originals; custom prefabs go to Resources/Characters)
        Directory.CreateDirectory("Assets/Resources/WBI");
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/WBI/CharacterBase.mat") == null)
        {
            var cm0 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            cm0.SetFloat("_Smoothness", 0.38f);
            AssetDatabase.CreateAsset(cm0, "Assets/Resources/WBI/CharacterBase.mat");
        }
        var player = new GameObject("Player");
        var visualAnchor = new GameObject("Visual");
        visualAnchor.transform.SetParent(player.transform, false);
        visualAnchor.transform.localPosition = new Vector3(0f, -1f, 0f);   // character models have their feet at the origin
        player.transform.position = spawnBase;
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.5f; cc.center = Vector3.zero;
        player.AddComponent<NetworkObject>();
        var ont = player.AddComponent<OwnerNetworkTransform>();
        ont.SyncScaleX = ont.SyncScaleY = ont.SyncScaleZ = false;
        ont.Interpolate = true;
        player.AddComponent<PlayerController>();
        player.AddComponent<InteractionSystem>();

        var hold = new GameObject("HoldPoint");
        hold.transform.SetParent(player.transform, false);
        hold.transform.localPosition = new Vector3(0, 0.1f, 1.5f);
        var carry = player.AddComponent<CarrySystem>();
        carry.holdPoint = hold.transform;

        var broom = Prim(PrimitiveType.Cube, "BroomVisual", new Vector3(0.55f, 0.1f, 0.9f), new Vector3(0.08f, 0.08f, 1.6f), crateM, player.transform, false);
        Prim(PrimitiveType.Cube, "BroomHead", new Vector3(0, 0, 0.8f), new Vector3(1.8f, 1.2f, 0.12f), scrap, broom.transform, false);
        var hammer = Prim(PrimitiveType.Cube, "HammerVisual", new Vector3(0.55f, 0.2f, 0.8f), new Vector3(0.08f, 0.08f, 0.8f), crateM, player.transform, false);
        Prim(PrimitiveType.Cube, "HammerHead", new Vector3(0, 0, 0.45f), new Vector3(3f, 2f, 0.4f), metal, hammer.transform, false);
        broom.SetActive(false);
        hammer.SetActive(false);
        var net = player.AddComponent<PlayerNet>();
        net.spawnBase = spawnBase;
        net.carry = carry;
        net.broomVisual = broom;
        net.hammerVisual = hammer;
        net.visualAnchor = visualAnchor.transform;
        var proxy = new GameObject("PlayerProxy");
        proxy.transform.SetParent(player.transform, false);
        var pcol = proxy.AddComponent<CapsuleCollider>();
        pcol.height = 2f;
        pcol.radius = 0.5f;
        var prb = proxy.AddComponent<Rigidbody>();
        prb.isKinematic = true;
        net.proxyRb = prb;
        proxy.AddComponent<ProxyOwner>().owner = net;
        player.AddComponent<ToolController>();

        Directory.CreateDirectory("Assets/Prefabs");
        var playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player.prefab");
        Object.DestroyImmediate(player);

        // camera (enabled for the local player once it spawns)
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = 220f;
        camGo.AddComponent<AudioListener>();
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var volGo = new GameObject("PostProcessing");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/WBI-Post.asset");
        camGo.transform.position = new Vector3(0, 6, -22);
        camGo.transform.rotation = Quaternion.Euler(15, 0, 0);
        camGo.AddComponent<ThirdPersonCamera>().enabled = false;

        // case manager (shared case state)
        var gm = new GameObject("CaseManager");
        gm.AddComponent<NetworkObject>();
        var cm = gm.AddComponent<CaseManager>();
        cm.collectionZone = collection;
        cm.deliveryZone = delivery;

        // network manager + host/join menu
        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<NetworkManager>();
        var ut = nmGo.AddComponent<UnityTransport>();
        var prefabList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
        prefabList.Add(new NetworkPrefab { Prefab = playerPrefab });
        AssetDatabase.CreateAsset(prefabList, "Assets/Prefabs/NetworkPrefabs.asset");
        nm.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = ut,
            PlayerPrefab = playerPrefab,
            EnableSceneManagement = true,
            ConnectionApproval = true,
            TickRate = 30
        };
        nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
        nmGo.AddComponent<NetworkMenu>();
        nmGo.AddComponent<PauseMenu>();
        nmGo.AddComponent<WhoBrokeIt.Net.ChatBox>();
        nmGo.AddComponent<PerfOverlay>();
        nmGo.AddComponent<SmokeRunner>();
        nmGo.AddComponent<ScreenshotRunner>();
        nmGo.AddComponent<UpdateChecker>();
        nmGo.AddComponent<InstallGuard>();
        nmGo.AddComponent<ConnectionGate>();
        nmGo.AddComponent<DedicatedServer>();

        // The scene must exist on disk before NetworkObject can derive GlobalObjectIdHash: save, generate hashes, save again.
        EditorSceneManager.SaveScene(scene, scenePath);
        var validate = typeof(NetworkObject).GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fixedCount = 0;
        foreach (var no in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (validate != null) validate.Invoke(no, null);
            EditorUtility.SetDirty(no);
            var prop = new SerializedObject(no).FindProperty("GlobalObjectIdHash");
            if (prop != null && prop.uintValue != 0) fixedCount++;
        }
        Debug.Log("[WBI] NetworkObject hash uretilen: " + fixedCount);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.SaveAssets();
    }
}
