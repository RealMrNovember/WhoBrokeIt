using UnityEngine;
using UnityEngine.InputSystem;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Finds the best Interactable near the player, shows a prompt, fires on [E].</summary>
    public class InteractionSystem : MonoBehaviour
    {
        public float range = 2.5f;
        Interactable current;
        GUIStyle style;

        void Update()
        {
            current = FindBest();
            var kb = Keyboard.current;
            if (current != null && kb != null && kb.eKey.wasPressedThisFrame) current.Interact(gameObject);
        }

        Interactable FindBest()
        {
            Interactable best = null;
            var bestScore = float.MinValue;
            foreach (var c in Physics.OverlapSphere(transform.position, range, ~0, QueryTriggerInteraction.Collide))
            {
                var it = c.GetComponentInParent<Interactable>();
                if (it == null) continue;
                var to = it.transform.position - transform.position;
                to.y = 0f;
                var facing = Vector3.Dot(transform.forward, to.normalized);
                var score = facing * 2f - to.magnitude;
                if (facing > -0.2f && score > bestScore) { best = it; bestScore = score; }
            }
            return best;
        }

        void OnGUI()
        {
            if (current == null) return;
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 50), "[E] " + current.Prompt, style);
        }
    }
}
