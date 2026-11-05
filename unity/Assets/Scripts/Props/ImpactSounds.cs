using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Audio;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// Material sound for any rigidbody. The server detects the impact and sends one tiny unreliable event
    /// (position, material, intensity, mass); every client plays it locally. Rate limited: at most 10 events/s per object and 6 per frame overall.
    /// Heavy things sound heavy: pitch falls with mass, volume rises with speed.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class ImpactSounds : NetworkBehaviour
    {
        public MaterialKind material = MaterialKind.Wood;
        public float minSpeed = 1.6f;

        float last;
        static int frame, perFrame;
        public static int Sent, Played;

        void OnCollisionEnter(Collision col)
        {
            if (!IsServer || !IsSpawned) return;
            if (Time.time - last < 0.1f) return;
            var v = col.relativeVelocity.magnitude;
            if (v < minSpeed) return;
            if (frame != Time.frameCount) { frame = Time.frameCount; perFrame = 0; }
            if (perFrame++ >= 6) return;
            last = Time.time;
            var own = GetComponent<Rigidbody>();
            var mass = Mathf.Max(own != null ? own.mass : 20f, col.rigidbody != null ? col.rigidbody.mass * 0.25f : 0f);   // heavy partner makes it sound heavier
            var p = col.contactCount > 0 ? col.GetContact(0).point : transform.position;
            Sent++;
            ImpactRpc(p, (byte)material, (byte)Mathf.Clamp((v - minSpeed) / 10f * 255f, 8f, 255f), (byte)Mathf.Clamp(mass, 1f, 255f));
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
        void ImpactRpc(Vector3 pos, byte mat, byte intensity, byte mass)
        {
            Played++;
            if (Net.DedicatedServer.IsDedicated) return;
            if (float.IsNaN(pos.x) || float.IsInfinity(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z)) return;
            GameAudio.PlayImpact((MaterialKind)Mathf.Min(mat, (byte)6), pos, intensity / 255f, mass);
        }
    }
}
