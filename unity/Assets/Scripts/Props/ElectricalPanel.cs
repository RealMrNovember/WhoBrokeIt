using System.Collections;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Audio;
using WhoBrokeIt.Interaction;

namespace WhoBrokeIt.Props
{
    /// <summary>
    /// A breakable electrical panel: whole = its lights run, cracked = they flicker and the panel throws sparks, broken = dark and sparking.
    /// Repair it with the hammer like any Destructible. Optional link to the old FuseBox: the fuse box loses power while the panel is damaged
    /// and gets it back when the panel is whole again (server writes the NetworkVariable). The existing FuseBox keeps its own behaviour when unlinked.
    /// </summary>
    [RequireComponent(typeof(Destructible))]
    public class ElectricalPanel : NetworkBehaviour
    {
        public Light[] lights = new Light[0];
        public Renderer indicator;
        public FuseBox linkedFuse;

        Destructible d;
        float nextSpark;
        Coroutine flicker;

        public bool Powered => d != null && d.state.Value == Destructible.Intact;

        void Awake() { d = GetComponent<Destructible>(); }

        public override void OnNetworkSpawn()
        {
            d.StateChanged += OnState;
            Apply(d.state.Value);
        }

        public override void OnNetworkDespawn() { if (d != null) d.StateChanged -= OnState; }

        void OnState(byte prev, byte now)
        {
            Apply(now);
            if (IsServer && linkedFuse != null && linkedFuse.IsSpawned) linkedFuse.powered.Value = now == Destructible.Intact;
        }

        void Apply(byte s)
        {
            if (flicker != null) { StopCoroutine(flicker); flicker = null; }
            if (s == Destructible.Cracked) flicker = StartCoroutine(Flicker());
            else foreach (var l in lights) if (l != null) l.enabled = s == Destructible.Intact;
            if (indicator != null)
            {
                var c = s == Destructible.Intact ? new Color(0.2f, 0.9f, 0.3f) : (s == Destructible.Cracked ? new Color(1f, 0.7f, 0.1f) : new Color(0.9f, 0.2f, 0.2f));
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c);
                indicator.SetPropertyBlock(mpb);
            }
        }

        IEnumerator Flicker()
        {
            while (true)
            {
                var on = Random.value > 0.45f;
                foreach (var l in lights) if (l != null) l.enabled = on;
                yield return new WaitForSeconds(Random.Range(0.06f, 0.35f));
            }
        }

        void Update()
        {
            if (d == null || d.state.Value == Destructible.Intact || Net.DedicatedServer.IsDedicated || Time.time < nextSpark) return;
            nextSpark = Time.time + Random.Range(0.5f, 1.3f);
            var p = d.WorldBounds.center;
            DebrisPool.Puff(p, new Color(1f, 0.85f, 0.3f), 5, 2.6f, 1.2f, 0.045f, 0.45f);
            GameAudio.Play(Sfx.Spark, p, 0.5f, Random.Range(0.9f, 1.2f));
        }
    }
}
