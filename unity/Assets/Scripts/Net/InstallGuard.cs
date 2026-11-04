using System;
using System.IO;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// The game is distributed with an installer. On Windows, a copy that is not installed (no Uninstall.exe next to the exe,
    /// e.g. a zip unpacked by hand) shows a blocking notice instead of the menu. "-allowportable" skips the check (development).
    /// </summary>
    public class InstallGuard : MonoBehaviour
    {
        public static bool Blocked { get; private set; }
        public string downloadPage = "https://whobrokeit.cicibyte.com/#indir";
        GUIStyle title, body, btn;

        void Awake()
        {
#if UNITY_EDITOR
            return;
#else
            if (Application.platform != RuntimePlatform.WindowsPlayer) return;
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-allowportable") >= 0 || Array.IndexOf(args, "-smoke") >= 0 || Array.IndexOf(args, "-server") >= 0) return;
            try
            {
                var dir = Directory.GetParent(Application.dataPath).FullName;
                Blocked = !File.Exists(Path.Combine(dir, "Uninstall.exe"));
            }
            catch { Blocked = false; }
#endif
        }

        void OnGUI()
        {
            if (!Blocked) return;
            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                body = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                btn = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
            }
            GUI.depth = -1000;
            GUI.color = Color.white;
            GUI.Box(new Rect(0, 0, w, 1080), "");
            GUI.Box(new Rect(0, 0, w, 1080), "");
            title.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(w / 2f - 520, 250, 1040, 80), "Önce kurman gerekiyor", title);
            GUI.Label(new Rect(w / 2f - 520, 350, 1040, 160),
                "Who Broke It? doğrudan çalıştırılmaz. Kurulum dosyasını (WhoBrokeIt-Setup) indirip çalıştır; oyun varsayılan olarak C:\\Program Files\\CiciByte\\WhoBrokeIt klasörüne kurulur ve kendini güncel tutar.", body);
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.82f, 0.25f);
            if (GUI.Button(new Rect(w / 2f - 280, 560, 560, 84), "Kurulum dosyasını indir", btn)) Application.OpenURL(downloadPage);
            GUI.backgroundColor = prev;
            if (GUI.Button(new Rect(w / 2f - 280, 664, 560, 64), "Çık", btn)) Application.Quit();
        }
    }
}
