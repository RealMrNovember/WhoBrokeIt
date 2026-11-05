using System.Collections;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Destruction scenario against one dedicated server (tools/mp/destruct.sh). "-smoke destA|destB|destC &lt;ip&gt; &lt;port&gt;".
    /// A throws Crate_1 until it breaks, waits for the late joiner C, then repairs it with the hammer.
    /// B watches from the start, C joins AFTER the break (late joiner must see the broken state without replayed debris).
    /// Every client writes "[DS]" lines (state, debris counters); tools/mp/analyze-destruct.py compares them with the server's "[DESTR]" lines.
    /// </summary>
    public class DestructSmoke : MonoBehaviour
    {
        const int ExpectedWorldObjects = 45;
        string who;
        string ip;
        ushort port;
        int exceptions;
        readonly StringBuilder excText = new StringBuilder();
        float connectStart, worldReadyAt = -1f;

        static void L(string s) => Debug.Log("[DS] " + s);

        void Start()
        {
            var a = System.Environment.GetCommandLineArgs();
            var i = System.Array.IndexOf(a, "-smoke");
            if (i < 0 || i + 1 >= a.Length) { enabled = false; return; }
            var mode = a[i + 1];
            if (mode != "destA" && mode != "destB" && mode != "destC") { enabled = false; return; }
            who = mode.Substring(4);
            ip = i + 2 < a.Length ? a[i + 2] : "127.0.0.1";
            port = i + 3 < a.Length && ushort.TryParse(a[i + 3], out var p) ? p : (ushort)7790;
            Application.logMessageReceived += OnLog;
            StartCoroutine(Run());
        }

        void OnDestroy() { Application.logMessageReceived -= OnLog; }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type != LogType.Exception) return;
            exceptions++;
            if (excText.Length < 600) excText.Append(msg.Split('\n')[0]).Append(" | ");
        }

        static void Quit(int code)
        {
            try { System.IO.File.WriteAllText("/results/exit-" + System.Environment.MachineName + ".txt", code.ToString()); } catch { }
            Application.Quit(code);
        }

        void Fail(string why) { L(who + " FAIL " + why); Quit(1); }

        static int WorldObjects()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.SpawnManager == null) return 0;
            return nm.SpawnManager.SpawnedObjectsList.Count(o => o.GetComponent<PlayerNet>() == null);
        }

        static Destructible Crate() => FindObjectsByType<Destructible>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Crate_1");

        string Line()
        {
            var c = Crate();
            var sb = new StringBuilder();
            sb.Append(who).Append(" t=").Append(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append(" world=").Append(WorldObjects());
            sb.Append(" players=").Append(FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length);
            sb.Append(" st=").Append(c != null ? c.state.Value.ToString() : "-");
            sb.Append(" chunks=").Append(DebrisPool.TotalSpawned).Append(" puffs=").Append(DebrisPool.TotalPuffs);
            sb.Append(" fxCrack=").Append(Destructible.FxCrack).Append(" fxBreak=").Append(Destructible.FxBreak).Append(" fxRepair=").Append(Destructible.FxRepair);
            sb.Append(" rend=").Append(c != null && c.GetComponentsInChildren<Renderer>().Any(r => r.enabled && !(r is ParticleSystemRenderer)) ? 1 : 0);
            sb.Append(" col=").Append(c != null && c.GetComponentsInChildren<Collider>().Any(k => k.enabled && !k.isTrigger) ? 1 : 0);
            var panel = FindFirstObjectByType<ElectricalPanel>();
            sb.Append(" panel=").Append(panel != null ? (panel.Powered ? 1 : 0) : -1);
            sb.Append(" exc=").Append(exceptions);
            return sb.ToString();
        }

        IEnumerator Sampler(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) { L(Line()); yield return new WaitForSeconds(0.25f); }
        }

        IEnumerator Run()
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(ip, port);
            ConnectionGate.PrepareClient();
            connectStart = Time.realtimeSinceStartup;
            if (!nm.StartClient()) { Fail("StartClient"); yield break; }
            var t = 0f;
            while (t < 30f)
            {
                if (nm.IsConnectedClient && PlayerNet.Local != null && WorldObjects() >= ExpectedWorldObjects && Crate() != null) { worldReadyAt = Time.realtimeSinceStartup - connectStart; break; }
                t += Time.deltaTime; yield return null;
            }
            if (worldReadyAt < 0f) { Fail("dunya yuklenmedi, nesne=" + WorldObjects() + " crate=" + (Crate() != null)); yield break; }
            L(who + " READY after " + worldReadyAt.ToString("0.0") + "s " + Line());
            StartCoroutine(Sampler(who == "A" ? 150f : (who == "B" ? 120f : 60f)));
            if (who == "A") yield return RunA();
            else if (who == "B") yield return RunWatcher(110f);
            else yield return RunWatcher(45f, expectBrokenAtJoin: true);
        }

        IEnumerator RunWatcher(float seconds, bool expectBrokenAtJoin = false)
        {
            if (expectBrokenAtJoin)
            {
                var c = Crate();
                L(who + " JOIN state=" + c.state.Value + " chunks=" + DebrisPool.TotalSpawned + " rendererOn=" + (c.GetComponentsInChildren<Renderer>().Any(r => r.enabled) ? 1 : 0) + " colliderOn=" + (c.GetComponentsInChildren<Collider>().Any(k => k.enabled && !k.isTrigger) ? 1 : 0));
            }
            yield return new WaitForSeconds(seconds);
            L(who + " DONE exc=" + exceptions + (exceptions > 0 ? " " + excText : ""));
            Quit(exceptions == 0 ? 0 : 1);
        }

        static void Place(CharacterController cc, PlayerNet p, Vector3 pos, Vector3 look)
        {
            cc.enabled = false;
            p.transform.position = pos;
            p.transform.rotation = Quaternion.LookRotation(look);
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        IEnumerator RunA()
        {
            var player = PlayerNet.Local;
            var cc = player.GetComponent<CharacterController>();
            var crate = Crate();
            var carry = crate.GetComponent<Carryable>();
            var t = 0f;
            while (FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length < 2 && t < 30f) { t += Time.deltaTime; yield return null; }   // B must be there first
            yield return new WaitForSeconds(2f);

            // throw it until it is broken (the first hit usually only cracks it)
            for (var attempt = 1; attempt <= 4 && crate.state.Value != Destructible.Broken; attempt++)
            {
                var cp = crate.transform.position;
                Place(cc, player, new Vector3(cp.x, cp.y + 0.7f, cp.z - 1.6f), Vector3.forward);   // south of the crate, looking north (clear yard)
                yield return new WaitForSeconds(0.8f);
                carry.RequestInteract();
                t = 0f;
                while (!player.carry.IsCarrying && t < 8f) { t += Time.deltaTime; yield return null; }
                if (!player.carry.IsCarrying) { Fail("sandik alinamadi (deneme " + attempt + ") state=" + crate.state.Value); yield break; }
                yield return new WaitForSeconds(0.8f);
                L("A THROW attempt=" + attempt + " state=" + crate.state.Value);
                player.ThrowRpc(new Vector3(0f, 0.12f, 1f));
                t = 0f;
                var before = crate.state.Value;
                while (crate.state.Value == before && t < 6f) { t += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(1.5f);
                L("A AFTER THROW attempt=" + attempt + " state=" + crate.state.Value + " after " + t.ToString("0.0") + "s chunks=" + DebrisPool.TotalSpawned);
                if (crate.state.Value != Destructible.Broken && player.carry.IsCarrying) { player.DropRpc(); yield return new WaitForSeconds(1f); }
                if (crate.state.Value == Destructible.Intact) { /* did not even crack: throw again */ }
            }
            if (crate.state.Value != Destructible.Broken) { Fail("sandik kirilmadi, state=" + crate.state.Value); yield break; }
            L("A BROKEN state=2 chunks=" + DebrisPool.TotalSpawned + " fxBreak=" + Destructible.FxBreak);

            // wait for the late joiner (C), then repair with the hammer
            t = 0f;
            while (FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length < 3 && t < 45f) { t += Time.deltaTime; yield return null; }
            L("A SEES players=" + FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length + " (waited " + t.ToString("0.0") + "s)");
            yield return new WaitForSeconds(4f);

            var p2 = crate.transform.position;
            Place(cc, player, p2 + new Vector3(-1.4f, 0.7f, 0f), Vector3.right);
            yield return new WaitForSeconds(0.6f);
            player.toolId.Value = 2;                       // hammer
            yield return new WaitForSeconds(0.5f);
            t = 0f;
            while (crate.state.Value != Destructible.Intact && t < 20f)
            {
                player.UseToolRpc(2, Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }
            if (crate.state.Value != Destructible.Intact) { Fail("onarilamadi, state=" + crate.state.Value + " t=" + t.ToString("0.0")); yield break; }
            L("A REPAIRED after " + t.ToString("0.0") + "s fxRepair=" + Destructible.FxRepair);
            yield return new WaitForSeconds(4f);
            L("A DONE exc=" + exceptions + (exceptions > 0 ? " " + excText : ""));
            Quit(exceptions == 0 ? 0 : 1);
        }
    }
}
