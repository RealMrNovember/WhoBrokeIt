using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using WhoBrokeIt.Audio;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// A breakable thing. The SERVER measures hits (relative speed + impulse, forklift rams count extra) and owns the hit points;
    /// peers only receive the meaningful state: a NetworkVariable byte (Intact 0, Cracked 1, Broken 2, so late joiners see the right thing)
    /// and ONE small ClientRpc per state change for the spectacle (debris, dust, sound). Debris is client-local and pooled, never networked.
    /// Repair (see Repairable) brings the object back to Intact.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class Destructible : NetworkBehaviour
    {
        public const byte Intact = 0, Cracked = 1, Broken = 2;

        public MaterialKind material = MaterialKind.Wood;
        [Tooltip("Hit points in impulse units. Rule of thumb: 6-8 x the mass for wood (a 4 kg crate: ~30; a thrown crate hits for 15-60).")]
        public float strength = 30f;
        [Tooltip("Optional pre-fractured chunk prefab (children with meshes). Without it the object breaks into procedural box chunks.")]
        public GameObject brokenPrefab;
        public int maxChunks = 8;
        public bool breakable = true;

        public NetworkVariable<byte> state = new NetworkVariable<byte>(Intact);

        /// <summary>Raised on every peer after the visible state changed (previous, new).</summary>
        public event System.Action<byte, byte> StateChanged;

        // test/diagnostic counters (per process)
        public static int FxCrack, FxBreak, FxRepair;
        public static float LastBreakAt = -1f;

        float hp;
        Renderer[] renderers = new Renderer[0];
        Collider[] colliders = new Collider[0];
        Rigidbody rb;
        NetworkRigidbody nrb;
        MaterialPropertyBlock mpb;
        float graceUntil;
        CarrySystem holder;
        float lastHitLog;

        public float HpFraction => strength > 0f ? Mathf.Clamp01(hp / strength) : 1f;
        public bool IsBroken => state.Value == Broken;
        public Bounds WorldBounds
        {
            get
            {
                var has = false; var b = new Bounds(transform.position, Vector3.one * 0.5f);
                foreach (var r in renderers) { if (r == null) continue; if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds); }
                if (!has) foreach (var c in colliders) { if (c == null) continue; if (!has) { b = c.bounds; has = true; } else b.Encapsulate(c.bounds); }
                return b;
            }
        }

        void Awake() => CacheParts();

        /// <summary>Called again by Destructibles.Make after it added colliders/children.</summary>
        public void CacheParts()
        {
            rb = GetComponent<Rigidbody>();
            nrb = GetComponent<NetworkRigidbody>();
            var rl = new List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer)) rl.Add(r);
            renderers = rl.ToArray();
            var cl = new List<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>(true))
                if (!c.isTrigger) cl.Add(c);
            colliders = cl.ToArray();
        }

        public override void OnNetworkSpawn()
        {
            hp = strength;
            if (IsServer) graceUntil = Time.time + 4f;          // the world is still settling (overlap push-outs, first drop): no damage yet
            state.OnValueChanged += OnState;
            if (IsServer)
            {
                var res = GetComponent<Resettable>();
                if (res != null) res.onReset += ServerRestore;
            }
            Apply(state.Value, state.Value);                    // late joiner: show whatever the state is right now (no debris, no sound)
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= OnState;
        }

        void OnState(byte prev, byte now) => Apply(prev, now);

        // ---------- server: taking damage ----------
        public void ServerSetHeld(bool held, CarrySystem by) { holder = held ? by : null; }
        public void ServerGrace(float seconds) { graceUntil = Time.time + seconds; }

        void OnCollisionEnter(Collision col)
        {
            if (!IsServer || !IsSpawned || !breakable || state.Value == Broken) return;
            if (holder != null || Time.time < graceUntil) return;            // being carried or just dropped from the hands: safe
            if (PlayerNet.FromCollider(col.collider) != null) return;       // players bonk, they do not break things
            if (col.rigidbody != null && CarrySystem.IsSafeBody(col.rigidbody)) return;   // something being carried or just put down does not smash its neighbours
            var info = MaterialTable.Get(material);
            var v = col.relativeVelocity.magnitude;
            if (v < info.minSpeed) return;
            var own = rb != null ? Mathf.Max(0.1f, rb.mass) : 20f;
            var other = col.rigidbody;
            var mult = other != null ? Mathf.Clamp(Mathf.Sqrt(other.mass / own), 1f, 4f) : 1f;   // a forklift ram hits harder than a bump
            var damage = Mathf.Max(col.impulse.magnitude, own * v * 0.5f) * mult;
            var c0 = col.contactCount > 0 ? col.GetContact(0) : default;
            if (Time.time - lastHitLog > 0.2f) { lastHitLog = Time.time; Debug.Log("[DESTR] hit " + name + " v=" + v.ToString("0.0") + " impulse=" + col.impulse.magnitude.ToString("0.0") + " dmg=" + damage.ToString("0.0") + " hp=" + hp.ToString("0.0") + " other=" + col.collider.name); }
            ServerDamage(damage, col.contactCount > 0 ? c0.point : transform.position, col.contactCount > 0 ? c0.normal : Vector3.up);
        }

        /// <summary>Server only. Public so scripted events (explosions, falling loads) can hurt things too.</summary>
        public void ServerDamage(float damage, Vector3 point, Vector3 normal)
        {
            if (!IsServer || !IsSpawned || !breakable || state.Value == Broken) return;
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return;
            hp -= damage;
            byte next = hp <= 0f ? Broken : (hp <= strength * 0.55f ? Cracked : Intact);
            if (next < state.Value) next = state.Value;                       // never heals by itself
            if (next == state.Value) return;
            var mag = (byte)Mathf.Clamp(damage / Mathf.Max(1f, strength) * 100f, 1f, 255f);
            Debug.Log("[DESTR] " + name + " state " + state.Value + "->" + next + " damage=" + damage.ToString("0") + " hp=" + hp.ToString("0"));
            state.Value = next;
            if (next == Broken) ServerOnBroken();
            FxRpc(point, normal, (byte)material, mag, next == Broken ? (byte)2 : (byte)1);
        }

        void ServerOnBroken()
        {
            if (holder != null) holder.ServerDrop();                          // whoever carries it drops it
            SetKinematic(true);
            // things that were resting on it fall
            var b = WorldBounds; b.Expand(0.3f);
            foreach (var c in Physics.OverlapBox(b.center + Vector3.up * 0.2f, b.extents + Vector3.up * 0.2f))
                if (c.attachedRigidbody != null) c.attachedRigidbody.WakeUp();
        }

        /// <summary>Server: back to a whole object (repaired, reset with X, or scripted).</summary>
        public void ServerRestore()
        {
            if (!IsServer || !IsSpawned) return;
            var was = state.Value;
            hp = strength;
            if (was == Intact) return;
            state.Value = Intact;
            graceUntil = Time.time + 1.5f;                      // a repaired thing may still have to drop to the ground: that is not a hit
            SetKinematic(false);
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.WakeUp(); }
            Debug.Log("[DESTR] " + name + " state " + was + "->0 (restored)");
            FxRpc(transform.position, Vector3.up, (byte)material, 40, 3);
        }

        void SetKinematic(bool k)
        {
            if (rb == null) return;
            if (nrb != null) nrb.SetIsKinematic(k); else rb.isKinematic = k;
        }

        // ---------- every peer: what the state looks like ----------
        void Apply(byte prev, byte now)
        {
            var broken = now == Broken;
            foreach (var r in renderers) if (r != null) r.enabled = !broken;
            foreach (var c in colliders) if (c != null) c.enabled = !broken;
            if (now == Cracked) Tint(0.72f);
            else Tint(1f);
            if (prev != now && StateChanged != null) StateChanged(prev, now);
        }

        void Tint(float k)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (Mathf.Approximately(k, 1f)) { r.SetPropertyBlock(null); continue; }
                var src = r.sharedMaterial;
                var col = Color.white;
                if (src != null) { if (src.HasProperty("_BaseColor")) col = src.GetColor("_BaseColor"); else if (src.HasProperty("_Color")) col = src.GetColor("_Color"); }
                var c = new Color(col.r * k, col.g * k, col.b * k, col.a);
                mpb.Clear();
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                r.SetPropertyBlock(mpb);
            }
        }

        // ---------- the spectacle (one small RPC per state change) ----------
        [Rpc(SendTo.ClientsAndHost)]
        void FxRpc(Vector3 pos, Vector3 normal, byte mat, byte magnitude, byte kind)
        {
            if (kind == 1) FxCrack++; else if (kind == 2) { FxBreak++; LastBreakAt = Time.time; } else if (kind == 3) FxRepair++;
            if (Net.DedicatedServer.IsDedicated) return;
            if (!IsFinite(pos) || !IsFinite(normal)) return;
            var info = MaterialTable.Get((MaterialKind)Mathf.Min(mat, (byte)6));
            var power = Mathf.Clamp01(magnitude / 120f);
            if (kind == 1)
            {
                GameAudio.Play(Sfx.Creak, pos, 0.7f, Random.Range(0.9f, 1.1f));
                GameAudio.Play(info.hit, pos, 0.8f, Random.Range(0.9f, 1.1f));
                DebrisPool.Puff(pos, info.color, 6, 1.6f);
            }
            else if (kind == 2)
            {
                var b = WorldBounds;
                var away = normal.sqrMagnitude > 0.01f ? normal : Vector3.up;
                var n = Mathf.Clamp(Mathf.Min(maxChunks, info.debris + (int)(power * 4f)), 1, 24);
                if (brokenPrefab != null) DebrisPool.SpawnPrefab(brokenPrefab, transform.position, transform.rotation, away, 3f + power * 3f);
                else DebrisPool.SpawnBoxes(b, transform.rotation, n, info.chunk, info.color, away, 3f + power * 3f);
                DebrisPool.Puff(b.center, info.color, 14, 2.4f, 2.2f, 0.09f, 0.9f);
                GameAudio.Play(info.smash, pos, 1f, Random.Range(0.92f, 1.08f) * (rb != null ? Mathf.Clamp(1.4f - 0.12f * Mathf.Log(rb.mass + 1f, 2f), 0.7f, 1.3f) : 0.8f));
                var cam = ThirdPersonCamera.Instance;
                if (cam != null) ThirdPersonCamera.AddTrauma(Mathf.Clamp01(0.5f * power) * Mathf.Clamp01(1f - Vector3.Distance(cam.transform.position, pos) / 25f));
            }
            else if (kind == 3)
            {
                GameAudio.Play(Sfx.Pickup, pos, 0.9f, 0.8f);
                DebrisPool.Puff(pos + Vector3.up * 0.3f, new Color(0.5f, 1f, 0.7f), 12, 1.8f, 2.4f, 0.06f, 0.8f);
            }
        }

        static bool IsFinite(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
    }
}
