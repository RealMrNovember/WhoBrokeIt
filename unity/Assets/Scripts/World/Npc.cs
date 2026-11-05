using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.World
{
    /// <summary>
    /// Ambient pedestrian. The server walks it between random spots of its area (standing around now and then) and replicates the pose; every
    /// peer dresses it in one of the character models and the usual CharacterAnimator turns the movement into steps. It only lives its own life:
    /// nothing in the contracts depends on it, and it is a solid kinematic body that players and props bump into.
    /// </summary>
    public class Npc : NetworkBehaviour
    {
        public Vector3 center;
        public float radiusMin = 4f, radiusMax = 10f;
        public float speed = 1.3f;
        public bool stationary;                 // vendors stand at their stall

        public NetworkVariable<byte> look = new NetworkVariable<byte>(0);

        GameObject visual;
        Vector3 target;
        float waitUntil;
        bool hasTarget;

        public override void OnNetworkSpawn()
        {
            look.OnValueChanged += (a, b) => Dress(b);
            if (IsServer)
            {
                look.Value = (byte)Random.Range(0, CharacterCatalog.All.Length);
                waitUntil = Time.time + Random.Range(0.5f, 4f);
            }
            Dress(look.Value);
        }

        void Dress(int id)
        {
            if (Net.DedicatedServer.IsDedicated) return;               // servers never draw anything
            if (visual != null) Destroy(visual);
            visual = CharacterCatalog.Build(id, transform);             // the animator follows its parent, i.e. this root
        }

        bool Free(Vector3 from, Vector3 to)
        {
            if (Physics.CheckCapsule(to + Vector3.up * 0.5f, to + Vector3.up * 1.5f, 0.5f, ~0, QueryTriggerInteraction.Ignore)) return false;   // low things (planters, benches) count too
            var d = to - from; var len = d.magnitude;
            return len < 0.1f || !Physics.CapsuleCast(from + Vector3.up * 0.5f, from + Vector3.up * 1.5f, 0.38f, d / len, out _, len, ~0, QueryTriggerInteraction.Ignore);
        }

        void PickTarget()
        {
            for (var i = 0; i < 10; i++)
            {
                var a = Random.Range(0f, Mathf.PI * 2f);
                var r = Random.Range(radiusMin, radiusMax);
                var p = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (Vector3.Distance(p, transform.position) < 2f || !Free(transform.position, p)) continue;
                target = p; hasTarget = true; return;
            }
            hasTarget = false;
            waitUntil = Time.time + 2f;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned || stationary) return;
            if (Time.time < waitUntil) return;
            if (!hasTarget) { PickTarget(); if (!hasTarget) return; }
            var to = target - transform.position; to.y = 0f;
            if (to.magnitude < 0.25f) { hasTarget = false; waitUntil = Time.time + Random.Range(2f, 7f); return; }
            var dir = to.normalized;
            var step = dir * speed * Time.deltaTime;
            // something stepped in the way (a player, a prop): stand still a moment and choose again
            if (Physics.CapsuleCast(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 1.5f, 0.33f, dir, out var hit, 0.6f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform))
            { hasTarget = false; waitUntil = Time.time + Random.Range(0.6f, 1.6f); return; }
            transform.position += step;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
