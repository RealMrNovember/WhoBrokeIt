using Unity.Netcode;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>Writes the session events (start, players joining and leaving, end with its length) to the local telemetry log. Server side only.</summary>
    public class TelemetryRecorder : MonoBehaviour
    {
        bool hooked;
        float startedAt;
        int peak;

        void Update()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || hooked) return;
            hooked = true;
            nm.OnServerStarted += () => { startedAt = Time.realtimeSinceStartup; peak = 1; Telemetry.Log("session_start", "\"dedicated\":" + (DedicatedServer.IsDedicated ? "true" : "false")); };
            nm.OnClientConnectedCallback += id =>
            {
                if (!nm.IsServer) return;
                peak = Mathf.Max(peak, nm.ConnectedClientsIds.Count);
                Telemetry.Log("player_join", "\"players\":" + nm.ConnectedClientsIds.Count);
            };
            nm.OnClientDisconnectCallback += id =>
            {
                if (!nm.IsServer) return;
                Telemetry.Log("player_leave", "\"players\":" + Mathf.Max(0, nm.ConnectedClientsIds.Count - 1));
            };
            nm.OnServerStopped += _ => Telemetry.Log("session_end", "\"seconds\":" + Mathf.RoundToInt(Time.realtimeSinceStartup - startedAt) + ",\"peak\":" + peak);
        }
    }
}
