using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Player
{
    /// <summary>Holds one light rigidbody in front of the player. Drop is triggered by InteractionSystem ([E]).</summary>
    [RequireComponent(typeof(CharacterController))]
    public class CarrySystem : MonoBehaviour
    {
        public Transform holdPoint;
        public float maxMass = 6f;

        Rigidbody held;
        Collider[] heldColliders;
        CharacterController cc;
        bool wasGravity;
        float oldDamping;

        public bool IsCarrying => held != null;

        void Awake() => cc = GetComponent<CharacterController>();

        public bool TryPickup(Rigidbody rb)
        {
            if (held != null || rb == null) return false;
            if (rb.mass > maxMass)
            {
                CaseManager.Toast("Çok ağır! Forklift ya da el arabası lazım.", 2.5f);
                return false;
            }
            held = rb;
            heldColliders = rb.GetComponentsInChildren<Collider>();
            foreach (var c in heldColliders) Physics.IgnoreCollision(cc, c, true);
            wasGravity = rb.useGravity;
            oldDamping = rb.linearDamping;
            rb.useGravity = false;
            rb.linearDamping = 4f;
            return true;
        }

        public void Drop()
        {
            if (held == null) return;
            foreach (var c in heldColliders) if (c != null) Physics.IgnoreCollision(cc, c, false);
            held.useGravity = wasGravity;
            held.linearDamping = oldDamping;
            held = null;
        }

        void FixedUpdate()
        {
            if (held == null) return;
            var to = holdPoint.position - held.position;
            if (to.magnitude > 3.5f) { Drop(); return; }
            held.linearVelocity = Vector3.ClampMagnitude(to * 14f, 12f);
            held.angularVelocity = Vector3.zero;
            held.MoveRotation(Quaternion.Slerp(held.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), 0.2f));
        }
    }
}
