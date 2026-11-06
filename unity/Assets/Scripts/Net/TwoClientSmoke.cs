using System.Collections;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;
using WhoBrokeIt.Vehicles;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Two real clients against one dedicated server (see tools/mp). Started by "-smoke twoA|twoB &lt;ip&gt; &lt;port&gt;".
    /// A joins, enters the forklift, drives it, lifts the forks, gets off, picks up a crate, carries and drops it.
    /// B only watches and judges what it sees. Both write one "[MP]" line every 0.25 s (wall-clock ms) so tools/mp/analyze.py can compare their views.
    /// </summary>
    public class TwoClientSmoke : MonoBehaviour
    {
        const int ExpectedWorldObjects = 45;     // in-scene network objects (without player objects)
        string who;
        string ip;
        ushort port;
        int exceptions;
        readonly StringBuilder excText = new StringBuilder();
        float connectStart;
        float worldReadyAt = -1f;

        static void L(string s) => Debug.Log("[MP] " + s);
        static long Now => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        void Start()
        {
            var a = System.Environment.GetCommandLineArgs();
            var i = System.Array.IndexOf(a, "-smoke");
            if (i < 0 || i + 1 >= a.Length) { enabled = false; return; }
            var mode = a[i + 1];
            if (mode != "twoA" && mode != "twoB") { enabled = false; return; }
            who = mode == "twoA" ? "A" : "B";
            ip = i + 2 < a.Length ? a[i + 2] : "127.0.0.1";
            port = i + 3 < a.Length && ushort.TryParse(a[i + 3], out var p) ? p : (ushort)7790;
            Application.logMessageReceived += OnLog;
            StartCoroutine(who == "A" ? RunA() : RunB());
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

        bool Fail(string why)
        {
            L(who + " FAIL " + why);
            Quit(1);
            return false;
        }

        // ---------- common ----------
        IEnumerator Connect()
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(ip, port);
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "lossy") >= 0)
            {
                ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetDebugSimulatorParameters(100, 40, 8);
                L(who + " yerlesik benzetici: 100 ms + 40 ms titresim + %8 kayip (yalnizca Editor/gelistirme build)");
            }
            ConnectionGate.PrepareClient();
            connectStart = Time.realtimeSinceStartup;
            if (!nm.StartClient()) { Fail("StartClient"); yield break; }
            L(who + " verbindung gestartet " + ip + ":" + port);
        }

        static int WorldObjects()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || nm.SpawnManager == null) return 0;
            return nm.SpawnManager.SpawnedObjectsList.Count(o => o.GetComponent<PlayerNet>() == null);
        }

        IEnumerator WaitWorld(float timeout)
        {
            var nm = NetworkManager.Singleton;
            var t = 0f;
            while (t < timeout)
            {
                if (nm.IsConnectedClient && PlayerNet.Local != null && WorldObjects() >= ExpectedWorldObjects) { worldReadyAt = Time.realtimeSinceStartup - connectStart; yield break; }
                t += Time.deltaTime;
                yield return null;
            }
        }

        string Sample()
        {
            var nm = NetworkManager.Singleton;
            var fk = FindFirstObjectByType<Forklift>();
            var crate = FindObjectsByType<Carryable>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "Crate_0");
            var players = FindObjectsByType<PlayerNet>(FindObjectsSortMode.None);
            var other = players.FirstOrDefault(p => !p.IsOwner);
            var sb = new StringBuilder();
            sb.Append(who).Append(" t=").Append(Now);
            sb.Append(" objs=").Append(nm.SpawnManager.SpawnedObjectsList.Count);
            sb.Append(" world=").Append(WorldObjects());
            sb.Append(" players=").Append(players.Length);
            sb.Append(" forklifts=").Append(FindObjectsByType<Forklift>(FindObjectsSortMode.None).Length);
            if (fk != null)
            {
                var h = fk.transform.position; var v = fk.visual != null ? fk.visual.position : h;
                sb.Append(" fk=").Append(h.x.ToString("0.00")).Append(',').Append(h.z.ToString("0.00"));
                sb.Append(" fkv=").Append(v.x.ToString("0.00")).Append(',').Append(v.z.ToString("0.00"));
                sb.Append(" driver=").Append(fk.driverId.Value == ulong.MaxValue ? -1 : (long)fk.driverId.Value);
                sb.Append(" fkowner=").Append(fk.OwnerClientId);
            }
            if (crate != null)
            {
                var c = crate.transform.position;
                sb.Append(" crate=").Append(c.x.ToString("0.00")).Append(',').Append(c.y.ToString("0.00")).Append(',').Append(c.z.ToString("0.00"));
            }
            var ap = players.FirstOrDefault(p => who == "B" ? !p.IsOwner : p.IsOwner);   // A's own player (seen by both)
            if (ap != null)
            {
                var feet = ap.visualAnchor != null ? ap.visualAnchor.position : ap.transform.position;
                sb.Append(" a=").Append(feet.x.ToString("0.00")).Append(',').Append(feet.y.ToString("0.00")).Append(',').Append(feet.z.ToString("0.00"));
                var rp = ap.transform.position;
                sb.Append(" aroot=").Append(rp.x.ToString("0.0")).Append(',').Append(rp.y.ToString("0.0")).Append(',').Append(rp.z.ToString("0.0"));
                var al = ap.visualAnchor != null ? ap.visualAnchor.localPosition : Vector3.zero;
                sb.Append(" aanch=").Append(al.x.ToString("0.0")).Append(',').Append(al.y.ToString("0.0")).Append(',').Append(al.z.ToString("0.0"));
                sb.Append(" hit=").Append(Time.time - PlayerController.DebugLastHitTime < 0.3f ? PlayerController.DebugLastHit : "-");
                sb.Append(" aheld=").Append(ap.carry != null && ap.carry.IsCarrying ? (long)ap.carry.heldId.Value : -1);
                sb.Append(" aseated=").Append(ap.seated.Value ? 1 : 0);
            }
            sb.Append(" exc=").Append(exceptions);
            return sb.ToString();
        }

        IEnumerator Sampler(float seconds)
        {
            var end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                L(Sample());
                yield return new WaitForSeconds(0.25f);
            }
        }

        // ---------- client A: the actor ----------
        IEnumerator RunA()
        {
            yield return Connect();
            var nm = NetworkManager.Singleton;
            yield return WaitWorld(30f);
            L("A world ready after " + worldReadyAt.ToString("0.0") + " s, objects " + WorldObjects());
            if (worldReadyAt < 0f) { Fail("dunya yuklenmedi, nesne=" + WorldObjects()); yield break; }
            StartCoroutine(Sampler(110f));

            var player = PlayerNet.Local;
            var t = 0f;
            while (FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length < 2 && t < 30f) { t += Time.deltaTime; yield return null; }
            L("A sees " + FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length + " players");
            { var oth = FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).FirstOrDefault(p => !p.IsOwner); var tw = 0f; while (oth != null && oth.playerName.Value.Length == 0 && tw < 5f) { tw += Time.deltaTime; yield return null; } L("NAME other=\"" + (oth != null ? oth.playerName.Value.ToString() : "?") + "\""); }
            if (PlayerNet.Local != null) { PlayerNet.Local.SendChat("merhaba dunya"); PlayerNet.Local.SendChat("hizli ikinci mesaj"); PlayerNet.Local.SetEmote(3); }
            yield return new WaitForSeconds(1.5f);
            yield return new WaitForSeconds(3f);              // let B settle before the show starts

            var cc = player.GetComponent<CharacterController>();
            var fk = FindFirstObjectByType<Forklift>();
            yield return WalkTo(player, cc, fk.transform.position + fk.transform.right * -2.0f);
            fk.RequestInteract();
            t = 0f;
            while ((fk.driverId.Value != nm.LocalClientId || !fk.IsDriverLocal) && t < 12f) { t += Time.deltaTime; yield return null; }
            if (!fk.IsDriverLocal) { Fail("forklife binilemedi, surucu=" + fk.driverId.Value); yield break; }
            L("A seated after " + t.ToString("0.0") + " s");

            fk.TestDrive(1f, 0f, 0f); yield return new WaitForSeconds(3f);
            fk.TestDrive(1f, 1f, 0f); yield return new WaitForSeconds(1.5f);
            fk.TestDrive(0.6f, -1f, 0f); yield return new WaitForSeconds(1.2f);
            fk.TestDrive(0f, 0f, 1f); yield return new WaitForSeconds(1.2f);
            fk.TestDrive(0f, 0f, 0f); yield return new WaitForSeconds(1.5f);
            fk.RequestExit();
            t = 0f;
            while (fk.driverId.Value != ulong.MaxValue && t < 10f) { t += Time.deltaTime; yield return null; }
            if (fk.driverId.Value != ulong.MaxValue) { Fail("forklift'ten inilemedi"); yield break; }
            L("A got off after " + t.ToString("0.0") + " s");
            yield return new WaitForSeconds(1.0f);

            var crate = FindObjectsByType<Carryable>(FindObjectsSortMode.None).First(x => x.name == "Crate_0");
            var cp = crate.transform.position;
            // the scene places Crate_0 in open ground near where the forklift ends up: walk straight to it from the east
            yield return WalkTo(player, cc, new Vector3(cp.x + 1.6f, 0f, cp.z));
            player.transform.rotation = Quaternion.LookRotation(Vector3.left);
            yield return new WaitForSeconds(1f);
            crate.RequestInteract();
            t = 0f;
            while (!player.carry.IsCarrying && t < 8f) { t += Time.deltaTime; yield return null; }
            if (!player.carry.IsCarrying) { Fail("sandik alinamadi"); yield break; }
            L("A carrying after " + t.ToString("0.0") + " s");
            yield return new WaitForSeconds(1.0f);
            yield return WalkTo(player, cc, player.transform.position + new Vector3(5f, 0f, 0f));
            yield return new WaitForSeconds(2.0f);
            L("A dropping at " + player.transform.position.ToString("0.0") + " cc.vel=" + cc.velocity.ToString("0.0"));
            player.DropRpc();
            t = 0f;
            while (player.carry.IsCarrying && t < 8f) { t += Time.deltaTime; yield return null; }
            if (player.carry.IsCarrying) { Fail("sandik birakilamadi"); yield break; }
            yield return new WaitForSeconds(3f);
            foreach (var rv in FindObjectsByType<RideableVehicle>(FindObjectsSortMode.None).OrderBy(r => r.kind))
                yield return RideTest(player, cc, rv);
            L("A DONE exc=" + exceptions + (exceptions > 0 ? " " + excText : ""));
            Quit(exceptions == 0 ? 0 : 1);
        }

        /// <summary>Rides a skateboard/bicycle with scripted input and logs how it feels: delay until the vehicle moves, how far the shown (predicted) pose is from the replicated one, and the largest single-frame jump.</summary>
        IEnumerator RideTest(PlayerNet player, CharacterController cc, RideableVehicle rv)
        {
            var nm = NetworkManager.Singleton;
            Place(cc, player, rv.transform.position + rv.transform.right * -1.5f + Vector3.up * 0.3f);
            yield return new WaitForSeconds(1f);
            rv.RequestInteract();
            var t = 0f;
            while (rv.driverId.Value != nm.LocalClientId && t < 8f) { t += Time.deltaTime; yield return null; }
            if (rv.driverId.Value != nm.LocalClientId) { L("RIDE " + rv.kind + " FAIL binilemedi"); yield break; }
            yield return new WaitForSeconds(1.3f);            // hop (0.5 s) + prediction start
            var start = rv.VisualPosition;
            float react = -1f, maxErr = 0f, maxJump = 0f, maxSpeed = 0f, sumErr = 0f; int frames = 0;
            var last = start;
            var phases = new[] { new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, -1f), new Vector2(0f, 0f) };
            var clock = 0f;
            foreach (var ph in phases)
            {
                rv.TestDrive(ph.x, ph.y);
                var pt = 0f;
                while (pt < 2.5f)
                {
                    var dt = Time.deltaTime; pt += dt; clock += dt;
                    var v = rv.VisualPosition;
                    if (react < 0f && Vector3.Distance(v, start) > 0.15f) react = clock;
                    var err = Vector2.Distance(new Vector2(v.x, v.z), new Vector2(rv.transform.position.x, rv.transform.position.z));
                    maxErr = Mathf.Max(maxErr, err); sumErr += err; frames++;
                    maxJump = Mathf.Max(maxJump, Vector3.Distance(v, last)); last = v;
                    maxSpeed = Mathf.Max(maxSpeed, rv.Speed);
                    yield return null;
                }
            }
            L("RIDE " + rv.kind + " react=" + react.ToString("0.00") + "s maxPredVsServer=" + maxErr.ToString("0.00") + "m avgErr=" + (sumErr / Mathf.Max(1, frames)).ToString("0.00") + "m maxFrameJump=" + maxJump.ToString("0.00") + "m srvSpeed=" + maxSpeed.ToString("0.0") + " frames=" + frames);
            rv.TestDrive(0f, 0f);
            rv.RequestExit();
            t = 0f;
            while (rv.driverId.Value != ulong.MaxValue && t < 8f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(1f);
        }

        /// <summary>Walks the real character controller (like a player holding W) to a point on the ground; gives up after the timeout.</summary>
        IEnumerator WalkTo(PlayerNet p, CharacterController cc, Vector3 target, float timeout = 25f)
        {
            var t = 0f;
            while (t < timeout)
            {
                var to = target - p.transform.position; to.y = 0f;
                if (to.magnitude < 0.4f) { L(who + " WalkTo tamam " + target + " konum " + p.transform.position.ToString("0.0") + " t=" + t.ToString("0.0")); yield break; }
                var dir = to.normalized;
                p.transform.rotation = Quaternion.LookRotation(dir);
                cc.Move((dir * 5f + Vector3.down * 2f) * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }
            L(who + " WalkTo zaman asimi " + target);
        }

        static void Place(CharacterController cc, PlayerNet p, Vector3 pos)
        {
            cc.enabled = false;
            p.transform.position = pos;
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        // ---------- client B: the observer ----------
        IEnumerator RunB()
        {
            yield return Connect();
            yield return WaitWorld(30f);
            L("B world ready after " + worldReadyAt.ToString("0.0") + " s, objects " + WorldObjects());
            if (worldReadyAt < 0f) { Fail("dunya yuklenmedi, nesne=" + WorldObjects()); yield break; }

            var fk = FindFirstObjectByType<Forklift>();
            var fkStart = fk.transform.position;
            float maxDisp = 0f, maxForkGap = 0f; int drivenSamples = 0, goodRider = 0;
            bool sawDriver = false, sawCarry = false, sawDrop = false, afterCarry = false, ghost = false;
            Vector3 crateAtDrop = Vector3.zero;
            var end = Time.realtimeSinceStartup + 100f;
            var nm = NetworkManager.Singleton;
            var crate = FindObjectsByType<Carryable>(FindObjectsSortMode.None).First(x => x.name == "Crate_0");
            while (Time.realtimeSinceStartup < end)
            {
                L(Sample());
                if (FindObjectsByType<Forklift>(FindObjectsSortMode.None).Length != 1) ghost = true;
                var a = FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).FirstOrDefault(p => !p.IsOwner);
                if (a != null)
                {
                    if (fk.driverId.Value == a.OwnerClientId)
                    {
                        sawDriver = true;
                        maxDisp = Mathf.Max(maxDisp, Vector3.Distance(fk.transform.position, fkStart));
                        if (a.seated.Value)
                        {
                            drivenSamples++;
                            var feet = a.visualAnchor != null ? a.visualAnchor.position : a.transform.position;
                            if (Vector3.Distance(feet, fk.visual != null ? fk.visual.position : fk.transform.position) < 3.0f) goodRider++;
                        }
                    }
                    if (a.carry != null && a.carry.IsCarrying)
                    {
                        sawCarry = true;
                        if (Vector3.Distance(crate.transform.position, a.transform.position) < 3.5f) afterCarry = true;
                    }
                    else if (sawCarry && !sawDrop && afterCarry) { sawDrop = true; crateAtDrop = crate.transform.position; }
                }
                yield return new WaitForSeconds(0.25f);
            }
            var riderOk = drivenSamples > 0 && goodRider >= drivenSamples * 0.8f;
            var crateSettled = sawDrop && Vector3.Distance(crate.transform.position, crateAtDrop) < 1.5f && crate.transform.position.y < 1.5f;
            L("B RESULT world=" + WorldObjects() + " readyAfter=" + worldReadyAt.ToString("0.0") + "s driverSeen=" + sawDriver + " maxDisp=" + maxDisp.ToString("0.0")
              + " riderSamples=" + goodRider + "/" + drivenSamples + " carrySeen=" + sawCarry + " dropSeen=" + sawDrop + " crateSettled=" + crateSettled
              + " finalDriver=" + (fk.driverId.Value == ulong.MaxValue ? "none" : fk.driverId.Value.ToString()) + " ghost=" + ghost + " exc=" + exceptions + (exceptions > 0 ? " " + excText : ""));
            var ok = sawDriver && maxDisp >= 3f && riderOk && sawCarry && sawDrop && crateSettled && fk.driverId.Value == ulong.MaxValue && !ghost && exceptions == 0 && WorldObjects() >= ExpectedWorldObjects;
            L("B " + (ok ? "PASS" : "FAIL"));
            Quit(ok ? 0 : 1);
        }
    }
}
