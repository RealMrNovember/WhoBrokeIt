using System.Collections.Generic;
using UnityEngine;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// Client-local debris and dust (never networked): a capped pool of physics chunks (128) with a 6-10 s life that shrink out,
    /// plus a small pool of physics-free "puff" cubes used as cheap particles (dust, sparks, repair glitter).
    /// Dedicated servers never spawn anything (nothing is rendered there).
    /// </summary>
    public static class DebrisPool
    {
        public const int Cap = 128;
        const int PuffCap = 96;
        const int DebrisLayer = 31;

        class Chunk
        {
            public GameObject go; public Transform t; public Rigidbody rb; public float born, life; public Vector3 baseScale; public bool owned, used, frozen;
        }
        class Spark { public Transform t; public Vector3 v; public float born, life; public Vector3 baseScale; public bool used; }

        static readonly List<Chunk> chunks = new List<Chunk>();
        static readonly List<Spark> puffs = new List<Spark>();
        static Transform root;
        static Material baseMat;
        static MaterialPropertyBlock mpb;

        /// <summary>Counters read by the automated tests.</summary>
        public static int TotalSpawned { get; private set; }
        public static int TotalPuffs { get; private set; }
        public static int Active { get { var n = 0; foreach (var c in chunks) if (c.used) n++; return n; } }

        public static bool Enabled => !Net.DedicatedServer.IsDedicated;

        class Runner : MonoBehaviour { void Update() { Tick(); } }

        static void Ensure()
        {
            if (root != null) return;
            var go = new GameObject("DebrisPool");
            Object.DontDestroyOnLoad(go);
            root = go.transform;
            go.AddComponent<Runner>();
            mpb = new MaterialPropertyBlock();
            baseMat = Resources.Load<Material>("WBI/CharacterBase");
            if (baseMat == null) baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Physics.IgnoreLayerCollision(DebrisLayer, DebrisLayer, true);   // chunks do not collide with each other (cheap and stable)
        }

        static void Paint(Renderer r, Color c)
        {
            mpb.Clear();
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            r.SetPropertyBlock(mpb);
        }

        static Chunk Acquire()
        {
            Ensure();
            foreach (var c in chunks) if (!c.used && c.go != null && !c.owned) return c;
            if (chunks.Count < Cap)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Chunk";
                go.layer = DebrisLayer;
                go.transform.SetParent(root, false);
                go.GetComponent<Renderer>().sharedMaterial = baseMat;
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.4f; rb.linearDamping = 0.15f; rb.angularDamping = 0.1f;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                var ch = new Chunk { go = go, t = go.transform, rb = rb };
                chunks.Add(ch);
                return ch;
            }
            // pool is full: recycle the oldest live chunk
            Chunk oldest = null;
            foreach (var c in chunks) if (c.used && !c.owned && (oldest == null || c.born < oldest.born)) oldest = c;
            if (oldest != null) return oldest;
            return null;
        }

        static void Launch(Chunk c, Vector3 pos, Quaternion rot, Vector3 scale, Color color, Vector3 vel, Vector3 spin, float life)
        {
            c.used = true; c.frozen = false;
            c.born = Time.time; c.life = life; c.baseScale = scale;
            c.go.SetActive(true);
            c.t.SetPositionAndRotation(pos, rot);
            c.t.localScale = scale;
            if (c.rb != null)
            {
                c.rb.isKinematic = false;
                c.rb.linearVelocity = vel;
                c.rb.angularVelocity = spin;
            }
            var r = c.go.GetComponent<Renderer>();
            if (r != null && !c.owned) Paint(r, color);
            TotalSpawned++;
        }

        /// <summary>Procedural fracture: 'count' box chunks distributed through the (world) bounds, flung away from the hit.</summary>
        public static void SpawnBoxes(Bounds b, Quaternion rot, int count, float chunkSize, Color color, Vector3 away, float speed)
        {
            if (!Enabled || count <= 0) return;
            var life0 = Random.Range(6f, 10f);
            for (var i = 0; i < count; i++)
            {
                var c = Acquire();
                if (c == null) return;
                var p = b.center + new Vector3(Random.Range(-0.45f, 0.45f) * b.size.x, Random.Range(-0.4f, 0.4f) * b.size.y, Random.Range(-0.45f, 0.45f) * b.size.z);
                var cell = Mathf.Max(0.06f, Mathf.Min(chunkSize, Mathf.Max(b.size.x, b.size.y, b.size.z) * 0.45f));
                var scale = new Vector3(cell * Random.Range(0.6f, 1.2f), cell * Random.Range(0.35f, 0.9f), cell * Random.Range(0.6f, 1.2f));
                var dir = (p - b.center).normalized * 0.6f + away.normalized * 0.8f + Vector3.up * 0.5f;
                var tint = Color.Lerp(color, color * 0.7f, Random.value);
                Launch(c, p, rot * Random.rotation, scale, new Color(tint.r, tint.g, tint.b, 1f), dir.normalized * speed * Random.Range(0.5f, 1.2f), Random.insideUnitSphere * 8f, life0 + Random.Range(-1f, 1f));
            }
        }

        /// <summary>Pre-fractured prefab: every child mesh becomes an independent chunk (counted against the cap, destroyed at end of life).</summary>
        public static void SpawnPrefab(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 away, float speed)
        {
            if (!Enabled || prefab == null) return;
            Ensure();
            var inst = Object.Instantiate(prefab, pos, rot);
            var mfs = inst.GetComponentsInChildren<MeshFilter>();
            var life0 = Random.Range(6f, 10f);
            foreach (var mf in mfs)
            {
                // keep the cap: recycle the oldest owned chunk if needed
                if (chunks.Count >= Cap)
                {
                    Chunk oldest = null;
                    foreach (var o in chunks) if (o.used && (oldest == null || o.born < oldest.born)) oldest = o;
                    if (oldest != null) Retire(oldest, true);
                }
                var go = mf.gameObject;
                go.transform.SetParent(root, true);
                go.layer = DebrisLayer;
                var col = go.GetComponent<Collider>();
                if (col == null) { var bc = go.AddComponent<BoxCollider>(); bc.size = mf.sharedMesh != null ? mf.sharedMesh.bounds.size : Vector3.one * 0.2f; bc.center = mf.sharedMesh != null ? mf.sharedMesh.bounds.center : Vector3.zero; }
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null) rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.4f; rb.linearDamping = 0.15f;
                var ch = new Chunk { go = go, t = go.transform, rb = rb, owned = true };
                chunks.Add(ch);
                var dir = (go.transform.position - pos).normalized * 0.6f + away.normalized * 0.8f + Vector3.up * 0.5f;
                Launch(ch, go.transform.position, go.transform.rotation, go.transform.localScale, Color.white, dir.normalized * speed * Random.Range(0.5f, 1.2f), Random.insideUnitSphere * 6f, life0 + Random.Range(-1f, 1f));
            }
            Object.Destroy(inst);
        }

        static void Retire(Chunk c, bool remove)
        {
            c.used = false;
            if (c.owned)
            {
                if (c.go != null) Object.Destroy(c.go);
                chunks.Remove(c);
            }
            else if (c.go != null) { c.go.SetActive(false); }
        }

        /// <summary>Cheap physics-free particles: tiny cubes that fly, fall and shrink (dust, sparks, glitter).</summary>
        public static void Puff(Vector3 pos, Color color, int count, float speed, float up = 1.5f, float size = 0.07f, float life = 0.7f)
        {
            if (!Enabled) return;
            Ensure();
            for (var i = 0; i < count; i++)
            {
                Spark p = null;
                foreach (var q in puffs) if (!q.used) { p = q; break; }
                if (p == null)
                {
                    if (puffs.Count >= PuffCap) { p = puffs[0]; foreach (var q in puffs) if (q.born < p.born) p = q; }
                    else
                    {
                        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(go.GetComponent<Collider>());
                        go.name = "Puff";
                        go.layer = DebrisLayer;
                        go.transform.SetParent(root, false);
                        go.GetComponent<Renderer>().sharedMaterial = baseMat;
                        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        p = new Spark { t = go.transform };
                        puffs.Add(p);
                    }
                }
                p.used = true; p.born = Time.time; p.life = life * Random.Range(0.7f, 1.2f);
                p.baseScale = Vector3.one * size * Random.Range(0.6f, 1.4f);
                p.v = Random.onUnitSphere * speed * Random.Range(0.4f, 1f) + Vector3.up * up;
                p.t.gameObject.SetActive(true);
                p.t.position = pos + Random.insideUnitSphere * 0.1f;
                p.t.rotation = Random.rotation;
                p.t.localScale = p.baseScale;
                Paint(p.t.GetComponent<Renderer>(), color);
                TotalPuffs++;
            }
        }

        static void Tick()
        {
            var now = Time.time;
            for (var i = chunks.Count - 1; i >= 0; i--)
            {
                var c = chunks[i];
                if (!c.used) continue;
                if (c.go == null) { chunks.RemoveAt(i); continue; }
                var age = now - c.born;
                if (age >= c.life) { Retire(c, false); continue; }
                var left = c.life - age;
                if (left < 1.2f) c.t.localScale = c.baseScale * Mathf.Clamp01(left / 1.2f);
                if (!c.frozen && age > 2.5f && c.rb != null && !c.rb.isKinematic && c.rb.linearVelocity.sqrMagnitude < 0.01f) { c.rb.isKinematic = true; c.frozen = true; }   // settled: stop simulating it
            }
            foreach (var p in puffs)
            {
                if (!p.used) continue;
                var age = now - p.born;
                if (age >= p.life) { p.used = false; p.t.gameObject.SetActive(false); continue; }
                p.v += Vector3.down * 9f * Time.deltaTime;
                p.t.position += p.v * Time.deltaTime;
                p.t.localScale = p.baseScale * (1f - age / p.life);
            }
        }
    }
}
