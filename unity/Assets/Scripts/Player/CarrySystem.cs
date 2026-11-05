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
            heldId.Value = no.NetworkObjectId;
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

        public override void OnNetworkDespawn() { if (IsServer) ServerDrop(); }

        void FixedUpdate()
        {
            if (!IsServer || held == null) return;
            var to = holdPoint.position - held.position;
            if (to.magnitude > 3.5f) { ServerDrop(); return; }
            held.linearVelocity = Vector3.ClampMagnitude(to * 14f, 12f);
            held.angularVelocity = Vector3.zero;
            held.MoveRotation(Quaternion.Slerp(held.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), 0.2f));
        }
    }
}
