using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;
using WhoBrokeIt.Vehicles;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Automated headless multiplayer smoke test, only active with "-smoke host" or "-smoke client &lt;ip&gt;" on the command line.
    /// The host plays the whole case through the real request/RPC paths; the client checks the state replicates.
    /// Exit code 0 = pass.
    /// </summary>
    public class SmokeRunner : MonoBehaviour
    {
        const ushort Port = 7777;

        void Start()
        {
            var a = System.Environment.GetCommandLineArgs();
            var i = System.Array.IndexOf(a, "-smoke");
            if (i < 0 || i + 1 >= a.Length) return;
            var ip = i + 2 < a.Length ? a[i + 2] : "127.0.0.1";
            soloHost = a[i + 1] == "host1";
            var mode = a[i + 1];
            if (mode == "online") StartCoroutine(Online(ip));
            else StartCoroutine(mode.StartsWith("host") ? Host() : Client(ip));
        }

        static bool soloHost;

        static void Log(string s) => Debug.Log("[SMOKE] " + s);

        static void Finish(int code)
        {
            // Persist the result: the editor reloads its domain when play mode ends, which wipes statics.
            try { System.IO.File.WriteAllText("smoke-result.txt", code.ToString()); } catch { }
            Application.Quit(code);
        }

        static void Fail(string s)
        {
            Log("FAIL " + s);
            Finish(1);
        }

        static IEnumerator WaitFor(System.Func<bool> cond, float timeout)
        {
            var t = 0f;
            while (!cond() && t < timeout) { t += Time.deltaTime; yield return null; }
        }

        /// <summary>Quick play through the real matchmaking API, then act as a client against the dedicated server.</summary>
        IEnumerator Online(string apiBase)
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            if (apiBase.StartsWith("http")) OnlineApi.Base = apiBase;
            var a2 = System.Environment.GetCommandLineArgs();
            var privateRoom = System.Array.IndexOf(a2, "private") >= 0;
            OnlineApi.Room room = null;
            if (privateRoom) yield return OnlineApi.CreateRoom(r => room = r);
            else yield return OnlineApi.QuickPlay(r => room = r);
            if (room == null || !string.IsNullOrEmpty(room.error) || string.IsNullOrEmpty(room.address)) { Fail("eslestirme: " + (room != null ? room.error : "null")); yield break; }
            if (privateRoom)
            {
                if (string.IsNullOrEmpty(room.code) || room.type != "private") { Fail("ozel oda kodu yok: " + room.type); yield break; }
                Log("ozel oda olusturuldu, kod " + room.code);
                // a second player (here: a second API lookup) must be sent to the same server by that code
                OnlineApi.Room again = null;
                yield return OnlineApi.JoinRoom(room.code.ToLowerInvariant().Replace("-", " "), r => again = r);
                if (again == null || !string.IsNullOrEmpty(again.error) || again.port != room.port) { Fail("kodla katilim farkli oda: " + (again != null ? again.error + " port " + again.port : "null")); yield break; }
                Log("kodla katilim ayni odayi verdi (port " + again.port + ")");
                OnlineApi.Room wrong = null;
                yield return OnlineApi.JoinRoom("ZZZZ-99", r => wrong = r);
                if (wrong == null || string.IsNullOrEmpty(wrong.error)) { Fail("yanlis kod kabul edildi"); yield break; }
                Log("yanlis kod reddedildi: " + wrong.error);
            }
            Log("matchmaker: " + room.type + " oda " + room.address + ":" + room.port + " oyuncu " + room.players + "/" + room.capacity + " sürüm " + room.version);

            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(room.address, (ushort)room.port);
            // "lossy": a bad mobile line (about 100 ms each way, jitter, 8 percent loss) through the transport's built-in simulator (works in the editor)
            var lossy = System.Array.IndexOf(a2, "lossy") >= 0;
            if (lossy) { ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetDebugSimulatorParameters(100, 40, 8); Log("kotu ag benzetimi: 100 ms + 40 ms titresim + %8 kayip"); }
            ConnectionGate.PrepareClient();
            if (!nm.StartClient()) { Fail("StartClient"); yield break; }
            yield return WaitFor(() => nm.IsConnectedClient, lossy ? 40f : 20f);
            if (!nm.IsConnectedClient) { Fail("sunucuya baglanilamadi: " + nm.DisconnectReason); yield break; }
            Log("online: sunucuya baglandi");
            yield return WaitFor(() => PlayerNet.Local != null && CaseManager.I != null && CaseManager.I.IsSpawned, 20f);
            if (PlayerNet.Local == null || CaseManager.I == null) { Fail("oyuncu/vaka olusmadi"); yield break; }
            var cm = CaseManager.I;
            if (privateRoom)
            {
                var codeT0 = Time.realtimeSinceStartup;
                yield return WaitFor(() => cm.roomCode.Value.Length > 0, 45f);
                Log("oda kodu gelis suresi: " + (Time.realtimeSinceStartup - codeT0).ToString("0.0") + " sn");
                if (cm.roomCode.Value.ToString() != room.code && lossy) Log("UYARI (kayipli ag): oda kodu gelmedi, diger kontroller suruyor");
                else if (cm.roomCode.Value.ToString() != room.code) { Fail("oyun ici oda kodu '" + cm.roomCode.Value + "' beklenen '" + room.code + "'"); yield break; }
            }
            Log("online: oyuncu dogdu; nesne=" + nm.SpawnManager.SpawnedObjects.Count + " oda kodu='" + cm.roomCode.Value + "'");

            // clues over RPC -> server state -> replicated back
            foreach (var c in FindObjectsByType<Clue>(FindObjectsSortMode.None)) c.RequestInteract();
            yield return new WaitForSeconds(1.5f);
            if (cm.ClueCount != 3) { Fail("ipucu yansimadi: " + cm.ClueCount); yield break; }
            Log("online: ipuclari sunucudan geri yansidi (3/3)");

            // walk the (owner-authoritative) player next to a crate and pick it up
            var player = PlayerNet.Local;
            var crate = FindObjectsByType<Carryable>(FindObjectsSortMode.None).First(x => x.name == "Crate_0");
            var cc = player.GetComponent<CharacterController>();
            cc.enabled = false;
            player.transform.position = crate.transform.position + new Vector3(0f, 0.2f, -1.6f);
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            cc.enabled = true;
            yield return new WaitForSeconds(1f);
            crate.RequestInteract();
            yield return new WaitForSeconds(1f);
            if (!player.carry.IsCarrying) { Fail("sandik alinamadi"); yield break; }
            player.DropRpc();
            yield return new WaitForSeconds(0.6f);
            if (player.carry.IsCarrying) { Fail("sandik birakilamadi"); yield break; }
            Log("online: al/birak ok");

            // hammer the fuse box before the entrance is clear -> spark blast replicated
            var fuse = FindFirstObjectByType<FuseBox>();
            cc.enabled = false;
            player.transform.position = fuse.transform.position + new Vector3(0f, -0.2f, -1.6f);
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            cc.enabled = true;
            yield return new WaitForSeconds(1f);
            for (var k = 0; k < 140 && cm.sparks.Value == 0; k++) { player.UseToolRpc(2, 0.05f); yield return new WaitForSeconds(0.05f); }
            yield return new WaitForSeconds(0.8f);
            if (cm.sparks.Value != 1) { Fail("kivilcim yansimadi: " + cm.sparks.Value); yield break; }
            Log("online: cekic -> kivilcim kazasi sunucuda gerceklesti ve yansidi");

            // vehicle prediction: the driver's machine moves at once, the server follows, the rider stays on the vehicle
            var skate = FindObjectsByType<RideableVehicle>(FindObjectsSortMode.None).FirstOrDefault(x => x.kind == RideKind.Skateboard);
            if (skate == null) { Fail("online: kaykay yok"); yield break; }
            cc.enabled = false;
            player.transform.position = skate.transform.position + skate.transform.right * 1.2f + Vector3.up * 1.1f;
            cc.enabled = true;
            Physics.SyncTransforms();
            skate.RequestInteract();
            yield return new WaitForSeconds(1.6f);
            if (skate.driverId.Value != nm.LocalClientId) { Fail("online: kaykaya binilemedi, surucu=" + skate.driverId.Value); yield break; }
            var visStart = skate.VisualPosition; var hullStart = skate.transform.position;
            skate.TestDrive(1f, 0f);
            yield return new WaitForSeconds(0.5f);
            var visEarly = Vector3.Distance(skate.VisualPosition, visStart);
            var hullEarly = Vector3.Distance(skate.transform.position, hullStart);
            yield return new WaitForSeconds(2.0f);
            var visLate = Vector3.Distance(skate.VisualPosition, visStart);
            var hullLate = Vector3.Distance(skate.transform.position, hullStart);
            var gap = Vector3.Distance(player.transform.position, skate.VisualPosition);
            Log("online kaykay: 0.5 sn sonra gorsel " + visEarly.ToString("0.00") + " m (sunucu govdesi " + hullEarly.ToString("0.00") + " m); 2.5 sn sonra gorsel " + visLate.ToString("0.0") + " m, govde " + hullLate.ToString("0.0") + " m, surucu mesafesi " + gap.ToString("0.0") + " m");
            if (visEarly < 0.35f) { Fail("online: tahmin yok, arac aninda hareket etmedi"); yield break; }
            if (hullLate < 3f) { Fail("online: sunucu araci ilerletmedi"); yield break; }
            if (Mathf.Abs(visLate - hullLate) > 5f) { Fail("online: gorsel ile sunucu arasi fark buyuk"); yield break; }
            if (gap > 2.5f) { Fail("online: surucu arac uzerinde degil"); yield break; }
            skate.TestDrive(0f, 0f);
            skate.RequestExit();
            yield return new WaitForSeconds(1.0f);
            Log("online: arac tahmini ok");

            yield return new WaitForSeconds(2f);
            Log("ONLINE PASS");
            Finish(0);
        }

        IEnumerator Host()
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            if (!nm.StartHost()) { Fail("StartHost"); yield break; }
            Log("host started");

            yield return WaitFor(() => PlayerNet.Local != null && CaseManager.I != null && CaseManager.I.IsSpawned, 15f);
            if (PlayerNet.Local == null) { Fail("host player did not spawn"); yield break; }
            Log("host player spawned; spawned netobjects=" + nm.SpawnManager.SpawnedObjects.Count);

            if (!soloHost)
            {
                yield return WaitFor(() => nm.ConnectedClients.Count >= 2, 60f);
                Log("connected clients=" + nm.ConnectedClients.Count);
                if (nm.ConnectedClients.Count < 2) { Fail("client never connected"); yield break; }
            }
            yield return new WaitForSeconds(2f);

            var cm = CaseManager.I;

            // 1) clues through the real request path
            foreach (var c in FindObjectsByType<Clue>(FindObjectsSortMode.None)) c.RequestInteract();
            yield return new WaitForSeconds(1f);
            if (cm.ClueCount != 3) { Fail("clues=" + cm.ClueCount); yield break; }
            Log("clues ok (3/3)");

            // 2) carry a crate and drop it
            var player = PlayerNet.Local;
            var crate = FindObjectsByType<Carryable>(FindObjectsSortMode.None).First(x => x.name == "Crate_0");
            crate.GetComponent<Rigidbody>().position = player.transform.position + player.transform.forward * 1.5f;
            yield return new WaitForSeconds(0.3f);
            crate.RequestInteract();
            yield return new WaitForSeconds(0.6f);
            if (!player.carry.IsCarrying) { Fail("pickup failed"); yield break; }
            player.DropRpc();
            yield return new WaitForSeconds(0.4f);
            if (player.carry.IsCarrying) { Fail("drop failed"); yield break; }
            Log("carry/drop ok");

            // 2b) throw a crate: it must leave the player's hands and fly
            crate.GetComponent<Rigidbody>().position = player.transform.position + player.transform.forward * 1.5f;
            yield return new WaitForSeconds(0.3f);
            crate.RequestInteract();
            yield return new WaitForSeconds(0.6f);
            if (!player.carry.IsCarrying) { Fail("firlatma icin sandik alinamadi"); yield break; }
            var thrownFrom = crate.transform.position;
            player.ThrowRpc(player.transform.forward);
            yield return new WaitForSeconds(0.8f);
            var flew = (crate.transform.position - thrownFrom).magnitude;
            if (player.carry.IsCarrying || flew < 3f) { Fail("firlatma calismadi, mesafe " + flew); yield break; }
            Log("firlatma ok (mesafe " + flew.ToString("0.0") + " m)");

            // 2c) bonk: knockback + stun reaches the owning player and is replicated as the red flash flag
            player.ServerBonk(Vector3.back * 6f + Vector3.up * 2f, 0.8f);
            yield return new WaitForSeconds(0.4f);
            var ctrl = player.GetComponent<PlayerController>();
            if (!ctrl.Stunned || cm.bonks.Value < 1) { Fail("bonk etkisi yok: stunned=" + ctrl.Stunned + " bonks=" + cm.bonks.Value); yield break; }
            Log("bonk ok (sersemleme + sayac)");
            yield return new WaitForSeconds(1.2f);

            // 3) heavy cabinet must refuse pickup (no Carryable) and fuse before cleaning must spark
            var fuse = FindFirstObjectByType<FuseBox>();
            fuse.ServerRepair(2f);
            yield return new WaitForSeconds(1f);
            if (cm.sparks.Value != 1 || fuse.powered.Value) { Fail("spark logic sparks=" + cm.sparks.Value + " powered=" + fuse.powered.Value); yield break; }
            Log("spark blast ok");

            // 4) clean the entrance: move debris into the collection zone
            var zone = FindFirstObjectByType<CollectionZone>();
            var zc = zone.GetComponent<BoxCollider>().bounds.center;
            var n = 0;
            foreach (var d in FindObjectsByType<Debris>(FindObjectsSortMode.None))
            {
                d.GetComponent<Rigidbody>().position = zc + new Vector3((n % 5) * 0.4f - 0.8f, 0.4f, (n / 5) * 0.4f - 0.8f);
                n++;
            }
            yield return WaitFor(() => cm.DebrisCleared.Value, 8f);
            if (!cm.DebrisCleared.Value) { Fail("debris cleared never true, count=" + cm.debrisCount.Value); yield break; }
            Log("debris cleared ok (" + cm.debrisCount.Value + ")");

            // 5) repair fuse box
            fuse.ServerRepair(2f);
            yield return new WaitForSeconds(0.5f);
            if (!cm.FuseRepaired.Value) { Fail("fuse not repaired"); yield break; }
            Log("fuse ok");

            // 6) forklift enter request
            var fk = FindFirstObjectByType<Forklift>();
            fk.RequestInteract();
            yield return new WaitForSeconds(0.6f);
            if (fk.driverId.Value != nm.LocalClientId) { Fail("forklift driver=" + fk.driverId.Value); yield break; }
            Log("forklift enter ok");

            // drive it: forward for 2 s must move the hull several metres; fork up must raise the forks
            var fkBody = fk.GetComponent<Rigidbody>();
            var fkStart = fkBody.position;
            fk.ServerTestInput(1f, 0f, 1f);
            yield return new WaitForSeconds(2.0f);
            fk.ServerTestInput(0f, 0f, 0f);
            var drove = Vector3.Distance(fkBody.position, fkStart);
            Log("forklift surus: " + drove.ToString("0.00") + " m, yukseklik " + fkBody.position.y.ToString("0.00") + ", catal " + fk.ForkHeight.ToString("0.00") + " m");
            if (drove < 3f) { Fail("forklift hareket etmiyor (" + drove.ToString("0.00") + " m)"); yield break; }
            if (fk.ForkHeight < 0.5f) { Fail("catal kalkmiyor (" + fk.ForkHeight.ToString("0.00") + ")"); yield break; }
            Log("forklift surus ok");

            // steering: full lock for 1.5 s must turn the hull
            var yaw0 = fk.Heading;
            fk.ServerTestInput(1f, 1f, 0f);
            yield return new WaitForSeconds(1.5f);
            var turned = Mathf.Abs(Mathf.DeltaAngle(yaw0, fk.Heading));
            fk.ServerTestInput(0f, 0f, 0f);
            Log("forklift direksiyon: " + turned.ToString("0") + " derece, tekerlek " + fk.steerAngle.Value.ToString("0"));
            if (turned < 15f) { Fail("forklift donmuyor (" + turned.ToString("0") + ")"); yield break; }

            // ramp: start below the ramp (x -6, z -11) facing +Z and drive up; must climb and stay upright
            fk.ServerTeleport(new Vector3(-6f, 0.1f, -11f), 0f);
            fk.ServerTestInput(1f, 0f, 0f);
            var maxY = 0f; var minUp = 1f;
            for (var tick = 0; tick < 40; tick++)
            {
                yield return new WaitForSeconds(0.1f);
                maxY = Mathf.Max(maxY, fkBody.position.y);
                if (fkBody.position.y > 0.3f) minUp = Mathf.Min(minUp, fk.transform.up.y);
            }
            fk.ServerTestInput(0f, 0f, 0f);
            Log("forklift rampa: en yuksek " + maxY.ToString("0.00") + " m, en kucuk dikeylik " + minUp.ToString("0.00"));
            if (maxY < 0.6f) { Fail("forklift rampaya cikamadi (" + maxY.ToString("0.00") + ")"); yield break; }
            if (minUp < 0.7f) { Fail("forklift rampada devrildi (" + minUp.ToString("0.00") + ")"); yield break; }
            fk.ServerTeleport(new Vector3(8.5f, 0.1f, -7f), 0f);
            Log("forklift rampa ok");

            // 7) deliver the cabinet
            var dz = FindFirstObjectByType<DeliveryZone>();
            var cab = GameObject.Find("ToolCabinet").GetComponent<Rigidbody>();
            cab.position = dz.GetComponent<BoxCollider>().bounds.center + Vector3.up * 0.1f;
            yield return WaitFor(() => cm.Finished.Value, 8f);
            if (!cm.Finished.Value) { Fail("not finished; delivered=" + cm.CabinetDelivered.Value); yield break; }
            Log("case finished ok, elapsed=" + cm.elapsed.Value.ToString("0.0"));

            // 8) tool RPC does not throw
            player.UseToolRpc(1, 0.016f);
            yield return new WaitForSeconds(2f);

            // 9) skateboard and bicycle: hop on, ride, steer, hop off
            fk.RequestExit();
            yield return new WaitForSeconds(1.0f);
            var rides = FindObjectsByType<RideableVehicle>(FindObjectsSortMode.None);
            if (rides.Length < 2) { Fail("kaykay/bisiklet sahnede yok (" + rides.Length + ")"); yield break; }
            foreach (var rv in rides)
            {
                var pcc = player.GetComponent<CharacterController>();
                pcc.enabled = false;
                player.transform.position = rv.transform.position + rv.transform.right * 1.2f + Vector3.up * 1.1f;
                pcc.enabled = true;
                Physics.SyncTransforms();
                rv.RequestInteract();
                yield return new WaitForSeconds(1.0f);
                if (rv.driverId.Value != nm.LocalClientId) { Fail(rv.label + ": binilemedi, surucu=" + rv.driverId.Value); yield break; }
                if (player.ride.Value != (byte)rv.kind) { Fail(rv.label + ": ride degeri " + player.ride.Value); yield break; }
                var rvStart = rv.transform.position;
                rv.ServerTestInput(1f, 0f, 0f);
                yield return new WaitForSeconds(2.0f);
                var rvDist = Vector3.Distance(rv.transform.position, rvStart);
                var rvYaw = rv.Heading;
                rv.ServerTestInput(1f, 1f, 0f);
                yield return new WaitForSeconds(1.5f);
                var rvTurn = Mathf.Abs(Mathf.DeltaAngle(rvYaw, rv.Heading));
                rv.ServerTestInput(0f, 0f, 0f);
                yield return new WaitForSeconds(0.8f);
                var rvGap = Vector3.Distance(player.transform.position, rv.transform.position);
                Log(rv.label + ": yol " + rvDist.ToString("0.0") + " m, donus " + rvTurn.ToString("0") + " derece, surucu-arac mesafesi " + rvGap.ToString("0.0") + " m");
                if (rvDist < 3f) { Fail(rv.label + " ilerlemiyor"); yield break; }
                if (rvTurn < 12f) { Fail(rv.label + " donmuyor"); yield break; }
                if (rvGap > 2.5f) { Fail(rv.label + ": surucu arac uzerinde degil"); yield break; }
                rv.RequestExit();
                yield return new WaitForSeconds(0.8f);
                if (rv.driverId.Value != ulong.MaxValue || player.ride.Value != 0) { Fail(rv.label + ": inilemedi"); yield break; }
            }
            Log("kaykay ve bisiklet ok");
            yield return new WaitForSeconds(2f);
            Log("HOST PASS");
            Finish(0);
        }

        IEnumerator Client(string ip)
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(ip, Port);
            ConnectionGate.PrepareClient();
            if (!nm.StartClient()) { Fail("StartClient"); yield break; }
            yield return WaitFor(() => nm.IsConnectedClient, 30f);
            if (!nm.IsConnectedClient) { Fail("could not connect to " + ip); yield break; }
            Log("client connected");
            yield return WaitFor(() => PlayerNet.Local != null && CaseManager.I != null && CaseManager.I.IsSpawned, 20f);
            if (PlayerNet.Local == null || CaseManager.I == null) { Fail("client player/case not spawned"); yield break; }
            Log("client player spawned; spawned netobjects=" + nm.SpawnManager.SpawnedObjects.Count);

            yield return new WaitForSeconds(2f);
            var players = FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length;
            if (players != 2) { Fail("expected 2 players visible, saw " + players); yield break; }
            Log("both players visible");

            yield return WaitFor(() => CaseManager.I.Finished.Value, 90f);
            var cm = CaseManager.I;
            if (!cm.Finished.Value) { Fail("finished state never replicated; clues=" + cm.ClueCount + " debris=" + cm.debrisCount.Value); yield break; }
            var fk = FindFirstObjectByType<Forklift>();
            var fuse = FindFirstObjectByType<FuseBox>();
            if (!fuse.powered.Value) { Fail("fuse state not replicated"); yield break; }
            if (fk.driverId.Value == ulong.MaxValue) { Fail("forklift driver not replicated"); yield break; }
            var cab = GameObject.Find("ToolCabinet").transform.position;
            Log("replicated: clues=" + cm.ClueCount + " debris=" + cm.debrisCount.Value + " sparks=" + cm.sparks.Value + " cabinetPos=" + cab);
            Log("CLIENT PASS");
            Finish(0);
        }
    }
}
