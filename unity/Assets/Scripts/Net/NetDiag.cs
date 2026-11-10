using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Network health log for playtests: every 5 seconds a [NET] line in Player.log with this machine frame rate (average and worst), the round trip time
    /// to the server and, for every other player, how long ago his replicated position last changed and how far it moved (a character that looks frozen
    /// shows up as a large age while he is still sending nothing). On the server the same is logged per client. No personal data.
    /// </summary>
    public class NetDiag : MonoBehaviour
    {
        float windowStart, worstFrame, frames;
        float nextLog = 5f, nextScan;
        PlayerNet[] players = new PlayerNet[0];

        struct Seen { public Vector3 pos; public float changedAt; public float moved; }
        readonly System.Collections.Generic.Dictionary<ulong, Seen> seen = new System.Collections.Generic.Dictionary<ulong, Seen>();

        void Update()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening) { windowStart = Time.unscaledTime; frames = 0; worstFrame = 0; return; }
            frames++;
            worstFrame = Mathf.Max(worstFrame, Time.unscaledDeltaTime);

            if (Time.unscaledTime > nextScan) { nextScan = Time.unscaledTime + 0.5f; players = FindPlayers(); }
            foreach (var p in players)
            {
                if (p == null || p.IsOwner) continue;
                var id = p.OwnerClientId;
                var pos = p.transform.position;
                if (!seen.TryGetValue(id, out var s)) s = new Seen { pos = pos, changedAt = Time.unscaledTime };
                var d = Vector3.Distance(pos, s.pos);
                if (d > 0.02f) { s.moved += d; s.pos = pos; s.changedAt = Time.unscaledTime; }
                seen[id] = s;
            }

            if (Time.unscaledTime < nextLog) return;
            nextLog = Time.unscaledTime + 5f;
            var span = Mathf.Max(0.01f, Time.unscaledTime - windowStart);
            var sb = new StringBuilder("[NET] fps=" + (frames / span).ToString("0") + " worstFrame=" + Mathf.RoundToInt(worstFrame * 1000f) + "ms");
            var ut = nm.NetworkConfig.NetworkTransport as UnityTransport;
            if (nm.IsServer)
            {
                foreach (var cid in nm.ConnectedClientsIds)
                {
                    if (cid == NetworkManager.ServerClientId) continue;
                    sb.Append(" c").Append(cid).Append(" rtt=").Append(ut != null ? ut.GetCurrentRtt(cid).ToString() : "?").Append("ms");
                }
            }
            else if (ut != null) sb.Append(" rtt=").Append(ut.GetCurrentRtt(NetworkManager.ServerClientId)).Append("ms");
            foreach (var kv in seen)
                sb.Append(" | p").Append(kv.Key).Append(" idle=").Append((Time.unscaledTime - kv.Value.changedAt).ToString("0.0")).Append("s moved=").Append(kv.Value.moved.ToString("0.0")).Append("m");
            Debug.Log(sb.ToString());
            windowStart = Time.unscaledTime; frames = 0; worstFrame = 0;
            var keys = new System.Collections.Generic.List<ulong>(seen.Keys);
            foreach (var k in keys) { var s = seen[k]; s.moved = 0f; seen[k] = s; }
        }

        static PlayerNet[] FindPlayers() => FindObjectsByType<PlayerNet>(FindObjectsSortMode.None);
    }
}
