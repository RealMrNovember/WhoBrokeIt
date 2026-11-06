using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Text chat during a match: Enter opens the input line, Enter sends, Esc cancels. The text goes to the server (PlayerNet.ChatRpc validates and rate-limits it)
    /// and comes back to everybody; the last few lines fade out after a while. While typing, the game ignores movement keys (PauseMenu.Blocked).
    /// </summary>
    public class ChatBox : MonoBehaviour
    {
        public static bool Typing { get; private set; }

        struct Line { public string who, text; public float at; }
        static readonly List<Line> lines = new List<Line>();
        string draft = "";
        int openedFrame = -1;
        GUIStyle label, field;

        public static void Add(string who, string text)
        {
            lines.Add(new Line { who = who, text = text, at = Time.unscaledTime });
            Debug.Log("[MP] CHAT " + who + ": " + text);
            if (lines.Count > 40) lines.RemoveAt(0);
        }

        static bool InGame()
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening && (nm.IsHost || nm.IsConnectedClient) && PlayerNet.Local != null && !DedicatedServer.IsDedicated;
        }

        void Update()
        {
            if (!InGame()) { Typing = false; return; }
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!Typing && !PauseMenu.Open && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) && !kb.altKey.isPressed)
            {
                Typing = true; draft = ""; openedFrame = Time.frameCount;
            }
            if (Typing && PauseMenu.Open) Typing = false;
        }

        void OnGUI()
        {
            if (!InGame()) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = false };
                field = new GUIStyle(GUI.skin.textField) { fontSize = 22 };
            }
            var s = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), 0.6f, 1.2f);
            label.fontSize = Mathf.RoundToInt(20f * s); field.fontSize = Mathf.RoundToInt(22f * s);
            var x = 16f; var baseY = Screen.height - 150f * s;
            var shown = 0;
            for (var i = lines.Count - 1; i >= 0 && shown < 7; i--)
            {
                var age = Time.unscaledTime - lines[i].at;
                if (!Typing && age > 12f) break;
                var a = Typing ? 1f : Mathf.Clamp01((12f - age) / 3f);
                var r = new Rect(x, baseY - (shown + 1) * 26f * s, 560f * s, 26f * s);
                var old = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.35f * a);
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 0.93f, 0.55f, a);
                var who = lines[i].who + ": ";
                GUI.Label(new Rect(r.x + 6f, r.y, r.width, r.height), who, label);
                GUI.color = new Color(1f, 1f, 1f, a);
                var off = label.CalcSize(new GUIContent(who)).x + 6f;
                GUI.Label(new Rect(r.x + off, r.y, r.width - off, r.height), lines[i].text, label);
                GUI.color = old;
                shown++;
            }
            if (!Typing) return;
            var e = Event.current;
            if (e.type == EventType.KeyDown && Time.frameCount != openedFrame)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    var msg = draft.Trim();
                    if (msg.Length > 0 && PlayerNet.Local != null) PlayerNet.Local.SendChat(msg);
                    Typing = false; e.Use(); GUI.FocusControl(null);
                    return;
                }
                if (e.keyCode == KeyCode.Escape) { Typing = false; e.Use(); GUI.FocusControl(null); return; }
            }
            GUI.SetNextControlName("wbi_chat");
            draft = GUI.TextField(new Rect(x, baseY, 560f * s, 34f * s), draft, 100, field);
            GUI.FocusControl("wbi_chat");
        }
    }
}
