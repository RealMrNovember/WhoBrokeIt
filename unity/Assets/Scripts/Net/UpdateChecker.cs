using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Self-updater for the direct-download (non-Steam) build. On start it reads latest.json from the site; if a newer version exists
    /// it downloads the zip, verifies SHA-256, and (when no session is running) restarts into the new version via a small batch script.
    /// Disabled in the editor, on non-Windows, with "-noupdate", or in Steam builds (define STEAM_BUILD).
    /// </summary>
    public class UpdateChecker : MonoBehaviour
    {
        [Serializable]
        class Manifest { public string version; public string url; public string sha256; public string notes; }

        public string manifestUrl = "https://whobrokeit.cicibyte.com/builds/latest.json";

        enum State { Idle, Checking, Downloading, Ready, Applying, Error }
        State state = State.Idle;
        Manifest latest;
        string zipPath;
        float progress;
        string message = "";
        float applyAt = -1f;
        GUIStyle style;

        void Start()
        {
#if STEAM_BUILD || UNITY_EDITOR
            return;
#else
            if (Application.platform != RuntimePlatform.WindowsPlayer) return;
            if (InstallGuard.Blocked) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-noupdate") >= 0) return;
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-smoke") >= 0) return;
            StartCoroutine(Run());
#endif
        }

        static bool IsNewer(string remote, string local)
        {
            return Version.TryParse(remote, out var r) && Version.TryParse(local, out var l) && r > l;
        }

        IEnumerator Run()
        {
            state = State.Checking;
            using (var req = UnityWebRequest.Get(manifestUrl))
            {
                req.timeout = 10;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) { state = State.Idle; Debug.Log("[Update] kontrol edilemedi: " + req.error); yield break; }
                latest = JsonUtility.FromJson<Manifest>(req.downloadHandler.text);
            }
            if (latest == null || string.IsNullOrEmpty(latest.version) || !IsNewer(latest.version, Application.version))
            {
                state = State.Idle;
                Debug.Log("[Update] güncel (" + Application.version + ")");
                yield break;
            }

            Debug.Log("[Update] yeni sürüm " + latest.version);
            state = State.Downloading;
            var dir = Path.Combine(Application.persistentDataPath, "update");
            Directory.CreateDirectory(dir);
            zipPath = Path.Combine(dir, "WhoBrokeIt-" + latest.version + ".zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            var url = latest.url.StartsWith("http") ? latest.url : new Uri(new Uri(manifestUrl), latest.url).ToString();
            using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            {
                req.downloadHandler = new DownloadHandlerFile(zipPath);
                var op = req.SendWebRequest();
                while (!op.isDone) { progress = req.downloadProgress; yield return null; }
                if (req.result != UnityWebRequest.Result.Success) { Fail("indirilemedi: " + req.error); yield break; }
            }

            if (!string.IsNullOrEmpty(latest.sha256) && !string.Equals(Sha256(zipPath), latest.sha256, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(zipPath);
                Fail("dosya doğrulanamadı");
                yield break;
            }
            state = State.Ready;
            message = "Güncelleme hazır: " + latest.version;
        }

        void Fail(string why)
        {
            state = State.Error;
            message = "Güncelleme " + why;
            Debug.Log("[Update] " + message);
        }

        static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                var sb = new StringBuilder();
                foreach (var b in sha.ComputeHash(fs)) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        bool InSession()
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsListening;
        }

        void Update()
        {
            if (state != State.Ready) return;
            // not in a session: apply right away (short grace so the player sees why the window closes)
            if (!InSession())
            {
                if (applyAt < 0f) applyAt = Time.unscaledTime + 2.5f;
                if (Time.unscaledTime >= applyAt) Apply();
            }
            else applyAt = -1f;
        }

        void Apply()
        {
            state = State.Applying;
            try
            {
                var installDir = Directory.GetParent(Application.dataPath).FullName;
                var exe = Process.GetCurrentProcess().MainModule.FileName;
                var stage = Path.Combine(Application.persistentDataPath, "update", "stage");
                var bat = Path.Combine(Application.persistentDataPath, "update", "apply.bat");
                var script =
                    "@echo off\r\n" +
                    "timeout /t 2 /nobreak >nul\r\n" +
                    "if exist \"" + stage + "\" rmdir /s /q \"" + stage + "\"\r\n" +
                    "powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -Force -LiteralPath '" + zipPath + "' -DestinationPath '" + stage + "'\"\r\n" +
                    "robocopy \"" + stage + "\\WhoBrokeIt\" \"" + installDir + "\" /E /R:5 /W:1 /NFL /NDL /NJH /NJS >nul\r\n" +
                    "start \"\" \"" + exe + "\"\r\n";
                File.WriteAllText(bat, script, Encoding.ASCII);
                Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + bat + "\"") { CreateNoWindow = true, UseShellExecute = false });
                Application.Quit();
            }
            catch (Exception e)
            {
                Fail("kurulamadı: " + e.Message);
            }
        }

        void OnGUI()
        {
            if (state == State.Idle || state == State.Checking) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            var r = new Rect(Screen.width / 2f - 300, 10, 600, 44);
            GUI.Box(r, "");
            switch (state)
            {
                case State.Downloading:
                    GUI.Label(r, "Yeni sürüm " + (latest != null ? latest.version : "") + " indiriliyor... %" + Mathf.RoundToInt(progress * 100f), style);
                    break;
                case State.Ready:
                    if (InSession())
                    {
                        if (GUI.Button(new Rect(r.x + 380, r.y + 6, 210, 32), "Şimdi yeniden başlat")) Apply();
                        GUI.Label(new Rect(r.x, r.y, 380, r.height), message + " (oyun bitince)", style);
                    }
                    else GUI.Label(r, message + " · yeniden başlatılıyor...", style);
                    break;
                case State.Applying:
                    GUI.Label(r, "Güncelleniyor...", style);
                    break;
                case State.Error:
                    GUI.Label(r, message, style);
                    break;
            }
        }
    }
}
