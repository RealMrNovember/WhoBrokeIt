using UnityEngine;
using WhoBrokeIt.Net;
using UnityEngine.InputSystem;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Owner-side: finds the best Interactable near the player, shows a prompt, asks the server on [E]. While carrying, [E] drops.</summary>
    public class InteractionSystem : MonoBehaviour
    {
        public float range = 2.5f;
        Interactable current;
        PlayerNet mate;
        PlayerNet net;
        GUIStyle style;

        void Awake() => net = GetComponent<PlayerNet>();

        void Update()
        {
            var kb = Keyboard.current;
            var pressed = kb != null && kb.eKey.wasPressedThisFrame && !Net.PauseMenu.Blocked;
            if (net.carry != null && net.carry.IsCarrying)
            {
                current = null;
                mate = FindTeammate();
                if (pressed) { if (mate != null) net.carry.HandOverRpc(); else net.DropRpc(); }
                return;
            }
            current = FindBest();
            if (current != null && pressed) current.RequestInteract();
        }

        /// <summary>A teammate close in front of us who has free hands (for handing over what we carry).</summary>
        PlayerNet FindTeammate()
        {
            PlayerNet best = null; var bestScore = float.MinValue;
            foreach (var other in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None))
            {
                if (other == net || other.IsHidden || other.carry == null || other.carry.IsCarrying) continue;
                var to = other.transform.position - transform.position; to.y = 0f;
                if (to.magnitude > 2.5f) continue;
                var facing = Vector3.Dot(transform.forward, to.normalized);
                if (facing < 0.3f) continue;
                var score = facing * 2f - to.magnitude;
                if (score > bestScore) { best = other; bestScore = score; }
            }
            return best;
        }

        /// <summary>The thing under the crosshair: a thick ray from the camera through the screen centre, only things within reach of the character count.</summary>
        Interactable FindAimed()
        {
            var cam = Camera.main;
            if (cam == null || !cam.enabled) return null;
            var ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Interactable best = null; var bestD = float.MaxValue;
            foreach (var h in Physics.SphereCastAll(ray, 0.3f, 14f, ~0, QueryTriggerInteraction.Collide))
            {
                var it = h.collider.GetComponentInParent<Interactable>();
                if (it == null || !it.enabled || h.collider.transform.IsChildOf(transform)) continue;
                var p = h.distance > 0f ? h.point : h.collider.ClosestPoint(ray.origin);
                var reach = Vector3.Distance(transform.position + Vector3.up * 1f, h.collider.ClosestPoint(transform.position + Vector3.up * 1f));
                if (reach > range) continue;
                if (h.distance < bestD) { bestD = h.distance; best = it; }
            }
            return best;
        }

        Interactable FindBest()
        {
            var aimed = FindAimed();
            if (aimed != null) return aimed;
            return null;
        }

        static Texture2D dot;

        /// <summary>Aim point in the middle of the screen: a small dot that grows into a ring (yellow) when something can be used.</summary>
        void DrawCrosshair()
        {
            if (Net.PauseMenu.Blocked || Event.current.type != EventType.Repaint) return;
            if (dot == null) { dot = new Texture2D(1, 1); dot.SetPixel(0, 0, Color.white); dot.Apply(); }
            var s = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            var c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var on = current != null || (net.carry != null && net.carry.IsCarrying);
            var col = on ? new Color(1f, 0.85f, 0.2f, 0.95f) : new Color(1f, 1f, 1f, 0.8f);
            var old = GUI.color;
            var r = (on ? 5f : 3f) * Mathf.Max(0.7f, s);
            GUI.color = new Color(0f, 0f, 0f, 0.45f); GUI.DrawTexture(new Rect(c.x - r - 1.5f, c.y - r - 1.5f, 2f * r + 3f, 2f * r + 3f), dot);
            GUI.color = col; GUI.DrawTexture(new Rect(c.x - r, c.y - r, 2f * r, 2f * r), dot);
            if (on)
            {
                var len = 9f * Mathf.Max(0.7f, s); var th = 2f * Mathf.Max(0.7f, s); var gap = 12f * Mathf.Max(0.7f, s);
                foreach (var d in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                {
                    var p = c + d * gap;
                    var rc = d.x == 0f ? new Rect(p.x - th / 2f, p.y - (d.y > 0 ? 0f : len), th, len) : new Rect(p.x - (d.x > 0 ? 0f : len), p.y - th / 2f, len, th);
                    GUI.DrawTexture(rc, dot);
                }
            }
            GUI.color = old;
        }

        Interactable FindNearest()
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
            DrawCrosshair();
            string text = null;
            if (net.carry != null && net.carry.IsCarrying) text = (mate != null ? "[E] Arkadaşına ver" : "[E] Bırak") + "  ·  [Sol tık] Fırlat";
            else if (current != null) text = "[E] " + current.Prompt;
            if (text == null) return;
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            LocGui.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 50), text, style);
        }
    }
}
