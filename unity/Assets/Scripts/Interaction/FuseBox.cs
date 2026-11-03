using System.Collections;
using UnityEngine;
using WhoBrokeIt.Case;

namespace WhoBrokeIt.Interaction
{
    /// <summary>Repair with the hammer (hold LMB). Repairing before the entrance is clear causes a spark blast (comedy, not failure).</summary>
    public class FuseBox : Interactable
    {
        public Light[] lights;
        public Renderer indicator;
        public bool powered;
        public float repairSeconds = 1.6f;

        float progress;
        bool sparking;
        GUIStyle style;

        public bool Repaired => powered;
        public override string Prompt => powered ? "Sigorta kutusu çalışıyor" : "Çekiçle onar: [3] seç, sol tık basılı tut";

        void Start() => Apply();

        public override void Interact(GameObject user)
        {
            if (!powered) CaseManager.Toast("Bunu çekiçle onar: [3] tuşu, sonra sol tık basılı tut.", 3f);
        }

        public void Repair(float dt)
        {
            if (powered || sparking) return;
            progress += dt / repairSeconds;
            if (progress < 1f) return;
            progress = 0f;

            if (CaseManager.I != null && !CaseManager.I.DebrisCleared)
            {
                StartCoroutine(SparkBlast());
                return;
            }
            powered = true;
            Apply();
            if (CaseManager.I != null) CaseManager.I.OnFuseRepaired();
        }

        IEnumerator SparkBlast()
        {
            sparking = true;
            CaseManager.NoteSpark();
            CaseManager.Toast("ZAAAP! Önce girişi temizleseydin... Kıvılcımlar her yere saçıldı.", 3.5f);
            foreach (var c in Physics.OverlapSphere(transform.position, 6f))
            {
                var rb = c.attachedRigidbody;
                if (rb != null && !rb.isKinematic) rb.AddExplosionForce(450f, transform.position + Vector3.down * 0.5f, 6f, 0.5f);
            }
            for (var i = 0; i < 6; i++)
            {
                foreach (var l in lights) if (l != null) l.enabled = i % 2 == 0;
                yield return new WaitForSeconds(0.12f);
            }
            Apply();
            sparking = false;
        }

        void Apply()
        {
            foreach (var l in lights) if (l != null) l.enabled = powered;
            if (indicator != null) indicator.material.color = powered ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
        }

        void OnGUI()
        {
            if (powered || progress <= 0.01f) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
            style.normal.textColor = new Color(1f, 0.85f, 0.2f);
            var w = 360f;
            var r = new Rect(Screen.width / 2f - w / 2f, Screen.height * 0.62f, w, 28);
            GUI.Box(r, "");
            GUI.Box(new Rect(r.x + 3, r.y + 3, (w - 6) * Mathf.Clamp01(progress), r.height - 6), "", GUI.skin.box);
            GUI.Label(new Rect(r.x, r.y - 34, w, 30), "Onarılıyor...", style);
        }
    }
}
