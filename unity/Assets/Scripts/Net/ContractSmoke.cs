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
                var p = new Vector3(c.x + (k % 3 - 1) * 1.45f, 0.12f, c.z + (k / 3 - 0.5f) * 1.5f);   // wide enough that even the 1.2 m crates do not shove each other out of the zone
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.position = p; }
                items[k].transform.position = p;
            }
            Physics.SyncTransforms();
            yield return WaitFor(() => dir.state.Value == ContractDirector.Success, 10f);
            L("durum=" + dir.state.Value + " ilerleme=" + dir.progress.Value + "/" + d.goal + " puan=" + dir.score.Value + " tamam=" + dir.completed.Value);
            if (dir.state.Value != ContractDirector.Success) { Quit(1, "sozlesme tamamlanmadi (ilerleme " + dir.progress.Value + "/" + d.goal + ")"); yield break; }
            if (dir.score.Value < d.points || dir.completed.Value != 1) { Quit(1, "puan/sayac yanlis"); yield break; }

            // 1b) the delivered pile is spent: it must not count for the next contract until it left the zone and came back
            if (zone.Count(d.itemTag) != 0) { Quit(1, "teslim edilen esyalar harcanmadi (bolgede hala " + zone.Count(d.itemTag) + " sayiliyor)"); yield break; }
            var spentItem = items[0];
            var away = spentItem.transform.position + new Vector3(0f, 0f, 30f);
            spentItem.Body.position = away; spentItem.transform.position = away;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(1.2f);
            // back into the zone, onto the free centre spot and from a little height (the other items still lie around it)
            var back = new Vector3(c.x, 0.6f, c.z - 2.2f * 0f);
            spentItem.Body.linearVelocity = Vector3.zero; spentItem.Body.angularVelocity = Vector3.zero; spentItem.Body.position = back; spentItem.transform.position = back;
            Physics.SyncTransforms();
            yield return WaitFor(() => zone.Count(d.itemTag) >= 1, 4f);
            if (zone.Count(d.itemTag) < 1) { Quit(1, "bolgeden cikip geri konan esya tekrar sayilmadi (sayilan " + zone.Count(d.itemTag) + ") esya=" + spentItem.name + " tag=" + spentItem.itemTag + " konum=" + spentItem.transform.position + " bolge=" + zone.GetComponent<BoxCollider>().bounds + " hiz=" + spentItem.Body.linearVelocity.magnitude.ToString("0.0") + " yercekimi=" + spentItem.Body.useGravity + " harcanan=" + spentItem.spentZone + " aktif=" + spentItem.gameObject.activeInHierarchy); yield break; }
            L("harcanan esya kurali: bolgede 0 sayildi; cikarilip geri konunca sayildi");

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
            yield return new WaitForSeconds(1f);
            if (dir.state.Value != ContractDirector.Active) { Quit(1, "ikinci sozlesme hemen bitti (durum " + dir.state.Value + "): onceki esyalar tekrar sayildi"); yield break; }
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

            // 5) the forklift must be able to lift a pallet: slide the forks in, raise, back away, lower
            var fk = FindFirstObjectByType<WhoBrokeIt.Vehicles.Forklift>();
            var pallet = FindObjectsByType<ContractItem>(FindObjectsSortMode.None).Where(i => i.itemTag == "pallet" && i.Body != null && (i.GetComponent<WhoBrokeIt.Props.Destructible>() == null || !i.GetComponent<WhoBrokeIt.Props.Destructible>().IsBroken)).Where(i => { var q = i.transform.position; return Physics.Raycast(new Vector3(q.x, 4f, q.z - 4f), Vector3.down, out var g0, 8f, ~0, QueryTriggerInteraction.Ignore) && g0.point.y < 0.3f; }).OrderBy(i => i.transform.position.z).ThenBy(i => i.transform.position.x).FirstOrDefault();
            if (fk != null && pallet != null)
            {
                fk.RequestInteract();
                yield return WaitFor(() => fk.driverId.Value == nm.LocalClientId, 5f);
                if (fk.driverId.Value != nm.LocalClientId) { Quit(1, "forklift'e binilemedi"); yield break; }
                yield return new WaitForSeconds(1f);
                // the test pallets stand inside the warehouse among other props: move this one to a clear, level patch of ground so only the fork geometry is under test
                Vector3? spot = null;
                foreach (var gx in new[] { 0f, -8f, 8f, -16f, 16f, 0f, -8f, 8f })
                    foreach (var gz in new[] { -6f, 0f, 6f, -12f, 12f })
                    {
                        if (spot != null) break;
                        var cb = new Vector3(gx, 0.9f, gz);
                        if (!Physics.CheckBox(cb, new Vector3(1.8f, 0.8f, 6f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore) && Physics.Raycast(new Vector3(gx, 4f, gz), Vector3.down, out var sg, 8f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(sg.point.y) < 0.05f) spot = new Vector3(gx, 0.02f, gz + 1.5f);
                    }
                if (spot == null) { Quit(1, "palet testi icin bos zemin bulunamadi"); yield break; }
                pallet.Body.linearVelocity = Vector3.zero; pallet.Body.angularVelocity = Vector3.zero;
                pallet.transform.SetPositionAndRotation(spot.Value, Quaternion.identity); pallet.Body.position = spot.Value; pallet.Body.rotation = Quaternion.identity;
                Physics.SyncTransforms();
                yield return new WaitForSeconds(0.5f);
                L("palet tasindi: " + spot.Value.ToString("0.00"));
                var pp = pallet.transform.position;
                var groundY = pp.y;
                float Ground(float zz) { return Physics.Raycast(new Vector3(pp.x, 4f, zz), Vector3.down, out var gg, 8f, ~0, QueryTriggerInteraction.Ignore) ? gg.point.y : 0f; }
                var sgn = (Ground(pp.z - 4f) > 0.3f && Ground(pp.z + 4f) <= 0.3f) ? -1f : 1f;      // approach from the side whose ground is level with the pallet
                L("palet zemin: guney=" + Ground(pp.z - 4f).ToString("0.00") + " kuzey=" + Ground(pp.z + 4f).ToString("0.00") + " yon=" + sgn);
                var d0 = 4f;
                fk.ServerTeleport(new Vector3(pp.x, 0.1f, pp.z - sgn * d0), sgn > 0f ? 0f : 180f);
                yield return new WaitForSeconds(0.5f); L("palet teleport sonrasi: x=" + fk.transform.position.x.ToString("0.00") + " istenen=" + pp.x.ToString("0.00") + " z=" + fk.transform.position.z.ToString("0.00"));
                fk.ServerTestInput(0.35f, 0f, 0f);   // forward along the forklift heading
                var tt = 0f;
                var nx = 0f; var dumped = false;
                while (tt < 10f && sgn * (pp.z - fk.transform.position.z) > 1.75f)
                {
                    tt += Time.deltaTime;
                    if (tt > nx) { nx += 0.3f; L("palet gidis: t=" + tt.ToString("0.0") + " x=" + fk.transform.position.x.ToString("0.00") + " z=" + fk.transform.position.z.ToString("0.00")); }
                    if (!dumped && Vector3.Distance(pallet.transform.position, pp) > 0.03f)
                    {
                        dumped = true;
                        var sb = new System.Text.StringBuilder("palet ilk temas: forkY=" + fk.ForkHeight.ToString("0.00") + " hullY=" + fk.transform.position.y.ToString("0.00"));
                        foreach (var c0 in fk.forks.GetComponentsInChildren<Collider>()) sb.Append(" | F:" + c0.name + " " + c0.bounds.min.ToString("0.00") + ".." + c0.bounds.max.ToString("0.00"));
                        foreach (var c0 in pallet.GetComponentsInChildren<Collider>()) sb.Append(" | P:" + c0.name + " " + c0.bounds.min.ToString("0.00") + ".." + c0.bounds.max.ToString("0.00"));
                        L(sb.ToString());
                    }
                    yield return null;
                }
                fk.ServerTestInput(0f, 0f, 0f);
                var gap = sgn * (pp.z - fk.transform.position.z); L("palet yaklasma: govdeY=" + fk.transform.position.y.ToString("0.00") + " paletY=" + pp.y.ToString("0.00") + " paletZ=" + pp.z.ToString("0.00") + " paletX=" + pp.x.ToString("0.00") + " govdeX=" + fk.transform.position.x.ToString("0.00") + " yaw=" + fk.transform.eulerAngles.y.ToString("0") + " govdeZ=" + fk.transform.position.z.ToString("0.00") + " alt=" + (Physics.Raycast(fk.transform.position + Vector3.up * 3f, Vector3.down, out var gh, 6f, ~0, QueryTriggerInteraction.Ignore) ? gh.collider.name + "@" + gh.point.y.ToString("0.00") : "yok") + " kirik=" + (pallet.GetComponent<WhoBrokeIt.Props.Destructible>() != null && pallet.GetComponent<WhoBrokeIt.Props.Destructible>().IsBroken));
                yield return new WaitForSeconds(0.6f);
                var movedByForks = Vector3.Distance(pallet.transform.position, pp);
                fk.ServerTestInput(0f, 0f, 1f);
                yield return new WaitForSeconds(1.6f);
                fk.ServerTestInput(0f, 0f, 0f);
                yield return new WaitForSeconds(0.5f);
                var lifted = pallet.transform.position.y - groundY;
                fk.ServerTestInput(-0.4f, 0f, 0f);
                yield return new WaitForSeconds(1.0f);
                fk.ServerTestInput(0f, 0f, 0f);
                var stillOn = pallet.transform.position.y - groundY;
                var pb = new Bounds(pallet.transform.position, Vector3.zero); var pcn = 0;
                foreach (var c0 in pallet.GetComponentsInChildren<Collider>()) { pb.Encapsulate(c0.bounds); pcn++; }
                var fb = new Bounds(fk.forks.position, Vector3.zero); var fcn = 0; var fcs = "";
                foreach (var c0 in fk.forks.GetComponentsInChildren<Collider>()) { fb.Encapsulate(c0.bounds); fcn++; fcs += c0.name + (c0.isTrigger ? "(T)" : "") + " "; }
                L("palet tani: kirik=" + (pallet.GetComponent<WhoBrokeIt.Props.Destructible>() != null && pallet.GetComponent<WhoBrokeIt.Props.Destructible>().IsBroken) + " kinematik=" + pallet.Body.isKinematic + " kutle=" + pallet.Body.mass + " collider=" + pcn + " sinir=" + pb.min.ToString("0.00") + ".." + pb.max.ToString("0.00") + " | catal collider=" + fcn + " [" + fcs + "] sinir=" + fb.min.ToString("0.00") + ".." + fb.max.ToString("0.00") + " | ust=" + (pallet.transform.localScale.y).ToString("0.00") + " katman=" + pallet.gameObject.layer);
                L("palet kaldirma: yaklasma=" + gap.ToString("0.00") + " itilme=" + movedByForks.ToString("0.00") + " m kalkis=" + lifted.ToString("0.00") + " m geri giderken=" + stillOn.ToString("0.00") + " m catal=" + fk.ForkHeight.ToString("0.00"));
                fk.ServerTestInput(0f, 0f, -1f);
                yield return new WaitForSeconds(2.5f);
                fk.ServerTestInput(0f, 0f, 0f);
                if (lifted < 0.5f) { Quit(1, "forklift paleti kaldiramadi (kalkis " + lifted.ToString("0.00") + " m, itilme " + movedByForks.ToString("0.00") + " m)"); yield break; }
                if (stillOn < 0.4f) { Quit(1, "palet geri giderken catalda kalmadi (" + stillOn.ToString("0.00") + " m)"); yield break; }
            }
            else L("forklift/palet bulunamadi, palet adimi atlandi");

            // 6) the hand cart: Space loads a crate onto the bed, it rides along, Space again tips it off
            var cart = FindObjectsByType<WhoBrokeIt.Vehicles.RideableVehicle>(FindObjectsSortMode.None).FirstOrDefault(r => r.kind == WhoBrokeIt.Vehicles.RideKind.Cart);
            var crate = FindObjectsByType<WhoBrokeIt.Interaction.Carryable>(FindObjectsSortMode.None).Where(c => c.GetComponent<Rigidbody>() != null && !c.GetComponent<Rigidbody>().isKinematic && c.GetComponent<Rigidbody>().mass <= 25f && c.GetComponent<Rigidbody>().mass >= 2f && (c.GetComponent<WhoBrokeIt.Props.Destructible>() == null || !c.GetComponent<WhoBrokeIt.Props.Destructible>().IsBroken)).OrderBy(c => c.GetComponent<Rigidbody>().mass).FirstOrDefault();
            if (cart != null && crate != null)
            {
                Vector3? cspot = null;
                foreach (var gx in new[] { 0f, -8f, 8f, 16f, -16f, 24f, -24f, 12f, -12f, 4f })
                    foreach (var gz in new[] { -6f, 0f, 6f, 12f, -12f, 20f, 26f })
                    {
                        if (cspot != null) break;
                        if (!Physics.CheckBox(new Vector3(gx, 0.9f, gz), new Vector3(1.3f, 0.8f, 2.2f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore) && Physics.Raycast(new Vector3(gx, 4f, gz), Vector3.down, out var cg, 8f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(cg.point.y) < 0.05f) cspot = new Vector3(gx, 0.05f, gz);
                    }
                if (cspot == null) L("el arabasi testi: bos zemin yok, atlandi");
                else
                {
                    var crb = crate.GetComponent<Rigidbody>();
                    cart.ServerTeleport(cspot.Value, 0f);
                    crb.linearVelocity = Vector3.zero; crb.angularVelocity = Vector3.zero;
                    crb.position = cspot.Value + new Vector3(1.5f, 0.4f, 0.3f); crate.transform.position = crb.position; Physics.SyncTransforms();
                    yield return new WaitForSeconds(1.0f);
                    { var cq = cart.transform.position; cart.ServerTestInput(0.6f, 0f, 0f); yield return new WaitForSeconds(1.0f); cart.ServerTestInput(0f, 0f, 0f); yield return new WaitForSeconds(0.6f); L("el arabasi bos surus: gidilen=" + Vector3.Distance(cq, cart.transform.position).ToString("0.00") + " m ileride=" + (Physics.SphereCast(cq + Vector3.up * 0.5f, 0.4f, Vector3.forward, out var ah, 4f, ~0, QueryTriggerInteraction.Ignore) ? ah.collider.name + "@" + ah.distance.ToString("0.00") : "yok") + " altinda=" + (Physics.Raycast(cq + Vector3.up * 1f, Vector3.down, out var ad, 3f, ~0, QueryTriggerInteraction.Ignore) ? ad.collider.name + "@" + ad.point.y.ToString("0.00") : "yok")); cart.ServerTeleport(cspot.Value, 0f); crb.position = cspot.Value + new Vector3(1.5f, 0.4f, 0.3f); crate.transform.position = crb.position; crb.linearVelocity = Vector3.zero; Physics.SyncTransforms(); yield return new WaitForSeconds(1.0f); }
                    cart.ServerTestLoad();
                    yield return new WaitForSeconds(1.5f);
                    var cartTop = cart.transform.position.y + cart.bedY;
                    var loadedOk = Vector3.Distance(new Vector3(crb.position.x, 0f, crb.position.z), new Vector3(cart.transform.position.x, 0f, cart.transform.position.z)) < 1.0f && crb.position.y > cartTop - 0.05f;
                    var p0 = crb.position; var c0p = cart.transform.position;
                    cart.ServerTestInput(0.6f, 0f, 0f);
                    yield return new WaitForSeconds(1.0f);
                    { var crb2 = cart.GetComponent<Rigidbody>(); L("el arabasi surus: hiz=" + cart.Speed.ToString("0.00") + " govdeHiz=" + crb2.linearVelocity.ToString("0.00") + " kinematik=" + crb2.isKinematic + " yercekimi=" + crb2.useGravity + " pos=" + cart.transform.position.ToString("0.00")); }
                    yield return new WaitForSeconds(1.0f);
                    cart.ServerTestInput(0f, 0f, 0f);
                    yield return new WaitForSeconds(0.5f);
                    var follow = Vector3.Distance(new Vector3(crb.position.x, 0f, crb.position.z), new Vector3(cart.transform.position.x, 0f, cart.transform.position.z));
                    var travelled = Vector3.Distance(p0, crb.position);
                    cart.ServerTestLoad();
                    yield return new WaitForSeconds(1.2f);
                    var offDist = Vector3.Distance(new Vector3(crb.position.x, 0f, crb.position.z), new Vector3(cart.transform.position.x, 0f, cart.transform.position.z));
                    L("el arabasi: araba_gidilen=" + Vector3.Distance(c0p, cart.transform.position).ToString("0.00") + " hiz=" + cart.Speed.ToString("0.00") + " yuklendi=" + loadedOk + " yukseklik=" + (crb.position.y - cartTop).ToString("0.00") + " gidilen=" + travelled.ToString("0.00") + " m takip=" + follow.ToString("0.00") + " m indirme=" + offDist.ToString("0.00") + " m");
                    if (!loadedOk) { Quit(1, "el arabasi kutuyu yuklemedi"); yield break; }
                    if (travelled < 1.0f || follow > 1.0f) { Quit(1, "el arabasinda kutu arabayla gitmedi (gidilen " + travelled.ToString("0.00") + ", takip " + follow.ToString("0.00") + ")"); yield break; }
                    if (offDist < 0.9f) { Quit(1, "el arabasi kutuyu indirmedi (" + offDist.ToString("0.00") + " m)"); yield break; }
                }
            }
            else L("el arabasi/kutu bulunamadi, el arabasi adimi atlandi");
            // 7) placement audit: NPCs, vendors and stalls must not stand inside walls, props or each other
            {
                var bad = 0;
                var seen = new System.Collections.Generic.HashSet<string>();
                for (var sample = 0; sample < 60; sample++)       // 60 s of wandering: nobody may walk into anything
                {
                    foreach (var npc0 in FindObjectsByType<WhoBrokeIt.World.Npc>(FindObjectsSortMode.None))
                    {
                        var p = npc0.transform.position;
                        var names = Physics.OverlapCapsule(p + Vector3.up * 0.45f, p + Vector3.up * 1.5f, 0.28f, ~0, QueryTriggerInteraction.Ignore).Where(h => !h.transform.IsChildOf(npc0.transform) && h.bounds.max.y > 0.3f).Select(h => h.name).Distinct().ToArray();
                        if (names.Length > 0 && seen.Add(npc0.name + ">" + string.Join(",", names))) { bad++; L("yerlesim: " + npc0.name + " " + p.ToString("0.0") + " engel=" + string.Join(",", names) + " (t=" + sample + ")"); }
                    }
                    yield return new WaitForSeconds(1f);
                }
                foreach (var tr in FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "prop_stall_a"))
                {
                    var cols = tr.GetComponentsInChildren<Collider>();
                    if (cols.Length == 0) { L("yerlesim: tezgah " + tr.position.ToString("0.0") + " collider yok"); continue; }
                    var b = cols[0].bounds; foreach (var cc9 in cols) b.Encapsulate(cc9.bounds);
                    var names = Physics.OverlapBox(b.center, b.extents * 0.9f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Where(h => !h.transform.IsChildOf(tr) && h.bounds.max.y > 0.3f).Select(h => h.name).Distinct().ToArray();
                    if (names.Length > 0) { bad++; L("yerlesim: tezgah " + tr.position.ToString("0.0") + " engel=" + string.Join(",", names)); }
                }
                foreach (var sc in FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(q => q.name == "COL_prop_stall_a_1")) L("yerlesim: tezgah-collider " + sc.bounds.min.ToString("0.00") + ".." + sc.bounds.max.ToString("0.00"));
                L("yerlesim: sorunlu=" + bad);
            }

            yield return new WaitForSeconds(0.5f);
            if (exceptions > 0) { Quit(1, "istisna sayisi " + exceptions); yield break; }
            Quit(0, "sozlesme sistemi: teklif, baslat, teslim, puan, sure, yeniden teklif, cift baslatma reddi (istisna 0)");
        }
    }
}
