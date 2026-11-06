using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Netcode;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Props;
using WhoBrokeIt.Contracts;

/// <summary>
/// Greenfield Square (docs/GREENFIELD_MEYDANI.md): the first real playable environment, assembled from the Blender kit
/// (Assets/Art/Kit/&lt;cat&gt;/&lt;name&gt;.glb, see tools/blender). World coordinates: origin = centre of the plaza, +X east, +Z north.
/// Missing kit assets fall back to a plain box of the expected size so the scene always builds (the report lists what was missing).
/// </summary>
public static partial class ProjectSetup
{
    // ---------- kit loading ----------
    static readonly List<string> kitMissing = new List<string>();
    static readonly List<string> kitUsed = new List<string>();
    static Transform gfRoot;

    static readonly Dictionary<string, Vector3> kitFallbackSize = new Dictionary<string, Vector3>
    {
        { "bld_shopfront_a", new Vector3(8f, 6.5f, 5f) }, { "bld_shopfront_b", new Vector3(8f, 6.5f, 5f) }, { "bld_shopfront_c", new Vector3(8f, 6.5f, 5f) },
        { "bld_workshop_a", new Vector3(14f, 5f, 10f) }, { "bld_warehouse_a", new Vector3(24f, 7f, 16f) }, { "bld_shed_a", new Vector3(4f, 2.6f, 3f) },
        { "bld_halfwall_a", new Vector3(4f, 1.8f, 0.3f) }, { "bld_clocktower_a", new Vector3(3.5f, 14f, 3.5f) }, { "bld_watertower_a", new Vector3(5f, 12f, 5f) },
        { "prop_fountain_a", new Vector3(6.6f, 2.5f, 6.6f) }, { "prop_lamp_a", new Vector3(0.4f, 4f, 0.4f) }, { "prop_bench_a", new Vector3(1.8f, 0.9f, 0.6f) },
        { "prop_tree_a", new Vector3(3f, 5f, 3f) }, { "prop_tree_b", new Vector3(3.4f, 6f, 3.4f) }, { "prop_planter_a", new Vector3(1f, 0.8f, 0.5f) },
        { "prop_stall_a", new Vector3(2.4f, 2.6f, 1.6f) }, { "prop_bin_a", new Vector3(0.6f, 0.95f, 0.6f) }, { "prop_hydrant_a", new Vector3(0.4f, 0.8f, 0.4f) },
        { "prop_sign_a", new Vector3(0.9f, 2.8f, 0.2f) }, { "prop_sack_a", new Vector3(0.5f, 0.25f, 0.8f) }, { "prop_cementmixer_a", new Vector3(1f, 1.3f, 1.4f) },
        { "prop_barrow_a", new Vector3(0.7f, 0.7f, 1.5f) }, { "prop_trolley_a", new Vector3(1.3f, 0.9f, 0.9f) }, { "prop_planks_a", new Vector3(0.5f, 0.35f, 1.2f) },
        { "prop_crate_a", new Vector3(0.8f, 0.8f, 0.8f) }, { "prop_crate_b", new Vector3(0.5f, 0.5f, 0.5f) }, { "prop_crate_c", new Vector3(1.2f, 1.2f, 1.2f) },
        { "prop_barrel_a", new Vector3(0.6f, 0.95f, 0.6f) }, { "prop_pallet_a", new Vector3(1.2f, 0.15f, 0.8f) }, { "prop_toolbox_a", new Vector3(0.5f, 0.25f, 0.45f) },
        { "prop_sawhorse_a", new Vector3(1.2f, 0.85f, 0.55f) }, { "prop_cone_a", new Vector3(0.36f, 0.6f, 0.36f) }, { "prop_toolwall_a", new Vector3(3f, 2f, 0.2f) },
        { "prop_scaffold_a", new Vector3(3f, 3f, 3f) }, { "prop_rack_a", new Vector3(2.7f, 4f, 1.1f) }, { "prop_shelf_a", new Vector3(1.2f, 2f, 0.4f) },
        { "prop_workbench_a", new Vector3(1.8f, 1.1f, 0.72f) }, { "prop_table_a", new Vector3(1.4f, 0.8f, 0.8f) }, { "prop_fusebox_a", new Vector3(0.5f, 0.7f, 0.24f) },
        { "fnc_panel_a", new Vector3(2f, 1.1f, 0.1f) }, { "env_curb_a", new Vector3(2f, 0.2f, 1.5f) }, { "env_ramp_a", new Vector3(4f, 1f, 4.5f) },
        { "env_hill_a", new Vector3(14f, 2.5f, 10f) }, { "env_cobble_disc_a", new Vector3(28f, 0.1f, 28f) }, { "env_asphalt_strip_a", new Vector3(10f, 0.05f, 8f) },
    };

    /// <summary>Loads Assets/Art/Kit/&lt;cat&gt;/&lt;name&gt;.glb as a scene object (origin at the base). Returns a coloured box of the expected size when the asset is missing.</summary>
    static GameObject Kit(string name, Vector3 pos, float yaw = 0f, Transform parent = null, bool dynamic = false)
    {
        var cat = name.Substring(0, name.IndexOf('_'));
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Kit/" + cat + "/" + name + ".glb");
        GameObject go;
        if (asset != null)
        {
            go = Object.Instantiate(asset);
            go.name = name;
            KitColliders(go, dynamic);
            if (name == "prop_pallet_a") PalletShape(go);
            if (!kitUsed.Contains(name)) kitUsed.Add(name);
        }
        else
        {
            if (!kitMissing.Contains(name)) kitMissing.Add(name);
            var size = kitFallbackSize.ContainsKey(name) ? kitFallbackSize[name] : Vector3.one;
            go = new GameObject(name);
            var box = Prim(PrimitiveType.Cube, "Fallback", new Vector3(0f, size.y * 0.5f, 0f), size, MatS("GFv2_Fallback_" + cat, FallbackColor(cat) * 0.8f, 0.1f), go.transform, true);
            box.layer = 0;
        }
        go.transform.SetParent(dynamic ? null : (parent != null ? parent : gfRoot), false);   // network objects stay at the scene root
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    static Color FallbackColor(string cat)
    {
        switch (cat)
        {
            case "bld": return new Color(0.93f, 0.78f, 0.62f);
            case "env": return new Color(0.70f, 0.66f, 0.58f);
            case "fnc": return new Color(0.78f, 0.55f, 0.32f);
            default: return new Color(0.80f, 0.60f, 0.35f);
        }
    }

    /// <summary>The kit marks simple colliders as COL_* nodes: turn them into real colliders and hide their meshes.</summary>
    static void KitColliders(GameObject go, bool dynamic)
    {
        var cols = new List<Transform>();
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("COL_")) cols.Add(t);
        if (cols.Count == 0)
        {
            var b = BoundsOf(go);
            var bc = go.AddComponent<BoxCollider>();
            bc.center = go.transform.InverseTransformPoint(b.center);
            bc.size = b.size;
            return;
        }
        foreach (var t in cols)
        {
            var mf = t.GetComponent<MeshFilter>();
            var mr = t.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null)
            {
                var m = mf.sharedMesh;
                var boxy = IsBoxLike(m);
                if (boxy)
                {
                    var bc = t.gameObject.AddComponent<BoxCollider>();
                    bc.center = m.bounds.center;
                    bc.size = m.bounds.size;
                }
                else
                {
                    var mc = t.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = m;
                    mc.convex = dynamic;
                }
            }
            if (mr != null) Object.DestroyImmediate(mr);
            if (mf != null) Object.DestroyImmediate(mf);
        }
    }

    static bool IsBoxLike(Mesh m)
    {
        var v = m.vertices;
        if (v.Length <= 8) return true;
        var b = m.bounds;
        // all vertices on the bounding box corners (within 1 cm) = a box
        foreach (var p in v)
        {
            var dx = Mathf.Min(Mathf.Abs(p.x - b.min.x), Mathf.Abs(p.x - b.max.x));
            var dy = Mathf.Min(Mathf.Abs(p.y - b.min.y), Mathf.Abs(p.y - b.max.y));
            var dz = Mathf.Min(Mathf.Abs(p.z - b.min.z), Mathf.Abs(p.z - b.max.z));
            if (dx > 0.01f || dy > 0.01f || dz > 0.01f) return false;
        }
        return true;
    }

    static Bounds BoundsOf(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(root.transform.position, Vector3.one);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static readonly Dictionary<string, string> kitBreakTag = new Dictionary<string, string>
    {
        { "prop_crate_a", "wood" }, { "prop_crate_b", "wood" }, { "prop_crate_c", "wood" }, { "prop_barrel_a", "metal" },
        { "prop_planks_a", "wood" }, { "prop_pallet_a", "wood" },
    };

    /// <summary>
    /// The kit's pre-fractured "_broken" model as a real prefab asset. Pointing a scene object straight at the glTFast-imported .glb corrupts the
    /// built scene ("level0 is corrupted"), so the chunks are saved as a normal prefab first (the same way the character prefabs are made).
    /// </summary>
    static GameObject BrokenPrefab(string kitName)
    {
        var cat = kitName.Substring(0, kitName.IndexOf('_'));
        var glb = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Kit/" + cat + "/" + kitName + "_broken.glb");
        if (glb == null) return null;
        Directory.CreateDirectory("Assets/Prefabs/KitBroken");
        var path = "Assets/Prefabs/KitBroken/" + kitName + "_broken.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        var inst = Object.Instantiate(glb);
        inst.name = kitName + "_broken";
        foreach (var t in inst.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("COL_")) { var mr = t.GetComponent<MeshRenderer>(); if (mr != null) Object.DestroyImmediate(mr); }
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, path);
        Object.DestroyImmediate(inst);
        return prefab;
    }

    /// <summary>Makes a kit object breakable (Destructible + impact sounds + repair prompt); uses the kit's pre-fractured "_broken" model when it exists.</summary>
    static Destructible Breakable(GameObject go, string kitName, string materialTag, float massKg, bool carry, bool breakable = true)
    {
        return Destructibles.Make(go, materialTag, massKg, BrokenPrefab(kitName), carry, true, kitName == "prop_pallet_a" ? 400f : 0f, breakable);
    }

    /// <summary>Physics prop from the kit: Rigidbody + network sync + carry/reset (as the existing crates).</summary>
    static GameObject KitProp(string name, Vector3 pos, float yaw, float mass, bool carryable = true)
    {
        var go = Kit(name, pos, yaw, null, true);
        go.transform.position = pos;
        if (carryable) Carry(go, mass); else { Body(go, mass); go.AddComponent<Resettable>(); }
        go.AddComponent<ContractItem>().itemTag = ContractItem.TagFromName(name);
        if (kitBreakTag.TryGetValue(name, out var btag)) Breakable(go, name, btag, mass, carryable);
        return go;
    }

    /// <summary>URP Lit with explicit smoothness (the default 0.5 makes big flat grounds shine white).</summary>
    static Material MatS(string name, Color color, float smoothness = 0.08f, float metallic = 0f)
    {
        Directory.CreateDirectory("Assets/Materials");
        var path = "Assets/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void ContractZoneAt(string id, string label, Vector3 center, Vector3 size, Color color)
    {
        var z = Prim(PrimitiveType.Cube, "ContractZone_" + id, new Vector3(center.x, 0.07f, center.z), new Vector3(size.x, 0.03f, size.z), MatS("GFv2_ZoneB_" + id, Color.Lerp(color, new Color(0.5f, 0.5f, 0.5f), 0.35f), 0.15f), gfRoot, true);
        var bc = z.GetComponent<BoxCollider>();
        bc.isTrigger = true;
        bc.size = new Vector3(1f, 40f, 1f);
        var cz = z.AddComponent<ContractZone>();
        cz.zoneId = id;
        cz.label = label;
    }

    static GameObject Pad(string name, Vector3 center, Vector3 size, Color color)
    {
        // flat visual slab (no collider: the ground box collides)
        var g = Prim(PrimitiveType.Cube, name, center, size, MatS("GFv2_Pad_" + name, color), gfRoot, false);
        return g;
    }

    static void Invisible(string name, Vector3 center, Vector3 size)
    {
        var g = new GameObject(name);
        g.transform.SetParent(gfRoot, false);
        g.transform.localPosition = center;
        g.AddComponent<BoxCollider>().size = size;
    }

    static void SetupGreenfieldLighting()
    {
        // "Altin saat" preset (docs/SANAT_YONU.md): warm key, cool sky, trilight ambient, soft peach fog
        var skyMat = new Material(Shader.Find("Skybox/Procedural"));
        skyMat.SetFloat("_SunSize", 0.035f);
        skyMat.SetFloat("_SunSizeConvergence", 6f);
        skyMat.SetFloat("_AtmosphereThickness", 0.9f);
        skyMat.SetFloat("_Exposure", 1.0f);
        skyMat.SetColor("_SkyTint", new Color(0.52f, 0.66f, 0.92f));
        skyMat.SetColor("_GroundColor", new Color(0.58f, 0.70f, 0.50f));   // hazy green: the far lawn blends into the horizon (no tan band)
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.CreateAsset(skyMat, "Assets/Materials/GreenfieldSky.mat");
        RenderSettings.skybox = skyMat;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        // darker than the raw palette on purpose: sun + ambient add up, and bright ambient washed the ground out to white
        RenderSettings.ambientSkyColor = new Color(0.40f, 0.52f, 0.68f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.50f, 0.44f);
        RenderSettings.ambientGroundColor = new Color(0.30f, 0.33f, 0.26f);
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.95f, 0.80f, 0.66f);
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 230f;

        var sun = new GameObject("Sun");
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(1f, 0.85f, 0.64f);
        l.intensity = 1.0f;
        l.shadows = LightShadows.Soft;
        l.shadowStrength = 0.85f;
        RenderSettings.sun = l;
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
    }

    /// <summary>Everything that is "world": ground, buildings, props, landmarks, vehicles, case objects. Returns the case zones.</summary>
    static void BuildGreenfieldWorld(out CollectionZone collection, out DeliveryZone delivery, out Light[] lamps, out Rigidbody cabinetBody)
    {
        kitMissing.Clear();
        kitUsed.Clear();
        gfRoot = new GameObject("GreenfieldSquare").transform;
        SetupGreenfieldLighting();

        var dirt = new Color(0.41f, 0.64f, 0.33f);   // the base is a lawn; dirt/concrete yards and the plaza lie on top of it
        var grass = new Color(0.38f, 0.60f, 0.31f);
        var concrete = new Color(0.58f, 0.56f, 0.52f);

        // ---- ground: collision box + far grass ring + slabs ----
        var floor = Prim(PrimitiveType.Cube, "GroundCollider", new Vector3(0f, -0.5f, 3f), new Vector3(100f, 1f, 84f), MatS("GFv2_Lawn", dirt), gfRoot, true);
        var farGrass = Prim(PrimitiveType.Cylinder, "FarGrass", new Vector3(0f, -0.6f, 3f), new Vector3(1400f, 0.1f, 1400f), MatS("GFv2_Grass", grass, 0.05f), gfRoot, false);
        Pad("YardWarehouse", new Vector3(-9f, 0.01f, 18f), new Vector3(14f, 0.04f, 20f), concrete);
        Pad("YardWorkshop", new Vector3(4f, 0.01f, 20.5f), new Vector3(20f, 0.04f, 10f), concrete);
        Pad("YardConstruction", new Vector3(30f, 0.01f, 12f), new Vector3(24f, 0.04f, 16f), new Color(0.62f, 0.57f, 0.47f));

        // ---- plaza, fountain, clock tower ----
        Kit("env_cobble_disc_a", new Vector3(0f, 0f, 0f));
        Kit("prop_fountain_a", new Vector3(0f, 0f, 0f));
        Kit("bld_clocktower_a", new Vector3(0f, 0f, 10.5f), 180f);
        for (var i = 0; i < 4; i++)
        {
            var a = (i * 90f + 45f) * Mathf.Deg2Rad;
            Kit("prop_bench_a", new Vector3(Mathf.Sin(a) * 6.8f, 0f, Mathf.Cos(a) * 6.8f), i * 90f + 45f + 180f);
        }
        for (var i = 0; i < 8; i++)
        {
            var a = (i * 45f + 22.5f) * Mathf.Deg2Rad;
            Kit("prop_lamp_a", new Vector3(Mathf.Sin(a) * 13f, 0f, Mathf.Cos(a) * 13f), 0f);
        }
        Breakable(Kit("prop_stall_a", new Vector3(-9f, 0f, -8f), 35f), "prop_stall_a", "wood", 0f, false);
        Breakable(Kit("prop_stall_a", new Vector3(9f, 0f, -8f), -35f), "prop_stall_a", "wood", 0f, false);
        Kit("prop_planter_a", new Vector3(-4f, 0f, 7f), 0f); Kit("prop_planter_a", new Vector3(4f, 0f, 7f), 0f);
        Kit("prop_sign_a", new Vector3(12f, 0f, -3f), -60f);
        Kit("prop_bin_a", new Vector3(-11f, 0f, 3f), 0f); Kit("prop_hydrant_a", new Vector3(11f, 0f, 5f), 0f);

        // ---- main street (south) and shop fronts ----
        for (var i = 0; i < 9; i++) Kit("env_asphalt_strip_a", new Vector3(-40f + i * 10f, 0f, -22f), 0f);
        for (var i = 0; i < 45; i++) Kit("env_curb_a", new Vector3(-44f + i * 2f, 0f, -17.2f), 0f);
        for (var i = 0; i < 45; i++) Kit("env_curb_a", new Vector3(-44f + i * 2f, 0f, -26.6f), 180f);
        Kit("bld_shopfront_a", new Vector3(-24f, 0f, -31.5f), 0f);
        Kit("bld_shopfront_b", new Vector3(-8f, 0f, -31.5f), 0f);
        Kit("bld_shopfront_c", new Vector3(8f, 0f, -31.5f), 0f);
        Kit("bld_shopfront_a", new Vector3(24f, 0f, -31.5f), 0f);
        Kit("bld_shopfront_b", new Vector3(40f, 0f, -31.5f), 0f);
        for (var i = 0; i < 6; i++) Kit("prop_lamp_a", new Vector3(-36f + i * 14f, 0f, -17.8f), 0f);
        Kit("prop_tree_a", new Vector3(-16f, 0f, -18.6f), 0f); Kit("prop_tree_b", new Vector3(16f, 0f, -18.6f), 0f);
        Kit("prop_bench_a", new Vector3(-20f, 0f, -18.2f), 0f); Kit("prop_bench_a", new Vector3(20f, 0f, -18.2f), 0f);
        for (var i = 0; i < 4; i++) Kit("prop_cone_a", new Vector3(-30f + i * 0.9f, 0f, -22f), 0f);

        // ---- warehouse + loading dock (west) ----
        Kit("bld_warehouse_a", new Vector3(-26f, 0f, 18f), 90f);
        for (var i = 0; i < 3; i++) Kit("prop_rack_a", new Vector3(-33f, 0f, 12f + i * 4.5f), 90f);
        var rnd = new System.Random(11);
        var crateNames = new[] { "prop_crate_a", "prop_crate_b", "prop_crate_c" };
        var crateMass = new[] { 4f, 2.5f, 12f };
        for (var i = 0; i < 12; i++)
        {
            var k = i % 3;
            // a loose grid with a little jitter: crates that spawn overlapping shove each other apart with huge impulses and break before the game starts
            var crateZ = new[] { 11.5f, 14.2f, 16.9f, 25.0f };
            var c = KitProp(crateNames[k], new Vector3(-12f + (i % 3) * 2.6f + ((float)rnd.NextDouble() - 0.5f) * 0.5f, 0.05f, crateZ[i / 3] + ((float)rnd.NextDouble() - 0.5f) * 0.5f), (float)rnd.NextDouble() * 360f, crateMass[k], crateMass[k] <= 6f);
            if (i == 0) { c.name = "Crate_0"; c.transform.position = new Vector3(-14f, 0.05f, 35.5f); }   // the two-client test looks for this crate (open ground where its forklift run ends)
            if (i == 1) { c.name = "Crate_1"; c.transform.position = new Vector3(-17f, 0.05f, 35.5f); }   // the destruction test throws this one until it breaks (open ground, nothing behind it)
        }
        for (var i = 0; i < 6; i++) KitProp("prop_pallet_a", new Vector3(-20f + (i % 3) * 1.6f, 0.05f, 12f + (i / 3) * 1.2f), 0f, 14f, true);   // heavy: two people can carry it, or the forklift
        for (var i = 0; i < 4; i++) KitProp("prop_barrel_a", new Vector3(-10f + i * 1.1f, 0.05f, 28f), i * 40f, 5f);

        // ---- workshop (north) ----
        Kit("bld_workshop_a", new Vector3(4f, 0f, 28f), 180f);
        Kit("prop_workbench_a", new Vector3(0f, 0f, 30.5f), 180f);
        Kit("prop_toolwall_a", new Vector3(8f, 0f, 32.6f), 180f);
        Kit("prop_shelf_a", new Vector3(-1.8f, 0f, 32.5f), 180f);
        KitProp("prop_toolbox_a", new Vector3(0.2f, 0.95f, 30.5f), 0f, 3f);
        KitProp("prop_toolbox_a", new Vector3(4f, 0.05f, 25f), 40f, 3f);
        KitProp("prop_sawhorse_a", new Vector3(6f, 0.05f, 24f), 20f, 5f, false);

        // ---- construction site (east) ----
        foreach (var hx in new[] { 26f, 31f }) Breakable(Kit("bld_halfwall_a", new Vector3(hx, 0f, 16.5f), 0f), "bld_halfwall_a", "concrete", 0f, false);
        Kit("bld_shed_a", new Vector3(38f, 0f, 9f), 270f);
        Kit("prop_scaffold_a", new Vector3(24f, 0f, 8f), 0f); Kit("prop_scaffold_a", new Vector3(27f, 0f, 8f), 0f);
        Kit("prop_cementmixer_a", new Vector3(33f, 0f, 6f), 30f);
        for (var i = 0; i < 6; i++) KitProp("prop_sack_a", new Vector3(30f + (i % 3) * 0.7f, 0.05f, 4f + (i / 3) * 0.9f), i * 25f, 2f);
        for (var i = 0; i < 3; i++) KitProp("prop_planks_a", new Vector3(22f + i * 1.2f, 0.05f, 12f), 90f, 3f);
        KitProp("prop_barrow_a", new Vector3(28f, 0.05f, 10f), 15f, 9f, false);
        BuildHandCart(new Vector3(21f, 0.02f, 4f), 60f);   // pushable hand cart (RideKind.Cart); loads light props with Space
        for (var i = 0; i < 9; i++) Breakable(Kit("fnc_panel_a", new Vector3(19.5f + i * 2.5f, 0f, 20f), 0f), "fnc_panel_a", "wood", 0f, false);

        // ---- hill and ramp (south-east) ----
        Kit("env_hill_a", new Vector3(34f, 0f, -10f), 0f);
        Kit("env_ramp_a", new Vector3(24f, 0f, -10f), 90f);

        // ---- water tower (north-west landmark) and edges ----
        Kit("bld_watertower_a", new Vector3(-34f, 0f, 36f), 0f);
        for (var i = 0; i < 9; i++) Kit(i % 2 == 0 ? "prop_tree_a" : "prop_tree_b", new Vector3(-42f + i * 11f, 0f, 41f), 0f);
        for (var i = 0; i < 5; i++) Kit(i % 2 == 0 ? "prop_tree_b" : "prop_tree_a", new Vector3(-45f, 0f, -12f + i * 12f), 0f);
        for (var i = 0; i < 5; i++) Kit(i % 2 == 0 ? "prop_tree_b" : "prop_tree_a", new Vector3(45f, 0f, -12f + i * 12f), 0f);
        Invisible("Edge_N", new Vector3(0f, 3f, 43f), new Vector3(100f, 6f, 1f));
        Invisible("Edge_S", new Vector3(0f, 3f, -35f), new Vector3(100f, 6f, 1f));
        Invisible("Edge_W", new Vector3(-47f, 3f, 3f), new Vector3(1f, 6f, 84f));
        Invisible("Edge_E", new Vector3(47f, 3f, 3f), new Vector3(1f, 6f, 84f));

        // ---- electrical panel at the warehouse dock (breakable, repairable with the hammer; flickers its own lamp when damaged) ----
        var pnl = Kit("prop_fusebox_a", new Vector3(-13.7f, 1.2f, 14.5f), 90f);
        var pnlLamp = new GameObject("DockLamp");
        pnlLamp.transform.SetParent(gfRoot, false);
        pnlLamp.transform.localPosition = new Vector3(-12.6f, 3.2f, 14.5f);
        var pl = pnlLamp.AddComponent<Light>();
        pl.type = LightType.Point; pl.range = 8f; pl.intensity = 3.5f; pl.color = new Color(1f, 0.88f, 0.6f);
        var pind = Prim(PrimitiveType.Sphere, "PanelIndicator", new Vector3(0.18f, 0.3f, -0.15f), Vector3.one * 0.12f, MatS("GFv2_PanelInd", new Color(0.85f, 0.25f, 0.2f), 0.3f), pnl.transform, false);
        Destructibles.MakePanel(pnl, new[] { pl }, pind.GetComponent<Renderer>());

        // ---- contracts: delivery zones, boards, director ----
        ContractZoneAt("construction", "İnşaat alanı", new Vector3(28f, 0.04f, 9.5f), new Vector3(6f, 0.06f, 5f), new Color(0.25f, 0.65f, 0.95f));
        ContractZoneAt("workshop", "Atölye", new Vector3(-2f, 0.04f, 25f), new Vector3(5f, 0.06f, 4f), new Color(0.95f, 0.65f, 0.2f));
        ContractZoneAt("dock", "Depo rampası", new Vector3(-8f, 0.04f, 9f), new Vector3(6f, 0.06f, 4f), new Color(0.35f, 0.85f, 0.6f));
        ContractZoneAt("plaza", "Meydan", new Vector3(7.5f, 0.04f, -4f), new Vector3(5f, 0.06f, 5f), new Color(0.95f, 0.45f, 0.4f));
        ContractZoneAt("hill", "Tepe dibi", new Vector3(40f, 0.04f, -3f), new Vector3(5f, 0.06f, 5f), new Color(0.8f, 0.5f, 0.95f));
        for (var i = 0; i < 3; i++)
        {
            var b = Prim(PrimitiveType.Cube, "ContractBoard_" + i, new Vector3(-3.4f + i * 3.4f, 1.1f, -12.6f), new Vector3(1.9f, 2.1f, 0.18f), MatS("GFv2_Board", new Color(0.95f, 0.78f, 0.2f), 0.3f), gfRoot, true);
            Prim(PrimitiveType.Cube, "Post_" + i, new Vector3(0f, -0.9f, 0f), new Vector3(0.12f, 0.5f, 1.2f), MatS("GFv2_BoardPost", new Color(0.4f, 0.3f, 0.2f)), b.transform, false);
            Prim(PrimitiveType.Cube, "Badge_" + i, new Vector3(0f, 0.25f, -0.6f), new Vector3(0.4f, 0.4f, 0.2f), MatS("GFv2_BoardBadge", new Color(0.12f, 0.14f, 0.3f)), b.transform, false);
            b.AddComponent<NetworkObject>();
            var cb = b.AddComponent<ContractBoard>();
            cb.slot = i;
        }
        var dirGo = new GameObject("ContractDirector");
        dirGo.AddComponent<NetworkObject>();
        dirGo.AddComponent<ContractDirector>();

        // ---- vehicles ----
        BuildForklift(new Vector3(-8f, 0.02f, 21f));
        BuildSkateboard(new Vector3(5f, 0.02f, -14f), 20f);
        BuildBicycle(new Vector3(9f, 0.02f, -14.5f), -15f);
        BuildNpcs();
        BuildVendors();

        // ---- case objects (first contract: clear the debris, repair the fuse box, deliver the cabinet) ----
        var zoneM = Mat("Zone", new Color(0.2f, 0.7f, 0.9f));
        var zone = Prim(PrimitiveType.Cube, "CollectionZone", new Vector3(-6f, 0.04f, 22f), new Vector3(5f, 0.05f, 5f), zoneM, gfRoot, true);
        zone.GetComponent<Collider>().isTrigger = true;
        zone.GetComponent<BoxCollider>().size = new Vector3(1f, 40f, 1f);
        collection = zone.AddComponent<CollectionZone>();
        var del = Prim(PrimitiveType.Cube, "DeliveryZone", new Vector3(8f, 0.05f, 29f), new Vector3(3.2f, 0.05f, 3.2f), zoneM, gfRoot, true);
        del.GetComponent<Collider>().isTrigger = true;
        del.GetComponent<BoxCollider>().size = new Vector3(1f, 120f, 1f);
        delivery = del.AddComponent<DeliveryZone>();

        var debrisModels = new[] { "prop_crate_b", "prop_barrel_a", "prop_bin_a" };
        for (var i = 0; i < 22; i++)
        {
            var d = Kit(debrisModels[i % 3], new Vector3(-4f + (float)rnd.NextDouble() * 12f, 0.5f, 14f + (float)rnd.NextDouble() * 5f), (float)rnd.NextDouble() * 360f, null, true);
            d.name = "Debris_" + i;
            d.transform.position = new Vector3(-4f + (float)rnd.NextDouble() * 12f, 0.5f, 14f + (float)rnd.NextDouble() * 5f);
            d.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 90f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 90f);
            Body(d, 1f);
            d.AddComponent<Debris>();
            d.AddComponent<Carryable>().prompt = "Al";
        }

        var cabinet = Prim(PrimitiveType.Cube, "ToolCabinet", new Vector3(10f, 1f, 24f), new Vector3(1f, 1.8f, 0.7f), Mat("Red", new Color(0.85f, 0.2f, 0.2f)), gfRoot, true);
        Body(cabinet, 70f);
        cabinet.AddComponent<Resettable>();
        delivery.target = cabinet.GetComponent<Rigidbody>();
        cabinetBody = cabinet.GetComponent<Rigidbody>();
        var pal = Kit("prop_pallet_a", new Vector3(10f, 0f, 24f), 0f, null, true);
        Body(pal, 15f); pal.AddComponent<Resettable>();

        // fuse box + lights inside the workshop
        lamps = new Light[3];
        for (var i = 0; i < 3; i++)
        {
            var lamp = new GameObject("WorkshopLamp_" + i);
            lamp.transform.SetParent(gfRoot, false);
            lamp.transform.localPosition = new Vector3(-1f + i * 5f, 4.2f, 28f);
            var l = lamp.AddComponent<Light>();
            l.type = LightType.Point; l.range = 10f; l.intensity = 5f; l.color = new Color(1f, 0.85f, 0.55f);
            lamps[i] = l;
        }
        var fb = Kit("prop_fusebox_a", new Vector3(4f, 1.2f, 32.7f), 180f);
        fb.AddComponent<NetworkObject>();
        var fuse = fb.AddComponent<FuseBox>();
        fuse.lights = lamps;
        var ind = Prim(PrimitiveType.Sphere, "FuseIndicator", new Vector3(0.25f, 0.6f, -0.1f), Vector3.one * 0.18f, Mat("Metal_ind", new Color(0.8f, 0.8f, 0.8f)), fb.transform, false);
        fuse.indicator = ind.GetComponent<Renderer>();

        var mud = Mat("Mud", new Color(0.25f, 0.17f, 0.1f));
        var boreg = Prim(PrimitiveType.Capsule, "Boreg", new Vector3(5.2f, 0.25f, 32.2f), new Vector3(0.28f, 0.2f, 0.28f), Mat("Boreg", new Color(0.85f, 0.65f, 0.3f)), gfRoot, true);
        boreg.transform.rotation = Quaternion.Euler(0, 0, 90);
        MakeClue(boreg, 0, "Sıcak börek", "Sigorta kutusunun dibinde yarım yenmiş, hâlâ sıcak bir börek duruyor.");
        var print = Prim(PrimitiveType.Cube, "FootprintClue", new Vector3(2f, 0.1f, 24f), new Vector3(0.9f, 0.1f, 1.2f), mud, gfRoot, true);
        print.GetComponent<Renderer>().enabled = false;
        MakeClue(print, 1, "Küçük patili izler", "Çamurlu, küçük, çok hızlı bırakılmış izler.");
        var cable = Prim(PrimitiveType.Cylinder, "ChewedCable", new Vector3(-1.2f, 1.1f, 32.6f), new Vector3(0.08f, 0.5f, 0.08f), Mat("Dark", new Color(0.12f, 0.12f, 0.14f)), gfRoot, true);
        MakeClue(cable, 2, "Kemirilmiş kablo", "Kablo temiz kesilmemiş: minik dişlerin izi var.");

        if (kitMissing.Count > 0) Debug.Log("[WBI] Greenfield: kitte olmayan varlıklar (kutu ile yerine konuldu): " + string.Join(", ", kitMissing));
        Debug.Log("[WBI] Greenfield: kit varlığı kullanıldı: " + kitUsed.Count + ", eksik: " + kitMissing.Count);
    }

    // ---------- scene + build entry points ----------
    const string GreenfieldScenePath = ScenePath;

    static void BuildGreenfieldScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        SetupPostProcessing();
        Debug.Log("[WBI] karakter prefabi (yapay zeka modeli): " + AiAssets.BuildCharacterPrefabs());
        var crateM = MatTex("WoodPlanks", TexWood(), Color.white, new Vector2(1f, 1f), 0.25f);
        var scrap = Mat("Scrap", new Color(0.55f, 0.35f, 0.3f));
        var metal = MatTex("MetalBrushed", TexMetal(), new Color(0.78f, 0.82f, 0.88f), new Vector2(2f, 2f), 0.55f, 0.6f);
        BuildGreenfieldWorld(out var collection, out var delivery, out var lamps, out var cabinet);
        BuildPlayerAndRuntime(scene, GreenfieldScenePath, crateM, scrap, metal, collection, delivery, new Vector3(0f, 1.1f, -9f));
    }

    /// <summary>Greenfield scene + Linux player (fast): used for screenshots (-gfshots) and server tests.</summary>
    public static void BuildLinuxShotsGreenfield()
    {
        SetupRenderPipeline();
        SetupPlayerSettings();
        BuildGreenfieldScene();
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { GreenfieldScenePath },
            locationPathName = "Builds/ServerGF/wbi-server.x86_64",
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        };
        PlayerSettings.insecureHttpOption = UnityEditor.InsecureHttpOption.AlwaysAllowed;
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[WBI] Greenfield Linux build: " + report.summary.result + ", " + (report.summary.totalSize / (1024 * 1024)) + " MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}

public static partial class ProjectSetup
{
    /// <summary>
    /// A pallet must be liftable: the kit's single solid box left no room for the forks. Three runners and a deck (with real openings between them)
    /// replace it, and the model is made 1.45x taller so the openings (about 15 cm) clear the fork tines (7 cm, lowest position 4 cm).
    /// </summary>
    static void PalletShape(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (var x in new[] { -0.54f, 0f, 0.54f })
        {
            var bc = go.AddComponent<BoxCollider>();
            bc.center = new Vector3(x, 0.055f, 0f); bc.size = new Vector3(0.12f, 0.10f, 0.8f);
        }
        var deck = go.AddComponent<BoxCollider>();
        deck.center = new Vector3(0f, 0.117f, 0f); deck.size = new Vector3(1.2f, 0.024f, 0.8f);
        go.transform.localScale = new Vector3(1f, 1.45f, 1f);
    }
}
