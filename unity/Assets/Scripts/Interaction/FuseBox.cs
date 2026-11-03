using System.Collections;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Interaction
{
    /// <summary>
    /// Repair with the hammer (hold LMB). Repairing before the entrance is clear causes a spark blast (comedy, not failure).
    /// State lives on the server; every peer shows the lights and progress bar from the NetworkVariables.
    /// </summary>
    public class FuseBox : Interactable
    {
        public Light[] lights;
        public Renderer indicator;
        public float repairSeconds = 1.6f;

        public NetworkVariable<bool> powered = new NetworkVariable<bool>(false);
        public NetworkVariable<float> progress = new NetworkVariable<float>(0f);

        bool sparking;
        GUIStyle style;

        public bool Repaired => powered.Value;
        public override string Prompt => powered.Value ? "Sigorta kutusu çalışıyor" : "Çekiçle onar: [3] seç, sol tık basılı tut";

        public override void OnNetworkSpawn()
        {
            powered.OnValueChanged += (_, __) => Apply();
            Apply();
        }

        protected override void ServerInteract(ulong clientId)
        {
            if (!powered.Value) CaseManager.Toast("Bunu çekiçle onar: [3] tuşu, sonra sol tık basılı tut.");
        }

        /// <summary>Server only.</summary>
        public void ServerRepair(float dt)
        {
            if (!IsServer || powered.Value || sparking) return;
            progress.Value += dt / repairSeconds;
            if (progress.Value < 1f) return;
            progress.Value = 0f;

            if (CaseManager.I != null && !CaseManager.I.DebrisCleared.Value)
            {
                sparking = true;
                CaseManager.NoteSpark();
                CaseManager.Toast("ZAAAP! Önce girişi temizleseydin... Kıvılcımlar her yere saçıldı.");
                foreach (var c in Physics.OverlapSphere(transform.position, 6f))
                {
                    var rb = c.attachedRigidbody;
                    if (rb != null && !rb.isKinematic) rb.AddExplosionForce(450f, transform.position + Vector3.down * 0.5f, 6f, 0.5f);
                }
                SparkRpc();
                Invoke(nameof(EndSpark), 0.8f);
                return;
            }
            powered.Value = true;
            if (CaseManager.I != null) CaseManager.I.ServerFuseRepaired();
        }

        void EndSpark() => sparking = false;

        [Rpc(SendTo.ClientsAndHost)]
        void SparkRpc() => StartCoroutine(Flicker());

        IEnumerator Flicker()
        {
            for (var i = 0; i < 6; i++)
            {
                foreach (var l in lights) if (l != null) l.enabled = i % 2 == 0;
                yield return new WaitForSeconds(0.12f);
            }
            Apply();
        }

        void Apply()
        {
            foreach (var l in lights) if (l != null) l.enabled = powered.Value;
            if (indicator != null) indicator.material.color = powered.Value ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
        }

        void OnGUI()
        {
            if (powered.Value || progress.Value <= 0.01f) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            var w = 360f;
            var r = new Rect(Screen.width / 2f - w / 2f, Screen.height * 0.62f, w, 28);
            GUI.Box(r, "");
            GUI.Box(new Rect(r.x + 3, r.y + 3, (w - 6) * Mathf.Clamp01(progress.Value), r.height - 6), "", GUI.skin.box);
            GUI.Label(new Rect(r.x, r.y - 34, w, 30), "Onarılıyor...", style);
        }
    }
}
