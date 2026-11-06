using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Contracts;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Net
{
    /// <summary>
    /// Two-person carry scenario (modes twoPairA / twoPairB, see tools/mp pair.sh): both clients stand at a pallet, A grips, B joins, together they
    /// lift it and walk, then A lets go. Everything is logged with the tag PAIR and judged by the script.
    /// </summary>
    public partial class TwoClientSmoke
    {
        IEnumerator RunPair()
        {
            yield return Connect();
            yield return WaitWorld(30f);
            if (worldReadyAt < 0f) { Fail("dunya yuklenmedi"); yield break; }
            var nm = NetworkManager.Singleton;
            var t = 0f;
            while (FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Length < 2 && t < 30f) { t += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(2f);

            var player = PlayerNet.Local;
            var cc = player.GetComponent<CharacterController>();
            var pallet = FindObjectsByType<ContractItem>(FindObjectsSortMode.None).Where(i => i.itemTag == "pallet").OrderBy(i => i.transform.position.z).First();
            var prb = pallet.GetComponent<Rigidbody>();
            var no = pallet.GetComponent<NetworkObject>();
            var carryable = pallet.GetComponent<Carryable>();
            if (carryable == null) { Fail("palet tasinabilir degil"); yield break; }
            L("PAIR " + who + " carryable=" + carryable.name + " mass=" + carryable.GetComponent<Rigidbody>().mass + " same=" + (carryable.gameObject == pallet.gameObject) + " no=" + (no != null));
            var p0 = pallet.transform.position;
            // both stand on open ground in front of the pallet, shoulder to shoulder
            var side = who == "A" ? -0.9f : 0.9f;
            Place(cc, player, p0 + new Vector3(side, 0.6f, -1.6f));
            player.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            yield return new WaitForSeconds(1.5f);
            L("PAIR " + who + " pos=" + player.transform.position.ToString("0.0") + " pallet=" + p0.ToString("0.0") + " mass=" + prb.mass);

            if (who == "A") { carryable.RequestInteract(); yield return new WaitForSeconds(2.5f); }
            else { yield return new WaitForSeconds(1.5f); carryable.RequestInteract(); yield return new WaitForSeconds(1.0f); }
            yield return new WaitForSeconds(1.5f);

            var holders = CarrySystem.HoldersOf(no.NetworkObjectId).Count;
            var lifted = pallet.transform.position.y - p0.y;
            L("PAIR " + who + " lift holders=" + holders + " lifted=" + lifted.ToString("0.00") + " speedScale=" + player.carry.SpeedScale.ToString("0.00"));

            // walk forward together for 3 seconds at the carry speed
            var startPos = pallet.transform.position;
            var wt = 0f;
            while (wt < 3f)
            {
                cc.Move((Vector3.forward * 4.5f * player.carry.SpeedScale + Vector3.down * 2f) * Time.deltaTime);
                wt += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(0.5f);
            var moved = Vector3.Distance(new Vector3(pallet.transform.position.x, 0f, pallet.transform.position.z), new Vector3(startPos.x, 0f, startPos.z));
            var others = FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Where(p => p != player).ToArray();
            var mid = others.Length > 0 ? (others[0].transform.position + player.transform.position) * 0.5f : player.transform.position;
            var gap = Vector2.Distance(new Vector2(pallet.transform.position.x, pallet.transform.position.z), new Vector2(mid.x, mid.z));
            L("PAIR " + who + " walk moved=" + moved.ToString("0.00") + " gapToMidpoint=" + gap.ToString("0.00") + " holders=" + CarrySystem.HoldersOf(no.NetworkObjectId).Count);

            if (who == "A") { player.DropRpc(); }
            yield return new WaitForSeconds(2.5f);
            L("PAIR " + who + " dropped holders=" + CarrySystem.HoldersOf(no.NetworkObjectId).Count + " iHold=" + player.carry.IsCarrying + " exc=" + exceptions);
            yield return new WaitForSeconds(2f);
            Quit(exceptions == 0 ? 0 : 1);
        }
    }
}
