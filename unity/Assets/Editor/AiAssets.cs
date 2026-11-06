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

    /// <summary>Where the limbs of one generated character are (metres, character 1.8 m tall, facing +Z).</summary>
    struct RigSpec
    {
        public float hipY, armX, shoulderX, shoulderY, armTop, armBottom, legSwing, armSwing;
        public RigSpec(float hipY, float armX, float shoulderX, float shoulderY, float armTop, float armBottom, float legSwing = 36f, float armSwing = 32f)
        { this.hipY = hipY; this.armX = armX; this.shoulderX = shoulderX; this.shoulderY = shoulderY; this.armTop = armTop; this.armBottom = armBottom; this.legSwing = legSwing; this.armSwing = armSwing; }
    }

    static RigSpec SpecFor(string key)
    {
        switch (key)
        {
            case "muhendis": return new RigSpec(0.84f, 0.23f, 0.24f, 1.35f, 1.45f, 0.72f);
            case "asci":     return new RigSpec(0.55f, 0.20f, 0.22f, 0.95f, 1.05f, 0.45f, 30f, 40f);
            case "robot":    return new RigSpec(0.58f, 0.27f, 0.30f, 0.95f, 1.10f, 0.45f, 14f, 30f);
            case "kedi":     return new RigSpec(0.62f, 0.22f, 0.25f, 1.10f, 1.25f, 0.55f, 30f, 28f);
            default:         return new RigSpec(0.86f, 0.22f, 0.22f, 1.38f, 1.45f, 0.70f);   // isci
        }
    }

    static float Smooth(float a, float b, float x)
    {
        var t = Mathf.Clamp01((x - a) / Mathf.Max(b - a, 0.0001f));
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// The generated models are single static meshes. This adds a small skeleton (hips, two legs, two arms) and skins the mesh by region
    /// (below the hip = legs, split left/right; to the sides of the torso = arms) so CharacterAnimator can swing the limbs.
    /// Returns false (model left untouched) when the mesh cannot be read.
    /// </summary>
    static bool Rig(GameObject wrap, string key)
    {
        var spec = SpecFor(key);
        var original = wrap.transform.GetChild(0).gameObject;
        var filters = wrap.GetComponentsInChildren<MeshFilter>();
        if (filters.Length == 0) return false;
        foreach (var f in filters) if (f.sharedMesh == null || !f.sharedMesh.isReadable) { Debug.Log("[WBI] rig: mesh okunamiyor: " + key); return false; }

        var hips = new GameObject("Hips").transform;
        hips.SetParent(wrap.transform, false);
        hips.localPosition = new Vector3(0f, spec.hipY, 0f);
        Transform Bone(string name, Vector3 pos)
        {
            var b = new GameObject(name).transform;
            b.SetParent(hips, false);
            b.position = wrap.transform.TransformPoint(pos);
            return b;
        }
        var legL = Bone("LegL", new Vector3(-0.09f, spec.hipY, 0f));
        var legR = Bone("LegR", new Vector3(0.09f, spec.hipY, 0f));
        var armL = Bone("ArmL", new Vector3(-spec.shoulderX, spec.shoulderY, 0f));
        var armR = Bone("ArmR", new Vector3(spec.shoulderX, spec.shoulderY, 0f));
        var bones = new[] { hips, legL, legR, armL, armR };
        var bind = new Matrix4x4[bones.Length];
        for (var i = 0; i < bones.Length; i++) bind[i] = bones[i].worldToLocalMatrix * wrap.transform.localToWorldMatrix;

        Directory.CreateDirectory("Assets/Resources/Characters/Meshes");
        var n = 0;
        foreach (var f in filters)
        {
            var r = f.GetComponent<MeshRenderer>();
            if (r == null) continue;
            var src = f.sharedMesh;
            var m = Object.Instantiate(src);
            m.name = key + "_skin" + n;
            var v = src.vertices;
            var nrm = src.normals;
            var w = new BoneWeight[v.Length];
            for (var i = 0; i < v.Length; i++)
            {
                var p = wrap.transform.InverseTransformPoint(f.transform.TransformPoint(v[i]));
                v[i] = p;
                if (nrm.Length == v.Length) nrm[i] = wrap.transform.InverseTransformDirection(f.transform.TransformDirection(nrm[i])).normalized;

                var wLeg = 1f - Smooth(spec.hipY - 0.07f, spec.hipY + 0.07f, p.y);
                var sideR = Smooth(-0.035f, 0.035f, p.x);
                var wArm = Smooth(spec.armX - 0.015f, spec.armX + 0.03f, Mathf.Abs(p.x))
                         * Smooth(spec.armBottom - 0.05f, spec.armBottom + 0.05f, p.y)
                         * (1f - Smooth(spec.shoulderY - 0.01f, spec.shoulderY + 0.035f, p.y)) * (1f - wLeg);   // narrow blends: the arm moves as one piece, the shoulder cap barely follows it
                var legIdx = p.x >= 0f ? 2 : 1;
                var armIdx = p.x >= 0f ? 4 : 3;
                var legW = wLeg * (p.x >= 0f ? sideR : 1f - sideR);
                var legOther = wLeg - legW;                     // near the middle the other leg takes its share
                var bw = new BoneWeight();
                bw.boneIndex0 = 0; bw.weight0 = Mathf.Max(0f, 1f - wLeg - wArm);
                bw.boneIndex1 = legIdx; bw.weight1 = legW;
                bw.boneIndex2 = p.x >= 0f ? 1 : 2; bw.weight2 = legOther;
                bw.boneIndex3 = armIdx; bw.weight3 = wArm;
                w[i] = bw;
            }
            m.vertices = v;
            if (nrm.Length == v.Length) m.normals = nrm;
            m.tangents = null;
            m.boneWeights = w;
            m.bindposes = bind;
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, "Assets/Resources/Characters/Meshes/" + m.name + ".asset");

            var body = new GameObject("Body" + n);
            body.transform.SetParent(wrap.transform, false);
            var smr = body.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = m;
            smr.sharedMaterials = r.sharedMaterials;
            smr.bones = bones;
            smr.rootBone = hips;
            smr.localBounds = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(2.4f, 2.2f, 2.4f));
            smr.updateWhenOffscreen = false;
            n++;
        }
        // the static original is no longer needed
        Object.DestroyImmediate(original);

        var anim = wrap.AddComponent<WhoBrokeIt.Player.CharacterAnimator>();
        anim.hips = hips; anim.legL = legL; anim.legR = legR; anim.armL = armL; anim.armR = armR;
        anim.swingDegrees = spec.legSwing; anim.armSwingDegrees = spec.armSwing;
        return true;
    }


    /// <summary>Per-asset quality report for the build log (triangles, height, materials, biggest texture) with warnings over the budgets.</summary>
    static void QaReport(GameObject model, string key)
    {
        var tris = 0; var verts = 0; var mats = new System.Collections.Generic.HashSet<Material>(); var maxTex = 0;
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh != null) { tris += smr.sharedMesh.triangles.Length / 3; verts += smr.sharedMesh.vertexCount; }
            foreach (var m in smr.sharedMaterials) if (m != null) mats.Add(m);
        }
        foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null) { tris += mf.sharedMesh.triangles.Length / 3; verts += mf.sharedMesh.vertexCount; }
        foreach (var m in mats)
            if (m.mainTexture != null) maxTex = Mathf.Max(maxTex, Mathf.Max(m.mainTexture.width, m.mainTexture.height));
        var b = new Bounds(Vector3.zero, Vector3.zero); var first = true;
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            if (smr.sharedMesh != null) { if (first) { b = smr.sharedMesh.bounds; first = false; } else b.Encapsulate(smr.sharedMesh.bounds); }
        if (first) b = WorldBounds(model);
        var warn = "";
        if (tris > 120000) warn += " UYARI: ucgen butcesi asildi (>120k)";
        if (maxTex > 2048) warn += " UYARI: doku buyuk (>2048)";
        if (Mathf.Abs(b.size.y - 1.8f) > 0.15f) warn += " UYARI: boy 1.8 m degil";
        if (Mathf.Abs(b.min.y) > 0.05f) warn += " UYARI: ayaklar y=0 da degil";
        Debug.Log("[WBI] qa " + key + ": ucgen=" + tris + " tepe=" + verts + " boy=" + b.size.y.ToString("0.00") + " m malzeme=" + mats.Count + " enbuyuk_doku=" + maxTex + warn);
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
            Debug.Log("[WBI] iskelet " + entry.key + ": " + (Rig(model, entry.key) ? "tamam" : "yok (statik)"));
            QaReport(model, entry.key);
            PrefabUtility.SaveAsPrefabAsset(model, path);
            Object.DestroyImmediate(model);
            made++;
        }
        AssetDatabase.SaveAssets();
        return made;
    }
}
