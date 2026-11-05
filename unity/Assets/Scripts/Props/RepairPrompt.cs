using UnityEngine;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// Shows the "repair me" prompt on static breakables (carryable ones show it through Carryable). Lives on the root next to a trigger collider that
    /// is enabled only while the object is Broken (the intact colliders are off then, so nothing else would be found by the interaction search).
    /// Pressing [E] only explains how to repair; the work is done with the hammer.
    /// </summary>
    public class RepairPrompt : Interactable
    {
        void Reset() { prompt = "Kırık! Çekiç: [3] seç, sol tık basılı tut"; }
        protected override void ServerInteract(ulong clientId) => CaseManager.Toast("Çekiçle onar: [3] tuşu, sonra sol tık basılı tut. İki kişi daha hızlı onarır.");
    }
}
