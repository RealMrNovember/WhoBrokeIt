using UnityEngine;
using WhoBrokeIt.Case;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Light physics prop a player can pick up with [E]. Runs on the server.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : Interactable
    {
        void Reset() { prompt = "Al"; }

        Destructible destructible;
        bool IsBroken { get { if (destructible == null) destructible = GetComponent<Destructible>(); return destructible != null && destructible.IsBroken; } }

        /// <summary>A Broken breakable cannot be picked up: the prompt explains the repair instead (see Repairable).</summary>
        public override string Prompt
        {
            get
            {
                if (IsBroken) return "Kırık! Çekiç: [3] seç, sol tık basılı tut";
                var rb = GetComponent<Rigidbody>();
                if (rb != null && rb.mass > 6f && rb.mass <= CarrySystem.PairMaxMass)         // heavy: two people lift it together
                {
                    var no = GetComponent<Unity.Netcode.NetworkObject>();
                    return no != null && CarrySystem.HoldersOf(no.NetworkObjectId).Count == 1 ? "Taşımaya yardım et" : "Birlikte taşı (2 kişi)";
                }
                return prompt;
            }
        }

        protected override void ServerInteract(ulong clientId)
        {
            if (IsBroken) { CaseManager.Toast("Çekiçle onar: [3] tuşu, sonra sol tık basılı tut. İki kişi daha hızlı onarır."); return; }
            var player = PlayerNet.ForClient(clientId);
            if (player == null || player.carry == null) return;
            player.carry.ServerPickup(GetComponent<Rigidbody>(), player);
        }
    }
}
