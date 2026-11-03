using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Inspectable evidence. Adds an entry to the notebook once.</summary>
    public class Clue : Interactable
    {
        public string title = "İpucu";
        [TextArea] public string text = "";
        bool found;

        public override string Prompt => found ? title + " (incelendi)" : "İncele: " + title;

        public override void Interact(GameObject user)
        {
            if (CaseManager.I == null) return;
            if (CaseManager.I.AddClue(title, text)) found = true;
            else CaseManager.Toast(text, 3.5f);
        }
    }
}
