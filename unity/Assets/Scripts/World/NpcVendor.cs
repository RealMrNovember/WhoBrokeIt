using UnityEngine;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;

namespace WhoBrokeIt.World
{
    /// <summary>A street vendor: stands behind a stall in front of a building and chats when spoken to ([E] while aiming at them).</summary>
    public class NpcVendor : Interactable
    {
        public string title = "Satıcı";
        public string[] lines = { "Hoş geldin!" };
        int next;

        public override string Prompt => title + " ile konuş";

        protected override void ServerInteract(ulong clientId)
        {
            if (lines == null || lines.Length == 0) return;
            CaseManager.Toast(title + ": " + lines[next++ % lines.Length]);
        }
    }
}
