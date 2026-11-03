using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Inspectable evidence. The server records it in the shared notebook.</summary>
    public class Clue : Interactable
    {
        public int index;
        public string title = "İpucu";
        [TextArea] public string text = "";

        public override string Prompt
        {
            get
            {
                var found = CaseManager.I != null && CaseManager.I.HasClue(index);
                return found ? title + " (incelendi)" : "İncele: " + title;
            }
        }

        protected override void ServerInteract(ulong clientId)
        {
            if (CaseManager.I == null) return;
            if (!CaseManager.I.ServerAddClue(index, title)) CaseManager.Toast(text, 4f);
        }
    }
}
