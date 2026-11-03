using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Light physics prop a player can pick up with [E]. Runs on the server.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : Interactable
    {
        void Reset() { prompt = "Al"; }

        protected override void ServerInteract(ulong clientId)
        {
            var player = PlayerNet.ForClient(clientId);
            if (player == null || player.carry == null) return;
            player.carry.ServerPickup(GetComponent<Rigidbody>(), player);
        }
    }
}
