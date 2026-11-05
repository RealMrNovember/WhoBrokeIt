using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using WhoBrokeIt.Interaction;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// Prefab-agnostic setup for breakable things, for scene builders (Editor) and for runtime spawners.
    /// Idempotent: calling it on an object that already has some of the components only adds what is missing.
    /// </summary>
    public static class Destructibles
    {
        /// <summary>
        /// Makes 'go' a networked breakable. mass &gt; 0 gives it a dynamic Rigidbody (mass x material multiplier) with NetworkTransform + NetworkRigidbody,
        /// Resettable and sensible physics (friction material, continuous detection, early sleep so stacks settle). mass = 0 leaves it static
        /// (fence panels, doors, panels). carryable adds the [E] pick-up. repairable adds the repair prompt zone + Repairable.
        /// </summary>
        public static Destructible Make(GameObject go, MaterialKind m, float strength, GameObject brokenPrefab = null,
            float mass = 0f, bool carryable = false, bool repairable = true, int maxChunks = 8, float repairSeconds = 3f, string requiredKind = "")
        {
            return MakeCore(go, m, strength, brokenPrefab, mass, false, carryable, repairable, maxChunks, repairSeconds, requiredKind, true);
        }

        /// <summary>
        /// Kit-manifest flavoured overload: materialTag ("wood", "metal", ...) and the REAL mass in kg (massKg &lt;= 0 = static, no rigidbody).
        /// strength &lt;= 0 picks MaterialTable.DefaultStrength(kind, massKg). breakable=false keeps it indestructible (walls, heavy things).
        /// </summary>
        public static Destructible Make(GameObject go, string materialTag, float massKg, GameObject brokenPrefab = null,
            bool carryable = false, bool repairable = true, float strength = 0f, bool breakable = true, int maxChunks = 8)
        {
            var m = MaterialTable.Parse(materialTag);
            var d = MakeCore(go, m, strength > 0f ? strength : MaterialTable.DefaultStrength(m, massKg > 0f ? massKg : 25f), brokenPrefab, massKg, true, carryable, repairable && breakable, maxChunks, 3f, "", breakable);
            return d;
        }

        static Destructible MakeCore(GameObject go, MaterialKind m, float strength, GameObject brokenPrefab, float mass, bool exactMass,
            bool carryable, bool repairable, int maxChunks, float repairSeconds, string requiredKind, bool breakable)
        {
            var info = MaterialTable.Get(m);
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (mass > 0f)
            {
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.mass = exactMass ? mass : mass * info.massMul;
                rb.linearDamping = 0.3f; rb.angularDamping = 0.6f;
            }
            if (rb != null)
            {
                rb.sleepThreshold = 0.1f;                                           // stacks settle and stop simulating early
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // fast thrown props do not tunnel through walls
            }
            if (go.GetComponent<NetworkObject>() == null) go.AddComponent<NetworkObject>();
            if (rb != null)
            {
                var nt = go.GetComponent<NetworkTransform>();
                if (nt == null) nt = go.AddComponent<NetworkTransform>();
                nt.SyncScaleX = nt.SyncScaleY = nt.SyncScaleZ = false;
                nt.Interpolate = true;
                if (go.GetComponent<NetworkRigidbody>() == null) go.AddComponent<NetworkRigidbody>();
                if (go.GetComponent<Resettable>() == null) go.AddComponent<Resettable>();
                if (carryable && go.GetComponent<Carryable>() == null) go.AddComponent<Carryable>().prompt = "Al";
            }

            var d = go.GetComponent<Destructible>();
            if (d == null) d = go.AddComponent<Destructible>();
            d.material = m; d.strength = strength; d.brokenPrefab = brokenPrefab; d.maxChunks = maxChunks; d.breakable = breakable;

            var ims = go.GetComponent<ImpactSounds>();
            if (ims == null) ims = go.AddComponent<ImpactSounds>();
            ims.material = m;

            ApplyPhysicsMaterial(go);

            if (repairable)
            {
                var rep = go.GetComponent<Repairable>();
                if (rep == null) rep = go.AddComponent<Repairable>();
                rep.seconds = repairSeconds; rep.requiredKind = requiredKind;
                if (rep.zone == null)
                {
                    // The repair trigger is a disabled collider on the root (no child object): it is switched on while Broken (Repairable.OnState) so the
                    // interaction search finds the broken thing. The prompt comes from Carryable (it knows the broken state) or from a RepairPrompt on the root.
                    // (Scene corruption "level0 is corrupted" was caused by RepairPrompt living in Repairable.cs: every scene component needs its own same-named file.)
                    var rend = go.GetComponentsInChildren<Renderer>();
                    var bounds = new Bounds(go.transform.position, Vector3.one * 0.6f);
                    var has = false;
                    foreach (var r in rend) { if (!has) { bounds = r.bounds; has = true; } else bounds.Encapsulate(r.bounds); }
                    var bc = go.AddComponent<BoxCollider>();
                    bc.isTrigger = true;
                    bc.center = go.transform.InverseTransformPoint(bounds.center);
                    var ls = go.transform.lossyScale;
                    bc.size = new Vector3(bounds.size.x / Mathf.Max(0.01f, ls.x) + 0.5f, bounds.size.y / Mathf.Max(0.01f, ls.y) + 0.3f, bounds.size.z / Mathf.Max(0.01f, ls.z) + 0.5f);
                    bc.enabled = false;
                    rep.zone = bc;
                    if (go.GetComponent<Interactable>() == null) go.AddComponent<RepairPrompt>();
                }
            }
            d.CacheParts();
            return d;
        }

        /// <summary>Adds the panel behaviour on top of Make (the object should be static or light).</summary>
        public static ElectricalPanel MakePanel(GameObject go, Light[] lights, Renderer indicator, float strength = 90f, GameObject brokenPrefab = null)
        {
            var d = Make(go, MaterialKind.Metal, strength, brokenPrefab, 0f, false, true, 6, 3.5f);
            var p = go.GetComponent<ElectricalPanel>();
            if (p == null) p = go.AddComponent<ElectricalPanel>();
            p.lights = lights; p.indicator = indicator;
            return p;
        }

        static void ApplyPhysicsMaterial(GameObject go)
        {
#if UNITY_EDITOR
            const string path = "Assets/Materials/WBI_Prop.physicMaterial";
            var pm = UnityEditor.AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (pm == null)
            {
                System.IO.Directory.CreateDirectory("Assets/Materials");
                pm = new PhysicsMaterial("WBI_Prop") { dynamicFriction = 0.6f, staticFriction = 0.7f, bounciness = 0.05f, frictionCombine = PhysicsMaterialCombine.Average, bounceCombine = PhysicsMaterialCombine.Minimum };
                UnityEditor.AssetDatabase.CreateAsset(pm, path);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>()) if (!c.isTrigger) c.sharedMaterial = pm;
#endif
        }
    }
}
