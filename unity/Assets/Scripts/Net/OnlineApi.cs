using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace WhoBrokeIt.Net
{
    /// <summary>Client for the matchmaking service (room codes and quick play). Servers are dedicated; players never host for strangers.</summary>
    public static class OnlineApi
    {
        public static string Base = "https://whobrokeit.cicibyte.com/api";

        [Serializable]
        public class Room
        {
            public string code;
            public string type;      // "public" | "private"
            public string address;
            public int port;
            public int players;
            public int capacity;
            public string version;
            public string error;
        }

        static IEnumerator Send(UnityWebRequest req, Action<Room> done)
        {
            req.timeout = 12;
            yield return req.SendWebRequest();
            Room room = null;
            var body = req.downloadHandler != null ? req.downloadHandler.text : "";
            if (!string.IsNullOrEmpty(body))
            {
                try { room = JsonUtility.FromJson<Room>(body); } catch { }
            }
            if (room == null) room = new Room();
            if (req.result != UnityWebRequest.Result.Success && string.IsNullOrEmpty(room.error))
                room.error = req.result == UnityWebRequest.Result.ConnectionError ? "Sunucuya ulaşılamadı. İnternet bağlantını kontrol et." : "Beklenmeyen hata (" + req.responseCode + ").";
            done(room);
            req.Dispose();
        }

        public static IEnumerator QuickPlay(Action<Room> done)
        {
            var req = new UnityWebRequest(Base + "/quickplay", "POST") { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")) };
            req.SetRequestHeader("Content-Type", "application/json");
            yield return Send(req, done);
        }

        public static IEnumerator CreateRoom(Action<Room> done)
        {
            var req = new UnityWebRequest(Base + "/rooms", "POST") { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")) };
            req.SetRequestHeader("Content-Type", "application/json");
            yield return Send(req, done);
        }

        public static IEnumerator JoinRoom(string code, Action<Room> done)
        {
            var clean = UnityWebRequest.EscapeURL((code ?? "").Trim().ToUpperInvariant());
            var req = new UnityWebRequest(Base + "/rooms/" + clean, "GET") { downloadHandler = new DownloadHandlerBuffer() };
            yield return Send(req, done);
        }
    }
}
