using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Net;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace WhoBrokeIt.Net
{
    /// <summary>Graphics quality presets (Low / Medium / High) applied to the URP asset at runtime; the first start picks one from the hardware.</summary>
    public static class GraphicsQuality
    {
        const string PrefKey = "wbi_quality";
        public static readonly string[] Names = { "Düşük", "Orta", "Yüksek" };

        /// <summary>-1 until the player or the automatic detection has chosen.</summary>
        public static int Level => PlayerPrefs.GetInt(PrefKey, -1);

        public static int Detect()
        {
            var vram = SystemInfo.graphicsMemorySize;
            var gpu = SystemInfo.graphicsDeviceName ?? "";
            var level = vram < 2500 ? 0 : vram < 5000 ? 1 : 2;
            var integrated = (gpu.Contains("Intel") && !gpu.Contains("Arc")) || gpu.Contains("Radeon(TM) Graphics") || gpu.Contains("Vega") || gpu.Contains("UHD") || gpu.Contains("Iris");
            if (integrated) level = 0;
            if (SystemInfo.processorCount < 6) level = Mathf.Max(0, level - 1);
            return level;
        }

        public static void Apply(int level, bool save = true)
        {
            level = Mathf.Clamp(level, 0, 2);
            if (save) { PlayerPrefs.SetInt(PrefKey, level); PlayerPrefs.Save(); }
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset != null)
            {
                asset.renderScale = level == 0 ? 0.7f : level == 1 ? 0.85f : 1f;
                asset.shadowDistance = level == 0 ? 22f : level == 1 ? 40f : 60f;
                asset.shadowCascadeCount = level == 0 ? 1 : level == 1 ? 2 : 3;
                asset.msaaSampleCount = level == 0 ? 1 : level == 1 ? 2 : 4;
            }
            var sun = RenderSettings.sun;
            if (sun != null) sun.shadows = level == 0 ? LightShadows.Hard : LightShadows.Soft;
            foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)) v.enabled = level > 0;
            foreach (var c in Object.FindObjectsByType<UniversalAdditionalCameraData>(FindObjectsSortMode.None)) c.renderPostProcessing = level > 0;
        }

        public static void Cycle() { Apply(((Level < 0 ? Detect() : Level) + 1) % 3); }
    }

    /// <summary>Small FPS / ping read-out (F3 toggles) and automatic quality drop when the frame rate stays low.</summary>
    public class PerfOverlay : MonoBehaviour
    {
        static bool show = true;
        float timer, frames, shownFps, minFps = 999f, shownMin, lowSeconds, accumTime;
        GUIStyle style;

        void Start()
        {
            if (DedicatedServer.IsDedicated) { enabled = false; return; }
            show = PlayerPrefs.GetInt("wbi_perf", 1) == 1;
            GraphicsQuality.Apply(GraphicsQuality.Level < 0 ? GraphicsQuality.Detect() : GraphicsQuality.Level, GraphicsQuality.Level < 0);
            Audio.GameAudio.Apply();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) { show = !show; PlayerPrefs.SetInt("wbi_perf", show ? 1 : 0); }

            var dt = Time.unscaledDeltaTime;
            timer += dt; frames++;
            if (dt > 0f) minFps = Mathf.Min(minFps, 1f / dt);
            if (timer >= 0.5f)
            {
                shownFps = frames / timer; shownMin = minFps;
                timer = 0f; frames = 0f; minFps = 999f;
                // a long stretch of low frame rate while playing: step the quality down once
                var nm = NetworkManager.Singleton;
                var playing = nm != null && nm.IsListening && (nm.IsHost || nm.IsConnectedClient);
                lowSeconds = playing && shownFps < 24f ? lowSeconds + 0.5f : 0f;
                if (lowSeconds >= 8f)
                {
                    lowSeconds = 0f;
                    var cur = GraphicsQuality.Level < 0 ? GraphicsQuality.Detect() : GraphicsQuality.Level;
                    if (cur > 0)
                    {
                        GraphicsQuality.Apply(cur - 1);
                        Case.CaseManager.Toast("FPS düşük: grafik kalitesi " + GraphicsQuality.Names[cur - 1] + " yapıldı (Esc menüsünden değiştirebilirsin).");
                    }
                }
            }
        }

        void OnGUI()
        {
            if (!show) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperRight };
            var nm = NetworkManager.Singleton;
            var ping = "";
            if (nm != null && nm.IsListening && nm.IsConnectedClient && !nm.IsHost)
            {
                var ut = nm.NetworkConfig.NetworkTransport as UnityTransport;
                if (ut != null) ping = " · ping " + ut.GetCurrentRtt(NetworkManager.ServerClientId) + " ms";
            }
            var q = GraphicsQuality.Level >= 0 ? GraphicsQuality.Names[GraphicsQuality.Level] : "?";
            var text = "FPS " + shownFps.ToString("0") + " (en düşük " + shownMin.ToString("0") + ")" + ping + " · " + Screen.width + "x" + Screen.height + " · kalite " + q + " · F3";
            style.normal.textColor = shownFps < 30f ? new Color(1f, 0.5f, 0.45f) : new Color(0.85f, 1f, 0.85f);
            LocGui.Label(new Rect(Screen.width - 620, 4, 612, 24), text, style);
        }
    }
}
