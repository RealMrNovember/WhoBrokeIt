using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>Minimal Host / Join screen. Transport is direct IP (works over LAN and Tailscale); Steam lobbies replace this later.</summary>
    public class NetworkMenu : MonoBehaviour
    {
        public ushort port = 7777;
        string address = "127.0.0.1";
        string status = "";
        GUIStyle title, label, btn;
        string myIps;

        void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            myIps = LocalAddresses();
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

        void StartHostNow(string listen)
        {
            var nm = NetworkManager.Singleton;
            var ut = (UnityTransport)nm.NetworkConfig.NetworkTransport;
            ut.SetConnectionData("127.0.0.1", port, listen);
            if (!nm.StartHost()) status = "Sunucu başlatılamadı (port dolu olabilir).";
        }

        void OnGUI()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            if (nm.IsListening)
            {
                if (nm.IsServer && !nm.IsClient) return;
                if (nm.IsConnectedClient || nm.IsHost) return;
                GUI.Label(new Rect(20, 20, 600, 40), "Bağlanıyor...");
                return;
            }

            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                btn = new GUIStyle(GUI.skin.button) { fontSize = 28 };
            }
            title.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Box(new Rect(w / 2f - 420, 140, 840, 760), "");
            GUI.Label(new Rect(w / 2f - 400, 160, 800, 90), "Who Broke It?", title);
            GUI.Label(new Rect(w / 2f - 400, 250, 800, 40), "Greenfield · Kimin Alet Çantası?", label);

            if (GUI.Button(new Rect(w / 2f - 300, 330, 600, 70), "Tek kişilik oyna", btn)) StartHostNow("127.0.0.1");
            if (GUI.Button(new Rect(w / 2f - 300, 420, 600, 70), "Oda kur (arkadaşlar katılabilir)", btn)) StartHostNow("0.0.0.0");

            GUI.Label(new Rect(w / 2f - 300, 520, 600, 36), "Odaya katıl: sunucu adresi", label);
            address = GUI.TextField(new Rect(w / 2f - 300, 562, 600, 52), address, 40, new GUIStyle(GUI.skin.textField) { fontSize = 28, alignment = TextAnchor.MiddleCenter });
            if (GUI.Button(new Rect(w / 2f - 300, 630, 600, 70), "Katıl", btn))
            {
                var ut = (UnityTransport)nm.NetworkConfig.NetworkTransport;
                ut.SetConnectionData(address.Trim(), port);
                if (!nm.StartClient()) status = "Bağlantı başlatılamadı.";
            }

            GUI.Label(new Rect(w / 2f - 400, 730, 800, 80),
                "Oda kurunca arkadaşların bu bilgisayarın adresini yazar (Tailscale adresi ya da yerel ağ adresi). Bu bilgisayar: " + myIps + "  · port " + port, label);
            if (!string.IsNullOrEmpty(status)) GUI.Label(new Rect(w / 2f - 400, 830, 800, 50), status, label);
        }
    }
}
