using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Blender kit import validation (batchmode): every GLB under Assets/Art/Kit is loaded through the asset database (glTFast importer)
/// and checked: imported, meshes not null, triangle count, size/base against the expected scale, materials not null and within the slot limit.
/// Run: unity-editor -batchmode -nographics -projectPath ... -executeMethod KitImportValidator.Run -quit -logFile -
/// Log lines start with [KITCHECK]; exit code 1 when anything fails or nothing was found.
/// </summary>
public static class KitImportValidator
{
    const string Root = "Assets/Art/Kit";

    // expected sizes in Unity axes (x width, y height, z depth), tolerance in metres; checked only for these stems (not *_broken)
    struct Expect { public string prefix; public float x, y, z, tol; }
    static readonly Expect[] Expected =
    {
        new Expect { prefix = "bld_wall_", x = 3.0f, y = 3.0f, z = 0.3f, tol = 0.1f },
        new Expect { prefix = "bld_door_", x = 0.92f, y = 2.1f, z = -1f, tol = 0.1f },
        new Expect { prefix = "fnc_panel_a", x = 2.0f, y = 1.1f, z = -1f, tol = 0.1f },
        new Expect { prefix = "prop_pallet_", x = 1.2f, y = 0.13f, z = 0.8f, tol = 0.08f },
        new Expect { prefix = "prop_barrel_a", x = -1f, y = 0.9f, z = -1f, tol = 0.06f },
        new Expect { prefix = "prop_crate_a", x = 0.8f, y = 0.8f, z = 0.8f, tol = 0.06f },
        new Expect { prefix = "prop_crate_b", x = 0.5f, y = 0.5f, z = 0.5f, tol = 0.05f },
        new Expect { prefix = "prop_crate_c", x = 1.2f, y = 1.2f, z = 1.2f, tol = 0.06f },
        new Expect { prefix = "bld_clocktower_a", x = -1f, y = 14.0f, z = -1f, tol = 0.4f },
        new Expect { prefix = "bld_watertower_a", x = -1f, y = 12.0f, z = -1f, tol = 0.4f },
        new Expect { prefix = "prop_lamp_a", x = -1f, y = 4.0f, z = -1f, tol = 0.15f },
        new Expect { prefix = "prop_fountain_a", x = 6.0f, y = -1f, z = 6.0f, tol = 0.2f },
        new Expect { prefix = "bld_halfwall_a", x = 4.0f, y = 1.8f, z = -1f, tol = 0.2f },
        new Expect { prefix = "env_cobble_disc_a", x = 28.0f, y = -1f, z = 28.0f, tol = 0.3f },
        new Expect { prefix = "env_hill_a", x = 14.0f, y = 2.5f, z = 10.0f, tol = 0.3f },
    };

    static readonly string[] Facade = { "bld_shopfront", "bld_house", "bld_workshop", "bld_warehouse", "bld_shed", "bld_clocktower", "bld_watertower", "prop_fountain" };
    static readonly string[] TallProp = { "prop_lamp_a", "prop_tree", "prop_scaffold", "prop_rack", "prop_toolwall", "prop_stall", "prop_sign", "prop_cementmixer", "prop_planter", "prop_cart", "prop_clothesline" };

    static int SlotLimit(string stem)
    {
        foreach (var f in Facade) if (stem.StartsWith(f)) return 8;
        foreach (var t in TallProp) if (stem.StartsWith(t)) return 5;
        if (stem.StartsWith("env_")) return 5;
        if (stem.StartsWith("bld_halfwall")) return 3;
        if (stem.StartsWith("bld_building")) return 6;
        if (stem.StartsWith("bld_") || stem.StartsWith("fnc_")) return 3;
        if (stem.StartsWith("veh_")) return 6;
        if (stem.StartsWith("char_")) return 2;
        return 3;
    }

    public static void Run()
    {
        int ok = 0, fail = 0;
        if (!Directory.Exists(Root))
        {
            Debug.Log("[KITCHECK] FAIL " + Root + " yok");
            EditorApplication.Exit(1);
            return;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var files = new List<string>(Directory.GetFiles(Root, "*.glb", SearchOption.AllDirectories));
        files.Sort();
        foreach (var raw in files)
        {
            var path = raw.Replace('\\', '/');
            var stem = Path.GetFileNameWithoutExtension(path);
            var notes = new List<string>();
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null)
            {
                Debug.Log("[KITCHECK] FAIL " + stem + " ice aktarilamadi (GameObject yok)");
                fail++;
                continue;
            }
            long tris = 0;
            int cols = 0, bodies = 0;
            var mats = new HashSet<Material>();
            Bounds bounds = default;
            var haveBounds = false;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<Renderer>();
                if (mf.name.StartsWith("COL_")) { cols++; continue; }
                bodies++;
                var mesh = mf.sharedMesh;
                if (mesh == null) { notes.Add("bos mesh: " + mf.name); continue; }
                for (var s = 0; s < mesh.subMeshCount; s++) tris += mesh.GetIndexCount(s) / 3;
                if (r == null) { notes.Add("renderer yok: " + mf.name); continue; }
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) notes.Add("bos malzeme: " + mf.name);
                    else mats.Add(m);
                }
                var b = new Bounds(r.bounds.center, r.bounds.size);
                if (!haveBounds) { bounds = b; haveBounds = true; } else bounds.Encapsulate(b);
            }
            if (bodies == 0) notes.Add("govde mesh'i yok");
            if (mats.Count > SlotLimit(stem)) notes.Add(mats.Count + " malzeme > " + SlotLimit(stem));
            var size = haveBounds ? bounds.size : Vector3.zero;
            if (haveBounds)
            {
                if (Mathf.Abs(bounds.min.y) > 0.03f) notes.Add("taban y=" + bounds.min.y.ToString("0.000"));
                if (!stem.EndsWith("_broken"))
                    foreach (var e in Expected)
                    {
                        if (!stem.StartsWith(e.prefix)) continue;
                        if (e.x > 0 && Mathf.Abs(size.x - e.x) > e.tol) notes.Add("x=" + size.x.ToString("0.00") + " beklenen " + e.x);
                        if (e.y > 0 && Mathf.Abs(size.y - e.y) > e.tol) notes.Add("y=" + size.y.ToString("0.00") + " beklenen " + e.y);
                        if (e.z > 0 && Mathf.Abs(size.z - e.z) > e.tol) notes.Add("z=" + size.z.ToString("0.00") + " beklenen " + e.z);
                    }
            }
            var good = notes.Count == 0;
            if (good) ok++; else fail++;
            Debug.Log("[KITCHECK] " + (good ? "OK   " : "FAIL ") + stem + " tris=" + tris + " size=(" + size.x.ToString("0.00") + "," + size.y.ToString("0.00") + "," +
                      size.z.ToString("0.00") + ") mats=" + mats.Count + " bodies=" + bodies + " cols=" + cols + (good ? "" : "  " + string.Join("; ", notes)));
        }
        var pal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/WBI_Palette.png");
        Debug.Log("[KITCHECK] palet atlasi: " + (pal != null ? pal.width + "x" + pal.height : "YOK"));
        Debug.Log("[KITCHECK] SONUC ok=" + ok + " fail=" + fail);
        EditorApplication.Exit(fail == 0 && ok > 0 ? 0 : 1);
    }
}
