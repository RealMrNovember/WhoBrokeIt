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
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Workshop.unity";

    public static void BuildWindows()
    {
        SetupRenderPipeline();
        SetupPlayerSettings();
        BuildWorkshopScene();

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

    // ---------- scene ----------
    static void BuildWorkshopScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.46f, 0.5f, 0.58f);

        var ground = Mat("Ground", new Color(0.42f, 0.55f, 0.35f));
        var wall = Mat("Wall", new Color(0.85f, 0.78f, 0.62f));
        var crateM = Mat("Crate", new Color(0.72f, 0.5f, 0.25f));
        var metal = Mat("Metal", new Color(0.35f, 0.4f, 0.45f));
        var yellow = Mat("Yellow", new Color(0.97f, 0.76f, 0.1f));
        var dark = Mat("Dark", new Color(0.12f, 0.12f, 0.14f));
        var scrap = Mat("Scrap", new Color(0.55f, 0.35f, 0.3f));
        var mud = Mat("Mud", new Color(0.25f, 0.17f, 0.1f));
        var floorM = Mat("WorkshopFloor", new Color(0.6f, 0.58f, 0.55f));
        var zoneM = Mat("Zone", new Color(0.2f, 0.7f, 0.9f));
        var red = Mat("Red", new Color(0.85f, 0.2f, 0.2f));
        var boreg = Mat("Boreg", new Color(0.85f, 0.65f, 0.3f));
        var playerMat = Mat("Player", new Color(0.95f, 0.75f, 0.1f));

        // ground + workshop shell (open to the south, z=7)
        Prim(PrimitiveType.Cube, "Ground", new Vector3(0, -0.25f, 0), new Vector3(70, 0.5f, 70), ground);
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
        for (var i = 0; i < 22; i++)
        {
            var type = i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var d = Prim(type, "Debris_" + i,
                new Vector3(-4.5f + (float)rnd.NextDouble() * 9f, 0.4f, 3f + (float)rnd.NextDouble() * 4f),
                Vector3.one * (0.35f + (float)rnd.NextDouble() * 0.2f), scrap);
            d.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 90f, (float)rnd.NextDouble() * 90f, 0);
            Body(d, 1f);
            d.AddComponent<Debris>();
            d.AddComponent<Carryable>().prompt = "Al";
        }

        // props: a few crates and barrels in the yard
        for (var i = 0; i < 8; i++)
        {
            var c = Prim(PrimitiveType.Cube, "Crate_" + i, new Vector3(-9 + (i % 4) * 1.2f, 0.5f + (i / 4) * 1.02f, -9), Vector3.one, crateM);
            Carry(c, 4f);
        }
        for (var i = 0; i < 3; i++)
        {
            var b = Prim(PrimitiveType.Cylinder, "Barrel_" + i, new Vector3(-6 + i * 1.2f, 0.6f, -13), new Vector3(0.9f, 0.6f, 0.9f), red);
            Carry(b, 5f);
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

        var cabinet = Prim(PrimitiveType.Cube, "ToolCabinet", new Vector3(8.5f, 1.3f, -1f), new Vector3(1f, 1.8f, 0.7f), red);
        Body(cabinet, 70f);
        cabinet.AddComponent<Resettable>();
        delivery.target = cabinet.GetComponent<Rigidbody>();

        // forklift (faces +Z, toward the pallet)
        var fkRoot = new GameObject("ForkliftRoot");
        fkRoot.transform.position = new Vector3(8.5f, 0, -7f);
        var hull = Prim(PrimitiveType.Cube, "Forklift", new Vector3(0, 0.75f, 0), new Vector3(1.6f, 0.8f, 2.2f), yellow, fkRoot.transform);
        Prim(PrimitiveType.Cube, "Counterweight", new Vector3(0, 0.1f, -1.1f), new Vector3(1.6f, 0.9f, 0.5f), dark, hull.transform, false);
        Prim(PrimitiveType.Cube, "CabPostL", new Vector3(-0.7f, 0.9f, -0.2f), new Vector3(0.08f, 1.2f, 0.08f), dark, hull.transform, false);
        Prim(PrimitiveType.Cube, "CabPostR", new Vector3(0.7f, 0.9f, -0.2f), new Vector3(0.08f, 1.2f, 0.08f), dark, hull.transform, false);
        Prim(PrimitiveType.Cube, "CabRoof", new Vector3(0, 1.5f, -0.2f), new Vector3(1.5f, 0.08f, 1.2f), dark, hull.transform, false);
        for (var i = 0; i < 4; i++)
            Prim(PrimitiveType.Cylinder, "Wheel_" + i, new Vector3(i % 2 == 0 ? -0.85f : 0.85f, -0.35f, i < 2 ? -0.7f : 0.7f), new Vector3(0.55f, 0.15f, 0.55f), dark, hull.transform, false)
                .transform.localRotation = Quaternion.Euler(0, 0, 90);
        var hullBody = Body(hull, 700f);
        hullBody.linearDamping = 0f;
        hullBody.angularDamping = 2f;
        hull.AddComponent<Resettable>();

        var forksGo = new GameObject("Forks");
        forksGo.transform.SetParent(fkRoot.transform, false);
        forksGo.transform.position = new Vector3(8.5f, 0, -7f);
        Prim(PrimitiveType.Cube, "Mast", new Vector3(0, 0.7f, 1.3f), new Vector3(1.4f, 1.4f, 0.1f), dark, forksGo.transform);
        Prim(PrimitiveType.Cube, "ProngL", new Vector3(-0.25f, 0.045f, 2.0f), new Vector3(0.18f, 0.08f, 1.3f), metal, forksGo.transform);
        Prim(PrimitiveType.Cube, "ProngR", new Vector3(0.25f, 0.045f, 2.0f), new Vector3(0.18f, 0.08f, 1.3f), metal, forksGo.transform);
        var forkRb = forksGo.AddComponent<Rigidbody>();
        forkRb.isKinematic = true;
        Net(forksGo, false);
        var fk = hull.AddComponent<Forklift>();
        fk.forks = forkRb;
        fk.prompt = "Forklifte bin";

        // lights + fuse box
        var sun = new GameObject("Sun");
        var sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 0.9f;
        sunLight.color = new Color(1f, 0.92f, 0.78f);
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

        // player prefab (spawned by the NetworkManager for each client)
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        player.transform.position = new Vector3(0, 1.1f, -14);
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
        net.carry = carry;
        net.broomVisual = broom;
        net.hammerVisual = hammer;
        net.bodyRenderers = new[] { player.GetComponent<Renderer>() };
        var proxy = new GameObject("PlayerProxy");
        proxy.transform.SetParent(player.transform, false);
        var pcol = proxy.AddComponent<CapsuleCollider>();
        pcol.height = 2f;
        pcol.radius = 0.5f;
        var prb = proxy.AddComponent<Rigidbody>();
        prb.isKinematic = true;
        net.proxyRb = prb;
        player.AddComponent<ToolController>();

        Directory.CreateDirectory("Assets/Prefabs");
        var playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player.prefab");
        Object.DestroyImmediate(player);

        // camera (enabled for the local player once it spawns)
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.75f, 0.95f);
        cam.farClipPlane = 200f;
        camGo.AddComponent<AudioListener>();
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
            ConnectionApproval = false,
            TickRate = 30
        };
        nm.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
        nmGo.AddComponent<NetworkMenu>();
        nmGo.AddComponent<SmokeRunner>();
        nmGo.AddComponent<UpdateChecker>();

        // The scene must exist on disk before NetworkObject can derive GlobalObjectIdHash: save, generate hashes, save again.
        EditorSceneManager.SaveScene(scene, ScenePath);
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
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
    }
}
