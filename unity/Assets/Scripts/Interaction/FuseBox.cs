using UnityEngine;

namespace WhoBrokeIt.Interaction
{
    /// <summary>P0 test interactable: repairing the fuse box powers the yard lights.</summary>
    public class FuseBox : Interactable
    {
        public Light[] lights;
        public Renderer indicator;
        public bool powered;

        public override string Prompt => powered ? "Sigortayi kapat" : "Sigorta kutusunu onar";

        void Start() => Apply();

        public override void Interact(GameObject user)
        {
            powered = !powered;
            Apply();
        }

        void Apply()
        {
            foreach (var l in lights) if (l != null) l.enabled = powered;
            if (indicator != null) indicator.material.color = powered ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
        }
    }
}
