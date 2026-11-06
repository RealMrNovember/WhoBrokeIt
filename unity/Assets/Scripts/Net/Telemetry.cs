using System.IO;
using System.Text;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Local, anonymous play log for the playtest (MVP section 4): one JSON object per line in telemetry.jsonl next to the save data.
    /// Nothing is sent anywhere; the host (or the dedicated server) writes only game events: no names, no addresses.
    /// Turn it off with PlayerPrefs "wbi_telemetry" = 0.
    /// </summary>
    public static class Telemetry
    {
        static string path;
        static readonly object gate = new object();

        public static string Path
        {
            get
            {
                if (path == null) path = System.IO.Path.Combine(Application.persistentDataPath, "telemetry.jsonl");
                return path;
            }
        }

        public static void Log(string ev, string details = "")
        {
            if (PlayerPrefs.GetInt("wbi_telemetry", 1) == 0) return;
            try
            {
                var sb = new StringBuilder();
                sb.Append("{\"t\":").Append(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                  .Append(",\"v\":\"").Append(Application.version).Append("\",\"e\":\"").Append(ev).Append('"');
                if (details.Length > 0) sb.Append(",").Append(details);
                sb.Append("}\n");
                lock (gate)
                {
                    var fi = new FileInfo(Path);
                    if (fi.Exists && fi.Length > 1024 * 1024) File.Move(Path, Path + ".old");   // one rotation is enough
                    File.AppendAllText(Path, sb.ToString());
                }
            }
            catch { /* a failing log must never hurt the game */ }
        }
    }
}
