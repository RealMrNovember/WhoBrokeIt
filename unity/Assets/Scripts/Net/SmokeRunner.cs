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
            OnlineApi.Room room = null;
            yield return OnlineApi.QuickPlay(r => room = r);
            if (room == null || !string.IsNullOrEmpty(room.error) || string.IsNullOrEmpty(room.address)) { Fail("quickplay: " + (room != null ? room.error : "null")); yield break; }
            Log("matchmaker: " + room.type + " oda " + room.address + ":" + room.port + " oyuncu " + room.players + "/" + room.capacity + " sürüm " + room.version);

            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData(room.address, (ushort)room.port);
            ConnectionGate.PrepareClient();
            if (!nm.StartClient()) { Fail("StartClient"); yield break; }
            yield return WaitFor(() => nm.IsConnectedClient, 20f);
            if (!nm.IsConnectedClient) { Fail("sunucuya baglanilamadi: " + nm.DisconnectReason); yield break; }
            Log("online: sunucuya baglandi");
            yield return WaitFor(() => PlayerNet.Local != null && CaseManager.I != null && CaseManager.I.IsSpawned, 20f);
            if (PlayerNet.Local == null || CaseManager.I == null) { Fail("oyuncu/vaka olusmadi"); yield break; }
            var cm = CaseManager.I;
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

            // 7) deliver the cabinet
            var dz = FindFirstObjectByType<DeliveryZone>();
            var cab = GameObject.Find("ToolCabinet").GetComponent<Rigidbody>();
            cab.position = dz.GetComponent<BoxCollider>().bounds.center + Vector3.up * 0.1f;
            yield return WaitFor(() => cm.Finished.Value, 8f);
            if (!cm.Finished.Value) { Fail("not finished; delivered=" + cm.CabinetDelivered.Value); yield break; }
            Log("case finished ok, elapsed=" + cm.elapsed.Value.ToString("0.0"));

            // 8) tool RPC does not throw
            player.UseToolRpc(1, 0.016f);
            yield return new WaitForSeconds(6f);
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
