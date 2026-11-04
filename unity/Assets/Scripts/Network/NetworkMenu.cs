using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Main menu. Two online ways in, both through dedicated servers: quick play (public room, joins strangers automatically) and
    /// private rooms (share a code with friends). Also: character selection, camera mode, solo play and direct IP/LAN.
    /// </summary>
    public class NetworkMenu : MonoBehaviour
    {
        public ushort lanPort = 7777;

        enum Page { Main, Friends, Characters, Advanced }
        Page page = Page.Main;
        string code = "";
        string address = "127.0.0.1";
        string status = "";
        bool busy;
        float connectingSince = -1f;
        string myIps = "";
        GUIStyle title, label, btn, small, field;

        // character preview (rendered by a private camera into a texture, far away from the scene)
        int charIndex;
        Transform stage;
        Camera previewCam;
        RenderTexture previewRt;
        GameObject previewModel;
        int shownIndex = -1;

        void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            if (System.Array.IndexOf(args, "-server") >= 0 || System.Array.IndexOf(args, "-smoke") >= 0 || System.Array.IndexOf(args, "-shots") >= 0) { enabled = false; return; }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            myIps = LocalAddresses();
            charIndex = CharacterCatalog.SavedChoice();
            var nm = NetworkManager.Singleton;
            if (nm != null) nm.OnClientDisconnectCallback += OnDisconnected;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
            if (previewRt != null) previewRt.Release();
        }

        void OnDisconnected(ulong id)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || id != nm.LocalClientId) return;
            status = string.IsNullOrEmpty(nm.DisconnectReason) ? "Bağlantı kesildi." : nm.DisconnectReason;
            busy = false;
            connectingSince = -1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        static string LocalAddresses()
        {
            var sb = new System.Text.StringBuilder();
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                    if (ip.AddressFamily == AddressFamily.InterNetwork) sb.Append(ip).Append("   ");
            }
            catch { }
            return sb.ToString();
        }

        // ---------- actions ----------
        void Connect(string ip, int port)
        {
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(ip, (ushort)port);
            ConnectionGate.PrepareClient();
            connectingSince = Time.time;
            status = "";
            if (!nm.StartClient()) { status = "Bağlantı başlatılamadı."; connectingSince = -1f; }
        }

        void HostLocal(string listen)
        {
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", lanPort, listen);
            status = "";
            if (!nm.StartHost()) status = "Oda kurulamadı (port dolu olabilir).";
        }

        System.Collections.IEnumerator Online(System.Collections.IEnumerator call, OnlineApi.Room[] box, string waitText)
        {
            busy = true;
            status = waitText;
            yield return call;
            busy = false;
            var r = box[0];
            if (r == null || !string.IsNullOrEmpty(r.error)) { status = r != null && !string.IsNullOrEmpty(r.error) ? r.error : "Bir hata oldu."; yield break; }
            status = "Bağlanılıyor...";
            Connect(r.address, r.port);
        }

        void RunQuick() { var box = new OnlineApi.Room[1]; StartCoroutine(Online(OnlineApi.QuickPlay(r => box[0] = r), box, "Oda aranıyor...")); }
        void RunCreate() { var box = new OnlineApi.Room[1]; StartCoroutine(Online(OnlineApi.CreateRoom(r => box[0] = r), box, "Özel oda hazırlanıyor...")); }
        void RunJoin() { var box = new OnlineApi.Room[1]; StartCoroutine(Online(OnlineApi.JoinRoom(code, r => box[0] = r), box, "Oda bulunuyor...")); }

        // ---------- character preview ----------
        void EnsurePreview()
        {
            if (previewCam != null) return;
            stage = new GameObject("CharacterPreviewStage").transform;
            stage.position = new Vector3(500f, 500f, 500f);
            previewRt = new RenderTexture(512, 640, 24);
            var camGo = new GameObject("PreviewCamera");
            camGo.transform.SetParent(stage, false);
            camGo.transform.localPosition = new Vector3(0f, 1.0f, 4.3f);
            camGo.transform.localRotation = Quaternion.Euler(4f, 180f, 0f);
            previewCam = camGo.AddComponent<Camera>();
            previewCam.fieldOfView = 28f;
            previewCam.clearFlags = CameraClearFlags.SolidColor;
            previewCam.backgroundColor = new Color(0.13f, 0.22f, 0.18f);
            previewCam.cullingMask = 1 << 30;
            previewCam.targetTexture = previewRt;
            previewCam.depth = -10f;
            var lightGo = new GameObject("PreviewLight");
            lightGo.transform.SetParent(stage, false);
            lightGo.transform.rotation = Quaternion.Euler(30f, 200f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            l.cullingMask = 1 << 30;
        }

        void ShowPreview()
        {
            EnsurePreview();
            if (shownIndex == charIndex && previewModel != null) return;
            if (previewModel != null) Destroy(previewModel);
            previewModel = CharacterCatalog.Build(charIndex, stage);
            foreach (var t in previewModel.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            shownIndex = charIndex;
        }

        void Update()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && connectingSince > 0f && nm.IsConnectedClient) connectingSince = -1f;
            if (nm != null && connectingSince > 0f && Time.time - connectingSince > 15f)
            {
                nm.Shutdown();
                connectingSince = -1f;
                busy = false;
                status = "Sunucuya bağlanılamadı (zaman aşımı). Biraz sonra tekrar dene.";
            }

            if (previewCam != null) previewCam.enabled = page == Page.Characters;
            if (page == Page.Characters)
            {
                ShowPreview();
                if (previewModel != null) previewModel.transform.localRotation = Quaternion.Euler(0f, Time.time * 38f, 0f);
            }
        }

        // ---------- UI ----------
        bool Button(Rect r, string text, bool primary = false)
        {
            var prev = GUI.backgroundColor;
            if (primary) GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            var pressed = GUI.Button(r, text, btn);
            GUI.backgroundColor = prev;
            return pressed;
        }

        void ChooseCharacter(int index)
        {
            charIndex = (index + CharacterCatalog.Count) % CharacterCatalog.Count;
            CharacterCatalog.Save(charIndex);
        }

        // ---------- backdrop: a map-independent dusk gradient with drifting soft lights (covers the 3D scene behind the menu) ----------
        Texture2D logo, gradient, glow;

        void DrawBackdrop()
        {
            if (gradient == null)
            {
                gradient = new Texture2D(1, 256, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var top = new Color(0.05f, 0.09f, 0.17f);
                var mid = new Color(0.12f, 0.27f, 0.38f);
                var low = new Color(0.93f, 0.58f, 0.24f);
                for (var y = 0; y < 256; y++)
                {
                    var t = y / 255f;               // 0 = bottom of the screen
                    var c = t < 0.35f ? Color.Lerp(low, mid, Mathf.SmoothStep(0f, 1f, t / 0.35f)) : Color.Lerp(mid, top, Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f));
                    gradient.SetPixel(0, y, c);
                }
                gradient.Apply();
                glow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                for (var y = 0; y < 64; y++)
                    for (var x = 0; x < 64; x++)
                    {
                        var d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 32f;
                        var a = Mathf.Clamp01(1f - d);
                        glow.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (0.55f + 0.45f * Mathf.SmoothStep(0.8f, 0.95f, d))));
                    }
                glow.Apply();
            }
            var prevMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            var sw = (float)Screen.width; var sh = (float)Screen.height;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), gradient, ScaleMode.StretchToFill, false);
            var prevColor = GUI.color;
            for (var i = 0; i < 16; i++)
            {
                var seed = i * 12.9898f;
                var speed = 0.006f + (i % 5) * 0.004f;
                var x = Mathf.Repeat(Mathf.Sin(seed) * 0.5f + 0.5f + Time.time * speed, 1.2f) - 0.1f;
                var y = Mathf.Repeat(Mathf.Cos(seed * 1.7f) * 0.5f + 0.5f - Time.time * speed * 0.7f, 1f);
                var size = sh * (0.08f + (i % 4) * 0.07f);
                var warm = i % 3 == 0;
                GUI.color = new Color(warm ? 1f : 0.55f, warm ? 0.78f : 0.85f, warm ? 0.45f : 1f, 0.07f + (i % 4) * 0.025f);
                GUI.DrawTexture(new Rect(x * sw - size / 2f, y * sh - size / 2f, size, size), glow, ScaleMode.StretchToFill, true);
            }
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || InstallGuard.Blocked) return;
            if (nm.IsListening && (nm.IsHost || nm.IsConnectedClient || (nm.IsServer && !nm.IsClient))) return;

            DrawBackdrop();
            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            if (logo == null) logo = Resources.Load<Texture2D>("Brand/logo");
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                btn = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
                field = new GUIStyle(GUI.skin.textField) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
            }
            title.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Box(new Rect(w / 2f - 440, 36, 880, 984), "");
            if (logo != null) GUI.DrawTexture(new Rect(w / 2f - 340, 48, 680, 170), logo, ScaleMode.ScaleToFit, true);
            else GUI.Label(new Rect(w / 2f - 420, 90, 840, 90), "Who Broke It?", title);
            GUI.Label(new Rect(w / 2f - 420, 205, 840, 40), "Greenfield · Kimin Alet Çantası?", label);

            var connecting = nm.IsListening && nm.IsClient && !nm.IsConnectedClient;
            GUI.enabled = !busy && !connecting;
            var x = w / 2f - 320;
            var fp = ThirdPersonCamera.FirstPersonPreferred;

            if (page == Page.Main)
            {
                if (Button(new Rect(x, 250, 640, 84), "Hızlı oyun (online)", true)) RunQuick();
                GUI.Label(new Rect(x, 336, 640, 34), "Açık bir odaya otomatik girersin, orada kimse varsa onlarla oynarsın.", small);
                if (Button(new Rect(x, 386, 640, 84), "Arkadaşlarla oyna (oda kodu)")) { page = Page.Friends; status = ""; }
                GUI.Label(new Rect(x, 472, 640, 34), "Özel oda aç, kodu arkadaşlarına söyle.", small);
                if (Button(new Rect(x, 522, 640, 70), "Tek kişilik oyna")) HostLocal("127.0.0.1");

                var prevFont = btn.fontSize; btn.fontSize = 22;
                var charPressed = Button(new Rect(x, 616, 310, 64), "Karakter: " + CharacterCatalog.All[charIndex].name);
                btn.fontSize = prevFont;
                if (charPressed) { page = Page.Characters; status = ""; }
                if (Button(new Rect(x + 330, 616, 310, 64), fp ? "Kamera: 1. şahıs" : "Kamera: 3. şahıs")) ThirdPersonCamera.FirstPersonPreferred = !fp;
                GUI.Label(new Rect(x, 684, 640, 30), "Oyun içinde V tuşu kamerayı değiştirir.", small);

                if (Button(new Rect(x, 730, 640, 56), "Gelişmiş: IP ile bağlan / LAN odası")) { page = Page.Advanced; status = ""; }
            }
            else if (page == Page.Characters)
            {
                var entry = CharacterCatalog.All[charIndex];
                GUI.Label(new Rect(w / 2f - 420, 225, 840, 40), "Karakterini seç", label);
                if (previewRt != null) GUI.DrawTexture(new Rect(w / 2f - 160, 270, 320, 400), previewRt, ScaleMode.ScaleToFit, false);
                if (Button(new Rect(w / 2f - 330, 420, 140, 90), "◀")) ChooseCharacter(charIndex - 1);
                if (Button(new Rect(w / 2f + 190, 420, 140, 90), "▶")) ChooseCharacter(charIndex + 1);
                title.fontSize = 40;
                GUI.Label(new Rect(w / 2f - 420, 685, 840, 60), entry.name + "  (" + (charIndex + 1) + "/" + CharacterCatalog.Count + ")", title);
                title.fontSize = 64;
                GUI.Label(new Rect(w / 2f - 380, 745, 760, 70), entry.blurb, label);
                GUI.Label(new Rect(x, 820, 640, 30), "Seçimin kaydedilir; odaya girince diğer oyuncular da seni bu karakterle görür.", small);
                if (Button(new Rect(x, 870, 640, 70), "Tamam", true)) page = Page.Main;
            }
            else if (page == Page.Friends)
            {
                if (Button(new Rect(x, 270, 640, 84), "Özel oda kur", true)) RunCreate();
                GUI.Label(new Rect(x, 356, 640, 34), "Oda kodu oyunun içinde ekranın sağ üstünde görünür.", small);
                GUI.Label(new Rect(x, 430, 640, 36), "Arkadaşının odasına katıl: kodu yaz", label);
                code = GUI.TextField(new Rect(x, 476, 640, 64), code, 10, field).ToUpperInvariant();
                if (Button(new Rect(x, 556, 640, 74), "Koda katıl") && code.Trim().Length >= 4) RunJoin();
                if (Button(new Rect(x, 654, 640, 56), "← Geri")) { page = Page.Main; status = ""; }
            }
            else
            {
                GUI.Label(new Rect(x, 260, 640, 60), "Aynı ağdakilerle ya da VPN üzerinden doğrudan oynamak için.", label);
                if (Button(new Rect(x, 330, 640, 74), "LAN odası kur")) HostLocal("0.0.0.0");
                GUI.Label(new Rect(x, 408, 640, 52), "Bu bilgisayar: " + myIps + " · UDP " + lanPort, small);
                address = GUI.TextField(new Rect(x, 476, 640, 64), address, 40, field);
                if (Button(new Rect(x, 556, 640, 74), "Bu adrese katıl")) Connect(address.Trim(), lanPort);
                if (Button(new Rect(x, 654, 640, 56), "← Geri")) { page = Page.Main; status = ""; }
            }
            GUI.enabled = true;

            if (page != Page.Characters)
            {
                if (connecting || busy)
                    GUI.Label(new Rect(x, 800, 640, 50), connecting ? "Bağlanıyor..." : status, label);
                else if (!string.IsNullOrEmpty(status))
                {
                    label.normal.textColor = new Color(1f, 0.55f, 0.5f);
                    GUI.Label(new Rect(x - 40, 800, 720, 120), status, label);
                    label.normal.textColor = Color.white;
                }
            }
            GUI.Label(new Rect(w / 2f - 420, 975, 840, 30), "Sürüm " + Application.version, small);
        }
    }
}
