using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Net;
using WhoBrokeIt.Audio;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// Repair a Cracked/Broken Destructible with the hammer (hold left mouse with tool 3, same as the fuse box). Two or more players are faster.
    /// The server checks everything: tool selected, distance, facing, state, pace (dt is clamped to the real time since the last request).
    /// Optional requiredKind: a RepairMaterial of that kind must be lying/carried nearby and is consumed on completion.
    /// </summary>
    [RequireComponent(typeof(Destructible))]
    public class Repairable : NetworkBehaviour
    {
        public float seconds = 3f;
        [Tooltip("Empty = no material needed. Otherwise a RepairMaterial of this kind within 3.5 m is consumed.")]
        public string requiredKind = "";
        public float helperBonus = 0.8f;
        public Collider zone;                           // trigger on the root so the interaction search finds a Broken object; enabled only while Broken

        public NetworkVariable<float> progress = new NetworkVariable<float>(0f);

        Destructible d;
        readonly Dictionary<ulong, float> lastRequest = new Dictionary<ulong, float>();
        float lastRepairTime = -10f, lastToast = -10f;
        GUIStyle style;

        public bool NeedsRepair => d != null && d.state.Value != Destructible.Intact;

        void Awake() { d = GetComponent<Destructible>(); }

        public override void OnNetworkSpawn()
        {
            d.StateChanged += OnState;
            OnState(0, d.state.Value);
        }

        public override void OnNetworkDespawn() { if (d != null) d.StateChanged -= OnState; }

        void OnState(byte prev, byte now)
        {
            if (zone != null) zone.enabled = now == Destructible.Broken;
            if (IsServer && now == Destructible.Intact) progress.Value = 0f;
        }

        /// <summary>Server: one hammer tick from a player. Returns true when this object consumed the tick.</summary>
        public bool ServerRepair(PlayerNet who, float dt)
        {
            if (!IsServer || !IsSpawned || who == null || !NeedsRepair) return false;
            if (float.IsNaN(dt) || dt <= 0f) return false;
            var client = who.OwnerClientId;
            if (who.toolId.Value != 2 || who.carry == null || who.carry.IsCarrying || who.hidden.Value) return false;
            var b = d.WorldBounds;
            if (b.SqrDistance(who.transform.position) > 2.6f * 2.6f) return false;

            var now = Time.time;
            lastRequest.TryGetValue(client, out var prev);
            lastRequest[client] = now;
            var real = prev > 0f ? now - prev : 0.1f;
            dt = Mathf.Min(dt, 0.1f, Mathf.Max(0.02f, real * 1.5f));            // cannot repair faster than real time

            RepairMaterial mat = null;
            if (!string.IsNullOrEmpty(requiredKind))
            {
                mat = FindMaterial();
                if (mat == null)
                {
                    if (now - lastToast > 2.5f) { lastToast = now; CaseManager.Toast("Onarmak için malzeme lazım: " + requiredKind + " getir."); }
                    return true;
                }
            }

            var helpers = 0;
            foreach (var kv in lastRequest) if (now - kv.Value < 0.5f) helpers++;
            var rate = 1f + helperBonus * Mathf.Max(0, helpers - 1);
            lastRepairTime = now;
            progress.Value = Mathf.Min(1f, progress.Value + dt / Mathf.Max(0.2f, seconds) * rate);
            if (Mathf.Repeat(progress.Value * 6f, 1f) < dt / seconds * rate * 6f) TapRpc(b.center);     // a hammer tap now and then
            if (progress.Value >= 1f)
            {
                if (mat != null) { var no = mat.GetComponent<NetworkObject>(); if (no != null && no.IsSpawned) no.Despawn(true); else Destroy(mat.gameObject); }
                progress.Value = 0f;
                d.ServerRestore();
            }
            return true;
        }

        RepairMaterial FindMaterial()
        {
            foreach (var c in Physics.OverlapSphere(transform.position, 3.5f))
            {
                var m = c.GetComponentInParent<RepairMaterial>();
                if (m != null && m.kind == requiredKind) return m;
            }
            return null;
        }

        void Update()
        {
            if (!IsServer || progress.Value <= 0f || Time.time - lastRepairTime < 1.5f) return;
            progress.Value = Mathf.Max(0f, progress.Value - 0.25f * Time.deltaTime);   // work in progress fades if everybody walks away
        }

        [Rpc(SendTo.ClientsAndHost, Delivery = RpcDelivery.Unreliable)]
        void TapRpc(Vector3 pos)
        {
            if (Net.DedicatedServer.IsDedicated || float.IsNaN(pos.x) || float.IsInfinity(pos.x)) return;
            GameAudio.Play(Sfx.RepairTap, pos, 0.8f, Random.Range(0.9f, 1.15f));
            DebrisPool.Puff(pos, new Color(1f, 0.9f, 0.5f), 3, 1.5f, 1.2f, 0.05f, 0.4f);
        }

        void OnGUI()
        {
            if (!NeedsRepair || Net.DedicatedServer.IsDedicated) return;
            var me = PlayerNet.Local;
            if (me == null || d.WorldBounds.SqrDistance(me.transform.position) > 3.2f * 3.2f) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            var w = 360f;
            if (progress.Value > 0.01f)
            {
                var r = new Rect(Screen.width / 2f - w / 2f, Screen.height * 0.62f, w, 28);
                GUI.Box(r, "");
                GUI.Box(new Rect(r.x + 3, r.y + 3, (w - 6) * Mathf.Clamp01(progress.Value), r.height - 6), "", GUI.skin.box);
                LocGui.Label(new Rect(r.x, r.y - 34, w, 30), "Onarılıyor...", style);
            }
            else LocGui.Label(new Rect(0, Screen.height * 0.66f, Screen.width, 34), (d.IsBroken ? "Kırık" : "Çatlak") + " · çekiç [3], sol tık basılı tut" + (string.IsNullOrEmpty(requiredKind) ? "" : " · " + requiredKind + " lazım"), style);
        }
    }
}
