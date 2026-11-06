using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Anything a player can use with [E]. The request is sent to the server, which runs ServerInteract (server-authoritative).</summary>
    public abstract class Interactable : NetworkBehaviour
    {
        public string prompt = "Kullan";
        public virtual string Prompt => prompt;

        static readonly Dictionary<ulong, float> lastRequest = new Dictionary<ulong, float>();
        public const float ReachMeters = 7f;

        /// <summary>Called by the local player's InteractionSystem.</summary>
        public void RequestInteract() => InteractRpc();

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void InteractRpc(RpcParams rpcParams = default)
        {
            var sender = rpcParams.Receive.SenderClientId;
            if (sender != NetworkManager.ServerClientId)                       // the host is the server itself; every remote client is checked
            {
                if (lastRequest.TryGetValue(sender, out var t) && Time.time - t < 0.12f) return;      // no request floods
                lastRequest[sender] = Time.time;
                var p = PlayerNet.ByOwner(sender);
                if (p == null || p.hidden.Value || !Near(p.transform.position)) return;               // you can only use what is within reach
            }
            ServerInteract(sender);
        }

        bool Near(Vector3 from)
        {
            var cols = GetComponentsInChildren<Collider>();
            if (cols.Length == 0) return (transform.position - from).sqrMagnitude <= ReachMeters * ReachMeters * 2.25f;
            var b = cols[0].bounds;
            foreach (var c in cols) b.Encapsulate(c.bounds);
            return (b.ClosestPoint(from) - from).sqrMagnitude <= ReachMeters * ReachMeters;
        }

        protected abstract void ServerInteract(ulong clientId);
    }
}
