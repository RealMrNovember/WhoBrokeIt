using Unity.Netcode;
using UnityEngine;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Anything a player can use with [E]. The request is sent to the server, which runs ServerInteract (server-authoritative).</summary>
    public abstract class Interactable : NetworkBehaviour
    {
        public string prompt = "Kullan";
        public virtual string Prompt => prompt;

        /// <summary>Called by the local player's InteractionSystem.</summary>
        public void RequestInteract() => InteractRpc();

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void InteractRpc(RpcParams rpcParams = default) => ServerInteract(rpcParams.Receive.SenderClientId);

        protected abstract void ServerInteract(ulong clientId);
    }
}
