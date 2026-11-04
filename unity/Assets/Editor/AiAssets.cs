using System.IO;
using UnityEditor;
using UnityEngine;
using WhoBrokeIt.Player;

/// <summary>
/// Places the AI-generated models (SDXL concept -> TRELLIS mesh, see tools/gpu) into the scene and the character prefabs.
/// The .glb files live in Assets/ThirdParty/Generated/&lt;id&gt;.glb (imported by glTFast). When a file is missing the caller falls back
/// to the hand-built primitives, so the game always builds.
/// </summary>
public static class AiAssets
{
    public const string Dir = "Assets/ThirdParty/Generated/";

    /// <summary>Extra rotation so every generated character ends up facing +Z. Adjust after looking at the screenshots.</summary>
    public static float CharacterYaw = 0f;

    public static bool Has(string id)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(Dir + id + ".glb") != null;
    }

    static Bounds WorldBounds(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(root.transform.position, Vector3.one * 0.1f);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    /// <summary>
    /// Returns an object at the origin whose model is scaled to 'size' (height, or the largest dimension), standing on y=0 and
    /// centred on x/z. The caller moves/rotates the returned object freely (it has unit scale).
    /// </summary>
    public static GameObject Model(string id, float size, bool byHeight, float yaw = 0f, bool collider = true)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + id + ".glb");
        if (asset == null) return null;
        var wrap = new GameObject("Model_" + id);
        var inst = Object.Instantiate(asset, wrap.transform);
        inst.name = id;
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        var b = WorldBounds(wrap);
        var dim = byHeight ? b.size.y : Mathf.Max(b.size.x, b.size.y, b.size.z);
        inst.transform.localScale *= size / Mathf.Max(dim, 0.0001f);
        b = WorldBounds(wrap);
        inst.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
        b = WorldBounds(wrap);

        if (collider)
        {
            var bc = wrap.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, b.size.y * 0.5f, 0f);
            bc.size = b.size;
        }
        return wrap;
    }

    /// <summary>Character models: Resources/Characters/&lt;key&gt;.prefab (the runtime catalog prefers these over the generated primitives).</summary>
    public static int BuildCharacterPrefabs()
    {
        Directory.CreateDirectory("Assets/Resources/Characters");
        var made = 0;
        foreach (var entry in CharacterCatalog.All)
        {
            var path = "Assets/Resources/Characters/" + entry.key + ".prefab";
            var model = Model("char_" + entry.key, 1.8f, true, CharacterYaw, false);
            if (model == null)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
                continue;
            }
            model.name = "Character_" + entry.key;
            PrefabUtility.SaveAsPrefabAsset(model, path);
            Object.DestroyImmediate(model);
            made++;
        }
        AssetDatabase.SaveAssets();
        return made;
    }
}
