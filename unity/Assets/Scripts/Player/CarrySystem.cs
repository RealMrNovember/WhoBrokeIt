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
            return true;
        }

        public void ServerDrop()
        {
            if (!IsServer || held == null) return;
            foreach (var h in heldColliders) foreach (var b in bodyColliders) if (h != null && b != null) Physics.IgnoreCollision(h, b, false);
            held.useGravity = wasGravity;
            held.linearDamping = oldDamping;
            held = null;
            heldId.Value = ulong.MaxValue;
        }

        /// <summary>Server: release the held body with a throw toward 'aim'. The prop becomes a Projectile that can bonk a player.</summary>
        public void ServerThrow(Vector3 aim, PlayerNet thrower)
        {
            if (!IsServer || held == null) return;
            var rb = held;
            ServerDrop();
            aim.Normalize();
            aim.y = Mathf.Max(aim.y, 0.12f);
            aim.Normalize();
            rb.linearVelocity = aim * 15f;
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            var projectile = rb.GetComponent<Props.Projectile>();
            if (projectile == null) projectile = rb.gameObject.AddComponent<Props.Projectile>();
            projectile.Init(thrower);
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
