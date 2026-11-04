using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Main menu. Two online ways in, both through dedicated servers: quick play (public room, joins strangers automatically) and
    /// private rooms (share a code with friends). Solo play and direct IP/LAN stay available.
    /// </summary>
    public class NetworkMenu : MonoBehaviour
    {
        public ushort lanPort = 7777;

        enum Page { Main, Friends, Advanced }
        Page page = Page.Main;
        string code = "";
        string address = "127.0.0.1";
        string status = "";
        bool busy;
        float connectingSince = -1f;
        string myIps = "";
        GUIStyle title, label, btn, small, field;

        void Start()
        {
            if (DedicatedServer.IsDedicated || System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-smoke") >= 0) { enabled = false; return; }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            myIps = LocalAddresses();
            var nm = NetworkManager.Singleton;
            if (nm != null) nm.OnClientDisconnectCallback += OnDisconnected;
        }

        void OnDestroy()
        {
            if (NetworkManager.Singleton != null) NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
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

        void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || InstallGuard.Blocked) return;
            if (nm.IsListening && (nm.IsHost || nm.IsConnectedClient || (nm.IsServer && !nm.IsClient))) return;

            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                btn = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
                field = new GUIStyle(GUI.skin.textField) { fontSize = 30, alignment = TextAnchor.MiddleCenter };
            }
            title.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Box(new Rect(w / 2f - 440, 100, 880, 860), "");
            GUI.Label(new Rect(w / 2f - 420, 120, 840, 90), "Who Broke It?", title);
            GUI.Label(new Rect(w / 2f - 420, 205, 840, 40), "Greenfield · Kimin Alet Çantası?", label);

            var connecting = nm.IsListening && nm.IsClient && !nm.IsConnectedClient;
            GUI.enabled = !busy && !connecting;
            var x = w / 2f - 320;

            if (page == Page.Main)
            {
                if (Button(new Rect(x, 290, 640, 84), "Hızlı oyun (online)", true)) RunQuick();
                GUI.Label(new Rect(x, 376, 640, 34), "Açık bir odaya otomatik girersin, orada kimse varsa onlarla oynarsın.", small);
                if (Button(new Rect(x, 430, 640, 84), "Arkadaşlarla oyna (oda kodu)")) { page = Page.Friends; status = ""; }
                GUI.Label(new Rect(x, 516, 640, 34), "Özel oda aç, kodu arkadaşlarına söyle.", small);
                if (Button(new Rect(x, 570, 640, 70), "Tek kişilik oyna")) HostLocal("127.0.0.1");
                if (Button(new Rect(x, 664, 640, 56), "Gelişmiş: IP ile bağlan / LAN odası")) { page = Page.Advanced; status = ""; }
            }
            else if (page == Page.Friends)
            {
                if (Button(new Rect(x, 290, 640, 84), "Özel oda kur", true)) RunCreate();
                GUI.Label(new Rect(x, 376, 640, 34), "Oda kodu oyunun içinde ekranın sağ üstünde görünür.", small);
                GUI.Label(new Rect(x, 440, 640, 36), "Arkadaşının odasına katıl: kodu yaz", label);
                code = GUI.TextField(new Rect(x, 486, 640, 64), code, 10, field).ToUpperInvariant();
                if (Button(new Rect(x, 566, 640, 74), "Koda katıl") && code.Trim().Length >= 4) RunJoin();
                if (Button(new Rect(x, 664, 640, 56), "← Geri")) { page = Page.Main; status = ""; }
            }
            else
            {
                GUI.Label(new Rect(x, 270, 640, 60), "Aynı ağdakilerle ya da VPN üzerinden doğrudan oynamak için.", label);
                if (Button(new Rect(x, 340, 640, 74), "LAN odası kur")) HostLocal("0.0.0.0");
                GUI.Label(new Rect(x, 418, 640, 52), "Bu bilgisayar: " + myIps + " · UDP " + lanPort, small);
                address = GUI.TextField(new Rect(x, 486, 640, 64), address, 40, field);
                if (Button(new Rect(x, 566, 640, 74), "Bu adrese katıl")) Connect(address.Trim(), lanPort);
                if (Button(new Rect(x, 664, 640, 56), "← Geri")) { page = Page.Main; status = ""; }
            }
            GUI.enabled = true;

            if (connecting || busy)
                GUI.Label(new Rect(x, 750, 640, 50), connecting ? "Bağlanıyor..." : status, label);
            else if (!string.IsNullOrEmpty(status))
            {
                label.normal.textColor = new Color(1f, 0.55f, 0.5f);
                GUI.Label(new Rect(x - 40, 750, 720, 120), status, label);
                label.normal.textColor = Color.white;
            }
            GUI.Label(new Rect(w / 2f - 420, 900, 840, 30), "Sürüm " + Application.version, small);
        }
    }
}
