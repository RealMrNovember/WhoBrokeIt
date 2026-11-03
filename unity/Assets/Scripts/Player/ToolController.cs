using UnityEngine;
using UnityEngine.InputSystem;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Player
{
    public enum Tool { Hands, Broom, Hammer }

    /// <summary>Tool slots 1-3. Broom pushes light props, hammer repairs the fuse box. Hold X to reset the nearest stuck prop.</summary>
    public class ToolController : MonoBehaviour
    {
        public GameObject broomVisual;
        public GameObject hammerVisual;
        public Tool current = Tool.Hands;

        float resetHold;
        GUIStyle style;

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) current = Tool.Hands;
            if (kb.digit2Key.wasPressedThisFrame) current = Tool.Broom;
            if (kb.digit3Key.wasPressedThisFrame) current = Tool.Hammer;
            if (broomVisual != null) broomVisual.SetActive(current == Tool.Broom);
            if (hammerVisual != null) hammerVisual.SetActive(current == Tool.Hammer);

            var using_ = mouse != null && mouse.leftButton.isPressed && Cursor.lockState == CursorLockMode.Locked;
            var swing = using_ ? Mathf.Sin(Time.time * 12f) * 30f : 0f;
            if (broomVisual != null) broomVisual.transform.localRotation = Quaternion.Euler(20f + swing, 0f, 0f);
            if (hammerVisual != null) hammerVisual.transform.localRotation = Quaternion.Euler(-40f + swing * 1.5f, 0f, 0f);

            if (using_)
            {
                if (current == Tool.Broom) Sweep();
                if (current == Tool.Hammer) Hammer();
            }

            if (kb.xKey.isPressed)
            {
                resetHold += Time.deltaTime;
                if (resetHold > 1f)
                {
                    resetHold = 0f;
                    var r = Resettable.Nearest(transform.position, 8f);
                    if (r != null)
                    {
                        r.ResetNow();
                        CaseManager.NoteReset();
                        CaseManager.Toast("Sıfırlandı.", 1.5f);
                    }
                }
            }
            else resetHold = 0f;
        }

        void Sweep()
        {
            var center = transform.position + transform.forward * 1.7f + Vector3.down * 0.5f;
            foreach (var c in Physics.OverlapSphere(center, 1.5f))
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb.isKinematic || rb.mass > 8f) continue;
                rb.AddForce(transform.forward * 24f + Vector3.up * 2f, ForceMode.Acceleration);
            }
        }

        void Hammer()
        {
            var center = transform.position + transform.forward * 1.0f;
            foreach (var c in Physics.OverlapSphere(center, 2.4f))
            {
                var fuse = c.GetComponentInParent<FuseBox>();
                if (fuse == null) continue;
                var to = fuse.transform.position - transform.position;
                to.y = 0f;
                if (Vector3.Dot(transform.forward, to.normalized) < 0.2f) continue;
                fuse.Repair(Time.deltaTime);
                return;
            }
        }

        void OnGUI()
        {
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            var names = new[] { "1 Eller", "2 Süpürge", "3 Çekiç" };
            for (var i = 0; i < 3; i++)
            {
                var r = new Rect(Screen.width / 2f - 240 + i * 160, Screen.height - 110, 150, 44);
                GUI.Box(r, "");
                style.normal.textColor = (int)current == i ? new Color(1f, 0.85f, 0.2f) : Color.white;
                GUI.Label(r, names[i], style);
            }
            if (resetHold > 0.05f)
                GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, 40), "Sıfırlanıyor...", style);
        }
    }
}
