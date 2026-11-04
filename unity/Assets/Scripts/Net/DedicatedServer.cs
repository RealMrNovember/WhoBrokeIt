using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Networking;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Headless dedicated server mode: "-server -port 7771 -slot s1 -api http://matchmaker:8100".
    /// Reports its player count to the matchmaker every few seconds and learns its room code. When the matchmaker says "reset"
    /// (room empty for a while) the process quits; Docker restarts it with a fresh world.
    /// </summary>
    public class DedicatedServer : MonoBehaviour
    {
        [Serializable] class Beat { public string slot; public int players; public string version; }
        [Serializable] class Reply { public string code; public string type; public bool reset; }

        public static bool IsDedicated { get; private set; }

        string slot = "local";
        ushort port = 7777;
        string api = "";

        static string Arg(string name, string fallback)
        {
            var a = Environment.GetCommandLineArgs();
            var i = Array.IndexOf(a, name);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : fallback;
        }

        void Start()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-server") < 0) return;
            IsDedicated = true;
            slot = Arg("-slot", "local");
            port = ushort.TryParse(Arg("-port", "7777"), out var p) ? p : (ushort)7777;
            api = Arg("-api", "");
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            // runs under a virtual display with software GL: never render anything
            foreach (var cam in Camera.allCameras) cam.enabled = false;
            foreach (var l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) l.enabled = false;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("0.0.0.0", port, "0.0.0.0");
            if (!nm.StartServer())
            {
                Debug.LogError("[Server] StartServer başarısız");
                Application.Quit(1);
                yield break;
            }
            Debug.Log("[Server] " + slot + " hazır, UDP " + port + ", sürüm " + Application.version);

            while (true)
            {
                yield return new WaitForSeconds(5f);
                if (string.IsNullOrEmpty(api)) continue;
                var body = JsonUtility.ToJson(new Beat { slot = slot, players = nm.ConnectedClientsIds.Count, version = Application.version });
                using (var req = new UnityWebRequest(api + "/server/heartbeat", "POST"))
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    req.downloadHandler = new DownloadHandlerBuffer();
                    req.SetRequestHeader("Content-Type", "application/json");
                    req.timeout = 4;
                    yield return req.SendWebRequest();
                    if (req.result != UnityWebRequest.Result.Success) continue;
                    var reply = JsonUtility.FromJson<Reply>(req.downloadHandler.text);
                    if (reply == null) continue;
                    if (CaseManager.I != null && CaseManager.I.IsSpawned)
                        CaseManager.I.roomCode.Value = new Unity.Collections.FixedString32Bytes(reply.code ?? "");
                    if (reply.reset)
                    {
                        Debug.Log("[Server] oda boşaldı, yeniden başlatılıyor");
                        Application.Quit(0);
                        yield break;
                    }
                }
            }
        }
    }
}
