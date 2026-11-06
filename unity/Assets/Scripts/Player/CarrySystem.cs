using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Holds one prop in front of the player. Server-authoritative: the server places the held body at the hold point (in the hands) every physics step;
    /// every peer can read heldId to show the prompt and draws the object exactly in the carrier hands.
    /// Heavy props (above maxMass, up to PairMaxMass) need TWO players: the first one grips and waits, the second one joins, then both lift it together,
    /// slowed down, with the object between them. Letting go, or walking too far apart, drops it for both.
    /// </summary>
    public class CarrySystem : NetworkBehaviour
    {
        public Transform holdPoint;
        public float maxMass = 6f;
        public const float PairMaxMass = 30f;
        public NetworkVariable<ulong> heldId = new NetworkVariable<ulong>(ulong.MaxValue);

        Rigidbody held;
        Collider[] heldColliders, bodyColliders;
        bool wasGravity, ownsPhysics, physicsBegun;
        float oldDamping;
        float heldSince;

        public bool IsCarrying => heldId.Value != ulong.MaxValue;

        // Bodies that must not hurt breakable things right now: carried ones (they are pulled around at speed) and ones just let go of.
        // A throw clears it again (a throw IS a hit). Server only.
        static readonly Dictionary<int, float> safeUntil = new Dictionary<int, float>();
        public static bool IsSafeBody(Rigidbody rb) => rb != null && safeUntil.TryGetValue(rb.GetInstanceID(), out var t) && Time.time < t;

        // ---------- who holds what (works on every machine, rebuilt once per frame) ----------
        static readonly Dictionary<ulong, List<CarrySystem>> holders = new Dictionary<ulong, List<CarrySystem>>();
        static readonly List<CarrySystem> Empty = new List<CarrySystem>();
        static int holdersFrame = -1;

        public static List<CarrySystem> HoldersOf(ulong objectId)
        {
            if (holdersFrame != Time.frameCount)
            {
                holdersFrame = Time.frameCount;
                holders.Clear();
                foreach (var p in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None))
                {
                    var c = p.carry;
                    if (c == null || c.heldId.Value == ulong.MaxValue) continue;
                    if (!holders.TryGetValue(c.heldId.Value, out var l)) holders[c.heldId.Value] = l = new List<CarrySystem>(2);
                    l.Add(c);
                }
            }
            return holders.TryGetValue(objectId, out var list) ? list : Empty;
        }

        public bool IsHeavy(Rigidbody rb) => rb != null && rb.mass > maxMass;

        /// <summary>Walking speed multiplier while carrying (a pair carry is slow, a lone grip a little).</summary>
        public float SpeedScale
        {
            get
            {
                if (!IsCarrying) return 1f;
                if (HoldersOf(heldId.Value).Count >= 2) return 0.55f;
                return TryGetHeld(out var rb) && IsHeavy(rb) ? 0.9f : 1f;
            }
        }

        bool TryGetHeld(out Rigidbody rb)
        {
            rb = held;
            if (rb != null) return true;
            if (heldId.Value == ulong.MaxValue || NetworkManager.Singleton == null) return false;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(heldId.Value, out var no) || no == null) return false;
            rb = no.GetComponent<Rigidbody>();
            return rb != null;
        }

        public bool ServerPickup(Rigidbody rb, PlayerNet player)
        {
            if (!IsServer || held != null || rb == null) return false;
            var no = rb.GetComponent<NetworkObject>();
            if (no == null) return false;
            if (rb.mass > PairMaxMass)
            {
                CaseManager.Toast("Çok ağır! Forklift ya da el arabası lazım.");
                return false;
            }
            if (rb.mass > maxMass)                                       // heavy: needs a second pair of hands
            {
                var others = new List<CarrySystem>(HoldersOf(no.NetworkObjectId));
                if (others.Count >= 2) return false;
                if (others.Count == 1 && others[0].held == null) return false;
                held = rb; heldSince = Time.time; heldId.Value = no.NetworkObjectId;
                holdersFrame = -1;                                       // the list changed
                if (others.Count == 0)
                {
                    CaseManager.Toast("Ağır! Bir arkadaşın da tutmalı: yanına gelip [E].");
                    return true;                                         // gripping, waiting for the partner
                }
                others[0].BeginPhysicsHold(rb, others[0].GetComponent<PlayerNet>(), true);
                BeginPhysicsHold(rb, player, false);
                CaseManager.Toast("İki kişi taşıyorsunuz. Biriniz bırakırsa ikiniz de bırakırsınız.");
                return true;
            }
            held = rb; heldSince = Time.time; heldId.Value = no.NetworkObjectId;
            BeginPhysicsHold(rb, player, true);
            return true;
        }

        void BeginPhysicsHold(Rigidbody rb, PlayerNet player, bool owner)
        {
            held = rb;
            physicsBegun = true; ownsPhysics = owner;
            heldColliders = rb.GetComponentsInChildren<Collider>();
            bodyColliders = player.BodyColliders();
            foreach (var h in heldColliders) foreach (var b in bodyColliders) Physics.IgnoreCollision(h, b, true);
            if (!owner) return;
            wasGravity = rb.useGravity;
            oldDamping = rb.linearDamping;
            rb.useGravity = false;
            rb.linearDamping = 4f;
            safeUntil[rb.GetInstanceID()] = float.MaxValue;
            var dest = rb.GetComponent<Props.Destructible>();
            if (dest != null) dest.ServerSetHeld(true, this);
        }

        /// <summary>Server: let go. A pair carry ends for both players.</summary>
        public void ServerDrop()
        {
            if (!IsServer || held == null) return;
            if (IsHeavy(held))
                foreach (var other in new List<CarrySystem>(HoldersOf(heldId.Value)))
                    if (other != this) other.Release();
            Release();
        }

        void Release()
        {
            if (!IsServer || held == null) return;
            if (physicsBegun && heldColliders != null && bodyColliders != null)
                foreach (var h in heldColliders) foreach (var b in bodyColliders) if (h != null && b != null) Physics.IgnoreCollision(h, b, false);
            if (ownsPhysics)
            {
                held.useGravity = wasGravity;
                held.linearDamping = oldDamping;
                safeUntil[held.GetInstanceID()] = Time.time + 0.7f;
                var dest = held.GetComponent<Props.Destructible>();
                if (dest != null) { dest.ServerSetHeld(false, null); dest.ServerGrace(0.7f); }   // dropped from the hands: not a hit
            }
            held = null; physicsBegun = false; ownsPhysics = false;
            heldId.Value = ulong.MaxValue;
            holdersFrame = -1;
        }

        /// <summary>Server: release the held body with a throw toward aim. The prop becomes a Projectile that can bonk a player.</summary>
        public void ServerThrow(Vector3 aim, PlayerNet thrower)
        {
            if (!IsServer || held == null || IsHeavy(held)) return;      // a heavy prop is not thrown
            var rb = held;
            ServerDrop();
            safeUntil[rb.GetInstanceID()] = 0f;
            var thrownDest = rb.GetComponent<Props.Destructible>();
            if (thrownDest != null) thrownDest.ServerGrace(0f);          // a throw IS a hit
            aim.Normalize();
            aim.y = Mathf.Max(aim.y, 0.12f);
            aim.Normalize();
            rb.linearVelocity = aim * 15f;
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            var projectile = rb.GetComponent<Props.Projectile>();
            if (projectile == null) projectile = rb.gameObject.AddComponent<Props.Projectile>();
            projectile.Init(thrower);
        }

        float nextHandOver;

        /// <summary>Owner asks to give the carried object to the teammate in front of them. The server checks everything.</summary>
        [Rpc(SendTo.Server)]
        public void HandOverRpc()
        {
            if (!IsServer || held == null || IsHeavy(held) || Time.time < nextHandOver) return;
            nextHandOver = Time.time + 0.6f;
            var me = GetComponent<PlayerNet>();
            if (me == null || me.hidden.Value) return;
            PlayerNet best = null; var bestScore = float.MinValue;
            foreach (var other in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None))
            {
                if (other == me || other.hidden.Value || other.carry == null || other.carry.IsCarrying) continue;
                var to = other.transform.position - transform.position; to.y = 0f;
                var dist = to.magnitude;
                if (dist > 2.8f) continue;
                var facing = Vector3.Dot(transform.forward, to.normalized);
                if (facing < 0.25f) continue;
                var score = facing * 2f - dist;
                if (score > bestScore) { best = other; bestScore = score; }
            }
            if (best == null) return;
            var rb = held;
            ServerDrop();
            if (!best.carry.ServerPickup(rb, best)) ServerPickup(rb, me);     // refused (too heavy...): the giver keeps it
        }

        public override void OnNetworkSpawn()
        {
            heldId.OnValueChanged += OnHeldChanged;
            if (heldId.Value != ulong.MaxValue) SetIgnoreEverywhere(heldId.Value, true);
        }

        public override void OnNetworkDespawn()
        {
            heldId.OnValueChanged -= OnHeldChanged;
            if (IsServer) ServerDrop();
        }

        // The server ignores collisions between the held box and the bodies (see BeginPhysicsHold), but every OTHER machine runs its own character
        // controller: it must ignore the replicated box too, or the carrier keeps bumping into what is in their own hands.
        void OnHeldChanged(ulong previous, ulong current)
        {
            SetIgnoreEverywhere(previous, false);
            SetIgnoreEverywhere(current, true);
        }

        static void SetIgnoreEverywhere(ulong objectId, bool ignore)
        {
            if (objectId == ulong.MaxValue || NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var no) || no == null) return;
            var cols = no.GetComponentsInChildren<Collider>();
            foreach (var p in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None))
                foreach (var b in p.BodyColliders())
                    foreach (var h in cols)
                        if (h != null && b != null) Physics.IgnoreCollision(h, b, ignore);
        }

        /// <summary>Where the held object belongs: at our hold point, or between the hold points of both carriers.</summary>
        bool Target(Rigidbody rb, out Vector3 pos, out Quaternion rot)
        {
            pos = default; rot = default;
            if (holdPoint == null) return false;
            if (!IsHeavy(rb))
            {
                pos = holdPoint.position; rot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                return true;
            }
            var list = HoldersOf(heldId.Value);
            if (list.Count < 2 || list[0].holdPoint == null || list[1].holdPoint == null) return false;     // still waiting for the second carrier
            pos = (list[0].holdPoint.position + list[1].holdPoint.position) * 0.5f;
            var f = list[0].transform.forward + list[1].transform.forward;
            if (f.sqrMagnitude < 0.01f) f = list[0].transform.forward;
            f.y = 0f;
            rot = Quaternion.LookRotation(f.normalized, Vector3.up);
            return true;
        }

        // Rigid hold: no spring means no lag and no swimming around the carrier.
        void FixedUpdate()
        {
            if (!IsServer || held == null) return;
            if (IsHeavy(held))
            {
                var list = HoldersOf(heldId.Value);
                if (list.Count == 1)                                       // gripping alone: nothing lifts, and walking away lets go
                {
                    if ((held.position - transform.position).magnitude > 4f) ServerDrop();
                    return;
                }
                if (list.Count >= 2 && (list[0].transform.position - list[1].transform.position).magnitude > 5f) { ServerDrop(); return; }
            }
            else
            {
                var to = holdPoint.position - held.position;
                if (to.magnitude > 3.5f && heldSince + 0.5f < Time.time) { ServerDrop(); return; }
            }
            if (!Target(held, out var pos, out var rot)) return;
            held.linearVelocity = Vector3.zero;
            held.angularVelocity = Vector3.zero;
            held.position = pos;
            held.rotation = rot;
        }

        // Every machine, after the replicated pose has been applied: show the held object exactly in the carrier hands (the replicated copy arrives
        // late and would trail behind or sink into the character).
        void LateUpdate()
        {
            if (heldId.Value == ulong.MaxValue || NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(heldId.Value, out var no) || no == null) return;
            var rb = no.GetComponent<Rigidbody>();
            if (rb == null || !Target(rb, out var pos, out var rot)) return;
            no.transform.SetPositionAndRotation(pos, rot);
        }
    }
}
