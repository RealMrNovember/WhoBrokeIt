using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Net;
using UnityEngine.InputSystem;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Esc menu while playing: resume, camera settings (mode, distance, height, shoulder, field of view, mouse speed),
    /// full screen, character change and leaving the game. F11 / Alt+Enter toggle full screen anywhere.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static bool Open { get; private set; }
        /// <summary>Game input is off: the Esc menu is open or the chat line is being typed.</summary>
        public static bool Blocked => Open || ChatBox.Typing;

        GUIStyle title, label, btn, small;
        Texture2D dim;

        static bool InGame()
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening && (nm.IsHost || nm.IsConnectedClient) && PlayerNet.Local != null && !DedicatedServer.IsDedicated;
        }

        public static void ToggleFullscreen()
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
            }
            else
            {
                var r = Screen.currentResolution;
                Screen.SetResolution(r.width, r.height, FullScreenMode.FullScreenWindow);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.f11Key.wasPressedThisFrame || (kb.enterKey.wasPressedThisFrame && kb.altKey.isPressed)) ToggleFullscreen();

            if (!InGame()) { Open = false; return; }
            if (kb.escapeKey.wasPressedThisFrame) SetOpen(!Open);
        }

        void SetOpen(bool open)
        {
            Open = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
        }

        bool Button(Rect r, string text, bool primary = false)
        {
            var prev = GUI.backgroundColor;
            if (primary) GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            var pressed = LocGui.Button(r, text, btn);
            GUI.backgroundColor = prev;
            return pressed;
        }

        float Slider(float x, float y, string name, float value, float min, float max, string fmt)
        {
            LocGui.Label(new Rect(x, y, 300, 36), name, small);
            var v = GUI.HorizontalSlider(new Rect(x + 300, y + 12, 220, 20), value, min, max);
            LocGui.Label(new Rect(x + 530, y, 110, 36), v.ToString(fmt), small);
            return v;
        }

        void OnGUI()
        {
            if (!Open) return;
            if (!InGame()) { Open = false; return; }
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
                small = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft };
                btn = new GUIStyle(GUI.skin.button) { fontSize = 26, fontStyle = FontStyle.Bold };
                dim = new Texture2D(1, 1);
                dim.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
                dim.Apply();
            }
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), dim);

            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            var x = w / 2f - 320;
            title.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Box(new Rect(w / 2f - 380, 80, 760, 880), "");
            LocGui.Label(new Rect(x, 110, 640, 70), "Duraklatıldı", title);

            if (Button(new Rect(x, 195, 640, 70), "Devam et", true)) SetOpen(false);

            LocGui.Label(new Rect(x, 285, 640, 34), "Kamera", label);
            var fp = ThirdPersonCamera.FirstPersonPreferred;
            if (Button(new Rect(x, 325, 640, 56), fp ? "Kamera: 1. şahıs  (değiştir)" : "Kamera: 3. şahıs  (değiştir)")) ThirdPersonCamera.FirstPersonPreferred = !fp;
            var cam = ThirdPersonCamera.Instance;
            if (cam != null)
            {
                var d = Slider(x, 392, "Mesafe", cam.Distance, cam.MinDistance, cam.MaxDistance, "0.0");
                if (Mathf.Abs(d - cam.Distance) > 0.001f) { cam.Distance = d; ThirdPersonCamera.DistancePref = d; }
            }
            ThirdPersonCamera.HeightAdjust = Slider(x, 426, "Yükseklik", ThirdPersonCamera.HeightAdjust, -0.8f, 1.2f, "0.0");
            ThirdPersonCamera.ShoulderAdjust = Slider(x, 460, "Omuz kaydırma", ThirdPersonCamera.ShoulderAdjust, -1.2f, 1.2f, "0.0");
            ThirdPersonCamera.Fov = Slider(x, 494, "Görüş alanı", ThirdPersonCamera.Fov, 50f, 100f, "0");
            ThirdPersonCamera.Sensitivity = Slider(x, 528, "Fare hızı", ThirdPersonCamera.Sensitivity, 0.03f, 0.4f, "0.00");
            WhoBrokeIt.Audio.GameAudio.Master = Slider(x, 566, "Ses (genel)", WhoBrokeIt.Audio.GameAudio.Master, 0f, 1f, "0.00");
            WhoBrokeIt.Audio.GameAudio.Effects = Slider(x, 600, "Ses efektleri", WhoBrokeIt.Audio.GameAudio.Effects, 0f, 1f, "0.00");
            LocGui.Label(new Rect(x, 636, 640, 30), "Oyunda: fare tekerleği yakınlaştırır, [ ] eğer, V 1./3. şahıs.", small);
            if (Button(new Rect(x, 668, 310, 50), "Varsayılan kamera"))
            {
                ThirdPersonCamera.HeightAdjust = 0f; ThirdPersonCamera.ShoulderAdjust = 0f; ThirdPersonCamera.Fov = 70f; ThirdPersonCamera.Sensitivity = 0.12f;
                if (cam != null) { cam.Distance = 5.5f; ThirdPersonCamera.DistancePref = 5.5f; }
            }
            if (Button(new Rect(x + 330, 668, 310, 50), Screen.fullScreen ? "Pencereli yap  (F11)" : "Tam ekran yap  (F11)")) ToggleFullscreen();

            var p = PlayerNet.Local;
            if (p != null)
            {
                var qlv = GraphicsQuality.Level < 0 ? GraphicsQuality.Detect() : GraphicsQuality.Level;
                if (Button(new Rect(x, 728, 640, 48), "Grafik kalitesi: " + GraphicsQuality.Names[qlv] + "  (değiştir)")) GraphicsQuality.Cycle();
                LocGui.Label(new Rect(x, 786, 640, 34), "Karakter: " + CharacterCatalog.All[Mathf.Clamp(p.characterId.Value, 0, CharacterCatalog.Count - 1)].name, label);
                if (Button(new Rect(x, 822, 310, 48), "◀ Önceki")) ChangeCharacter(p, -1);
                if (Button(new Rect(x + 330, 822, 310, 48), "Sonraki ▶")) ChangeCharacter(p, 1);
            }

            if (Button(new Rect(x + 450, 120, 190, 44), "Dil: " + Loc.LanguageName)) Loc.Toggle();
            if (Button(new Rect(x, 890, 310, 54), "Ana menüye dön"))
            {
                SetOpen(false);
                NetworkManager.Singleton.Shutdown();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (Button(new Rect(x + 330, 890, 310, 54), "Oyundan çık")) Application.Quit();
        }

        static void ChangeCharacter(PlayerNet p, int dir)
        {
            var i = (p.characterId.Value + dir + CharacterCatalog.Count) % CharacterCatalog.Count;
            CharacterCatalog.Save(i);
            p.characterId.Value = i;
        }
    }
}
