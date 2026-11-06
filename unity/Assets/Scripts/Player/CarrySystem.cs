using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Holds one light rigidbody in front of the player. Server-authoritative: the server drives the held body toward the player's
    /// hold point; every peer can read heldId to show the prompt.
    /// </summary>
    public class CarrySystem : NetworkBehaviour
    {
        public Transform holdPoint;
        public float maxMass = 6f;
        public NetworkVariable<ulong> heldId = new NetworkVariable<ulong>(ulong.MaxValue);

        Rigidbody held;
        Collider[] heldColliders, bodyColliders;
        bool wasGravity;
        float oldDamping;

        public bool IsCarrying => heldId.Value != ulong.MaxValue;

        // Bodies that must not hurt breakable things right now: carried ones (they are pulled around at speed) and ones just let go of.
        // A throw clears it again (a throw IS a hit). Server only.
        static readonly System.Collections.Generic.Dictionary<int, float> safeUntil = new System.Collections.Generic.Dictionary<int, float>();
        public static bool IsSafeBody(Rigidbody rb) => rb != null && safeUntil.TryGetValue(rb.GetInstanceID(), out var t) && Time.time < t;

        public bool ServerPickup(Rigidbody rb, PlayerNet player)
        {
            if (!IsServer || held != null || rb == null) return false;
            if (rb.mass > maxMass)
            {
                CaseManager.Toast("Çok ağır! Forklift ya da el arabası lazım.");
                return false;
            }
            var no = rb.GetComponent<NetworkObject>();
            if (no == null) return false;
            held = rb;
            heldColliders = rb.GetComponentsInChildren<Collider>();
            bodyColliders = player.BodyColliders();
            foreach (var h in heldColliders) foreach (var b in bodyColliders) Physics.IgnoreCollision(h, b, true);
            wasGravity = rb.useGravity;
            oldDamping = rb.linearDamping;
            rb.useGravity = false;
            rb.linearDamping = 4f;
            heldId.Value = no.NetworkObjectId; heldSince = Time.time;
            safeUntil[rb.GetInstanceID()] = float.MaxValue;
            var dest = rb.GetComponent<Props.Destructible>();
            if (dest != null) dest.ServerSetHeld(true, this);
            return true;
        }

        public void ServerDrop()
        {
            if (!IsServer || held == null) return;
            foreach (var h in heldColliders) foreach (var b in bodyColliders) if (h != null && b != null) Physics.IgnoreCollision(h, b, false);
            held.useGravity = wasGravity;
            held.linearDamping = oldDamping;
            safeUntil[held.GetInstanceID()] = Time.time + 0.7f;
            var dest = held.GetComponent<Props.Destructible>();
            if (dest != null) { dest.ServerSetHeld(false, null); dest.ServerGrace(0.7f); }   // dropped from the hands: not a hit
            held = null;
            heldId.Value = ulong.MaxValue;
        }

        /// <summary>Server: release the held body with a throw toward 'aim'. The prop becomes a Projectile that can bonk a player.</summary>
        public void ServerThrow(Vector3 aim, PlayerNet thrower)
        {
            if (!IsServer || held == null) return;
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
            if (!IsServer || held == null || Time.time < nextHandOver) return;
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

        // The server ignores collisions between the held box and the bodies (see ServerPickup), but every OTHER machine runs its own character
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

        // Rigid hold: the held body is placed exactly at the hold point (in the hands) every physics step. No spring means no lag and no swimming around the carrier.
        void FixedUpdate()
        {
            if (!IsServer || held == null) return;
            var to = holdPoint.position - held.position;
            if (to.magnitude > 3.5f && heldSince + 0.5f < Time.time) { ServerDrop(); return; }
            held.linearVelocity = Vector3.zero;
            held.angularVelocity = Vector3.zero;
            held.position = holdPoint.position;
            held.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        }

        float heldSince;

        // Every machine, after the replicated pose has been applied: show the held object exactly in the carrier's hands (the replicated copy arrives
        // late and would trail behind or sink into the character).
        void LateUpdate()
        {
            if (heldId.Value == ulong.MaxValue || holdPoint == null || NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(heldId.Value, out var no) || no == null) return;
            no.transform.SetPositionAndRotation(holdPoint.position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        }
    }
}
