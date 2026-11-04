using UnityEngine;
using UnityEngine.InputSystem;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Owner-side: finds the best Interactable near the player, shows a prompt, asks the server on [E]. While carrying, [E] drops.</summary>
    public class InteractionSystem : MonoBehaviour
    {
        public float range = 2.5f;
        Interactable current;
        PlayerNet net;
        GUIStyle style;

        void Awake() => net = GetComponent<PlayerNet>();

        void Update()
        {
            var kb = Keyboard.current;
            var pressed = kb != null && kb.eKey.wasPressedThisFrame;
            if (net.carry != null && net.carry.IsCarrying)
            {
                current = null;
                if (pressed) net.DropRpc();
                return;
            }
            current = FindBest();
            if (current != null && pressed) current.RequestInteract();
        }

        Interactable FindBest()
        {
            Interactable best = null;
            var bestScore = float.MinValue;
            foreach (var c in Physics.OverlapSphere(transform.position, range, ~0, QueryTriggerInteraction.Collide))
            {
                var it = c.GetComponentInParent<Interactable>();
                if (it == null || !it.enabled) continue;
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
            string text = null;
            if (net.carry != null && net.carry.IsCarrying) text = "[E] Bırak  ·  [Sol tık] Fırlat";
            else if (current != null) text = "[E] " + current.Prompt;
            if (text == null) return;
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 50), text, style);
        }
    }
}
