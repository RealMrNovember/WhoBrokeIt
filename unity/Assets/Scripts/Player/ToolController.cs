using UnityEngine;
using UnityEngine.InputSystem;

namespace WhoBrokeIt.Player
{
    /// <summary>Owner-side tool selection and input (1 hands, 2 broom, 3 hammer). The effects run on the server through PlayerNet.UseToolRpc.</summary>
    [RequireComponent(typeof(PlayerNet))]
    public class ToolController : MonoBehaviour
    {
        PlayerNet net;
        float resetHold;
        GUIStyle style;

        void Awake() => net = GetComponent<PlayerNet>();

        void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) net.toolId.Value = 0;
            if (kb.digit2Key.wasPressedThisFrame) net.toolId.Value = 1;
            if (kb.digit3Key.wasPressedThisFrame) net.toolId.Value = 2;
            var tool = net.toolId.Value;

            var using_ = mouse != null && mouse.leftButton.isPressed && Cursor.lockState == CursorLockMode.Locked;
            var swing = using_ ? Mathf.Sin(Time.time * 12f) * 30f : 0f;
            if (net.broomVisual != null) net.broomVisual.transform.localRotation = Quaternion.Euler(20f + swing, 0f, 0f);
            if (net.hammerVisual != null) net.hammerVisual.transform.localRotation = Quaternion.Euler(-40f + swing * 1.5f, 0f, 0f);

            var locked = Cursor.lockState == CursorLockMode.Locked;
            var carrying = net.carry != null && net.carry.IsCarrying;
            var stunned = GetComponent<PlayerController>().Stunned;

            if (using_ && tool != 0 && !carrying && !stunned) net.UseToolRpc(tool, Time.deltaTime);

            // left click while carrying: throw it where the camera looks; right click: swing at whoever stands in front
            if (locked && !stunned && mouse != null)
            {
                if (carrying && mouse.leftButton.wasPressedThisFrame)
                {
                    var cam = ThirdPersonCamera.Instance != null ? ThirdPersonCamera.Instance.transform.forward : transform.forward;
                    net.ThrowRpc(cam);
                }
                if (mouse.rightButton.wasPressedThisFrame) net.MeleeRpc();
            }

            if (kb.xKey.isPressed)
            {
                resetHold += Time.deltaTime;
                if (resetHold > 1f) { resetHold = 0f; net.ResetNearestRpc(); }
            }
            else resetHold = 0f;
        }

        void OnGUI()
        {
            if (style == null)
                style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var names = new[] { "1 Eller", "2 Süpürge", "3 Çekiç" };
            for (var i = 0; i < 3; i++)
            {
                var r = new Rect(Screen.width / 2f - 240 + i * 160, Screen.height - 110, 150, 44);
                GUI.Box(r, "");
                style.normal.textColor = net.toolId.Value == i ? new Color(1f, 0.85f, 0.2f) : Color.white;
                GUI.Label(r, names[i], style);
            }
            if (resetHold > 0.05f)
            {
                style.normal.textColor = Color.white;
                GUI.Label(new Rect(0, Screen.height * 0.6f, Screen.width, 40), "Sıfırlanıyor...", style);
            }
        }
    }
}
