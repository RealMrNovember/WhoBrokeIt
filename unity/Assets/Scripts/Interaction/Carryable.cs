using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Light physics prop the player can pick up with [E].</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : Interactable
    {
        void Reset() { prompt = "Al"; }

        public override void Interact(GameObject user)
        {
            var carry = user.GetComponent<CarrySystem>();
            if (carry != null) carry.TryPickup(GetComponent<Rigidbody>());
        }
    }
}
