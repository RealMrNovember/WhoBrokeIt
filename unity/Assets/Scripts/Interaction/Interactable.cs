using UnityEngine;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Anything the player can use with [E]. Subclass and override Interact.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public string prompt = "Kullan";
        public virtual string Prompt => prompt;
        public abstract void Interact(GameObject user);
    }
}
