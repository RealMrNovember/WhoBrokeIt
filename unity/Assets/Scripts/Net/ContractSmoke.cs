using System.Collections;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using WhoBrokeIt.Contracts;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Host-mode scenario for the contract system ("-smoke contract"): offers are valid, a board starts the contract in its slot,
    /// delivering the requested items into the zone completes it and scores, a second contract fails when its time runs out,
    /// and a new set of offers appears afterwards. Items are teleported into the zone (the walking is covered by the two-client test).
    /// </summary>
    public class ContractSmoke : MonoBehaviour
    {
        static void L(string s) => Debug.Log("[CONTRACT] " + s);
        int exceptions;

        void OnEnable() { Application.logMessageReceived += OnLog; }
        void OnDisable() { Application.logMessageReceived -= OnLog; }
        void OnLog(string c, string st, LogType t) { if (t == LogType.Exception) exceptions++; }

        void Start() { StartCoroutine(Run()); }

        static void Quit(int code, string why)
        {
            L((code == 0 ? "PASS " : "FAIL ") + why);
            try { System.IO.File.WriteAllText("/results/contract-exit.txt", code.ToString()); } catch { }
            Application.Quit(code);
        }

        IEnumerator WaitFor(System.Func<bool> cond, float timeout)
        {
            var t = 0f;
            while (!cond() && t < timeout) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator Run()
        {
            yield return null;
            var nm = NetworkManager.Singleton;
            ((UnityTransport)nm.NetworkConfig.NetworkTransport).SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
            if (!nm.StartHost()) { Quit(1, "host baslamadi"); yield break; }
            yield return WaitFor(() => PlayerNet.Local != null && ContractDirector.I != null && ContractDirector.I.IsSpawned, 20f);
            var dir = ContractDirector.I;
            if (PlayerNet.Local == null || dir == null) { Quit(1, "oyuncu/yonetici yok"); yield break; }
            yield return new WaitForSeconds(1f);

            var boards = FindObjectsByType<ContractBoard>(FindObjectsSortMode.None).OrderBy(b => b.slot).ToArray();
            var zones = FindObjectsByType<ContractZone>(FindObjectsSortMode.None);
            L("tahta=" + boards.Length + " bolge=" + zones.Length + " esya=" + FindObjectsByType<ContractItem>(FindObjectsSortMode.None).Length);
            if (boards.Length != 3) { Quit(1, "3 pano olmali"); yield break; }
            var o = new[] { ContractDirector.Offer(dir.offers.Value, 0), ContractDirector.Offer(dir.offers.Value, 1), ContractDirector.Offer(dir.offers.Value, 2) };
            L("teklifler=" + string.Join(",", o));
            if (o.Distinct().Count() != 3 || o.Any(x => x < 0 || x >= ContractPool.All.Length)) { Quit(1, "teklifler gecersiz"); yield break; }
            if (!boards.All(b => !string.IsNullOrEmpty(b.Prompt))) { Quit(1, "pano istemi bos"); yield break; }

            // 1) start slot 1 and deliver
            boards[1].RequestInteract();
            yield return WaitFor(() => dir.state.Value == ContractDirector.Active, 5f);
            if (dir.state.Value != ContractDirector.Active) { Quit(1, "sozlesme baslamadi"); yield break; }
            if (dir.activeId.Value != o[1]) { Quit(1, "yanlis sozlesme basladi: " + dir.activeId.Value + " != " + o[1]); yield break; }
            var d = dir.Def(dir.activeId.Value);
            var zone = zones.FirstOrDefault(z => z.zoneId == d.zoneId);
            var items = FindObjectsByType<ContractItem>(FindObjectsSortMode.None).Where(i => i.itemTag == d.itemTag).ToArray();
            L("sozlesme: " + d.title + " hedef=" + d.goal + " esya=" + items.Length + " bolge=" + (zone != null ? zone.zoneId : "YOK"));
            if (zone == null || items.Length < d.goal) { Quit(1, "bolge ya da yeterli esya yok"); yield break; }
            var c = zone.GetComponent<BoxCollider>().bounds.center;
            for (var k = 0; k < d.goal; k++)
            {
                var rb = items[k].Body;
                var p = new Vector3(c.x + (k % 3 - 1) * 1.1f, 0.12f, c.z + (k / 3 - 0.5f) * 1.1f);
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = p; }
                items[k].transform.position = p;
            }
            Physics.SyncTransforms();
            yield return WaitFor(() => dir.state.Value == ContractDirector.Success, 10f);
            L("durum=" + dir.state.Value + " ilerleme=" + dir.progress.Value + "/" + d.goal + " puan=" + dir.score.Value + " tamam=" + dir.completed.Value);
            if (dir.state.Value != ContractDirector.Success) { Quit(1, "sozlesme tamamlanmadi (ilerleme " + dir.progress.Value + "/" + d.goal + ")"); yield break; }
            if (dir.score.Value < d.points || dir.completed.Value != 1) { Quit(1, "puan/sayac yanlis"); yield break; }

            // 2) result screen resets to idle and offers are re-rolled
            var oldOffers = dir.offers.Value;
            yield return WaitFor(() => dir.state.Value == ContractDirector.Idle, 12f);
            if (dir.state.Value != ContractDirector.Idle) { Quit(1, "sonuctan sonra bosta degil"); yield break; }
            var no = new[] { ContractDirector.Offer(dir.offers.Value, 0), ContractDirector.Offer(dir.offers.Value, 1), ContractDirector.Offer(dir.offers.Value, 2) };
            if (no.Distinct().Count() != 3) { Quit(1, "yeni teklifler gecersiz"); yield break; }
            L("yeni teklifler=" + string.Join(",", no) + " (eskisi degisti: " + (oldOffers != dir.offers.Value) + ")");

            // 3) a contract that runs out of time fails
            boards[0].RequestInteract();
            yield return WaitFor(() => dir.state.Value == ContractDirector.Active, 5f);
            if (dir.state.Value != ContractDirector.Active) { Quit(1, "ikinci sozlesme baslamadi"); yield break; }
            dir.timeLeft.Value = 1.5f;
            yield return WaitFor(() => dir.state.Value == ContractDirector.Failed, 8f);
            if (dir.state.Value != ContractDirector.Failed) { Quit(1, "sure dolunca basarisiz olmadi"); yield break; }
            L("sure doldu -> Failed");

            // 4) a second start is refused while one is active (after another idle period)
            yield return WaitFor(() => dir.state.Value == ContractDirector.Idle, 12f);
            boards[2].RequestInteract();
            yield return WaitFor(() => dir.state.Value == ContractDirector.Active, 5f);
            var idBefore = dir.activeId.Value;
            boards[0].RequestInteract();
            yield return new WaitForSeconds(1f);
            if (dir.activeId.Value != idBefore) { Quit(1, "sozlesme sirasinda ikinci baslatma kabul edildi"); yield break; }

            yield return new WaitForSeconds(0.5f);
            if (exceptions > 0) { Quit(1, "istisna sayisi " + exceptions); yield break; }
            Quit(0, "sozlesme sistemi: teklif, baslat, teslim, puan, sure, yeniden teklif, cift baslatma reddi (istisna 0)");
        }
    }
}
