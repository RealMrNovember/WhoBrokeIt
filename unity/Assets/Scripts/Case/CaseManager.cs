using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Case
{
    /// <summary>P1: the fixed "Kimin Alet Çantası?" case. Tracks objectives, notebook, toasts and the finale screen.</summary>
    public class CaseManager : MonoBehaviour
    {
        public static CaseManager I { get; private set; }

        public int cluesTotal = 3;
        public int debrisNeeded = 16;
        public CollectionZone collectionZone;
        public DeliveryZone deliveryZone;

        public bool FuseRepaired { get; private set; }
        public bool DebrisCleared { get; private set; }
        public bool CabinetDelivered { get; private set; }
        public bool Finished { get; private set; }
        public int Sparks { get; private set; }
        public int Resets { get; private set; }

        readonly List<(string title, string text)> clues = new List<(string, string)>();
        readonly Queue<(string msg, float sec)> toasts = new Queue<(string, float)>();
        string toastText;
        float toastUntil;
        float elapsed;
        bool notebookOpen;
        GUIStyle label, big, small, box;

        void Awake() { I = this; }

        void Start()
        {
            Toast("Fitil (telsiz): Greenfield'a hoş geldiniz. İki iyi haberim var: kasabada kimse yok...", 5f);
            Toast("Fitil: ...ve kimse şikâyet etmiyor. Su kulesindeki forklifti sonra konuşuruz.", 5f);
            Toast("Fitil: Atölyeyi açın. Önce etrafı inceleyin. [Tab] defter, [1][2][3] aletler.", 6f);
        }

        public static void Toast(string msg, float sec = 3.5f)
        {
            if (I != null) I.toasts.Enqueue((msg, sec));
        }

        public static void NoteSpark() { if (I != null) I.Sparks++; }
        public static void NoteReset() { if (I != null) I.Resets++; }

        public bool AddClue(string title, string text)
        {
            foreach (var c in clues) if (c.title == title) return false;
            clues.Add((title, text));
            Toast("İpucu bulundu: " + title + " (" + clues.Count + "/" + cluesTotal + ")");
            return true;
        }

        public void OnFuseRepaired()
        {
            FuseRepaired = true;
            Toast("Atölyenin ışıkları yandı!");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame) notebookOpen = !notebookOpen;
                if (Finished && kb.enterKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (!Finished) elapsed += Time.deltaTime;

            if (!DebrisCleared && collectionZone != null && collectionZone.Count >= debrisNeeded)
            {
                DebrisCleared = true;
                Toast("Giriş temiz! Şimdi sigorta kutusu.");
            }
            if (!CabinetDelivered && deliveryZone != null && deliveryZone.Delivered)
            {
                CabinetDelivered = true;
                Toast("Alet dolabı yerinde. Harika iş!");
            }
            if (!Finished && clues.Count >= cluesTotal && DebrisCleared && FuseRepaired && CabinetDelivered)
            {
                Finished = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (toastText != null && Time.time > toastUntil) toastText = null;
            if (toastText == null && toasts.Count > 0)
            {
                var t = toasts.Dequeue();
                toastText = t.msg;
                toastUntil = Time.time + t.sec;
            }
        }

        void EnsureStyles()
        {
            if (label != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
            box = new GUIStyle(GUI.skin.box);
        }

        static string Tick(bool done) { return done ? "[x] " : "[ ] "; }

        void OnGUI()
        {
            EnsureStyles();
            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            var h = 1080f;

            GUI.Box(new Rect(20, 20, 560, 190), "", box);
            label.normal.textColor = Color.white;
            var debris = collectionZone != null ? collectionZone.Count : 0;
            GUI.Label(new Rect(34, 28, 540, 180),
                "GÖREV: Atölyeyi Aç\n" +
                Tick(clues.Count >= cluesTotal) + "İpuçlarını bul (" + clues.Count + "/" + cluesTotal + ")\n" +
                Tick(DebrisCleared) + "Enkazı toplama alanına süpür (" + Mathf.Min(debris, debrisNeeded) + "/" + debrisNeeded + ")\n" +
                Tick(FuseRepaired) + "Sigorta kutusunu çekiçle onar\n" +
                Tick(CabinetDelivered) + "Alet dolabını atölyeye taşı", label);

            small.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(20, h - 60, w - 40, 50),
                "WASD yürü · Shift koş · Space zıpla · E kullan/al/bırak · 1 eller 2 süpürge 3 çekiç · Sol tık kullan · Tab defter · X sıfırla (basılı tut)", small);

            if (toastText != null)
            {
                GUI.Box(new Rect(w / 2f - 480, 24, 960, 90), "", box);
                label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(w / 2f - 470, 28, 940, 84), toastText, label);
                label.alignment = TextAnchor.UpperLeft;
            }

            if (notebookOpen && !Finished)
            {
                GUI.Box(new Rect(w - 620, 120, 600, 520), "", box);
                var sb = new System.Text.StringBuilder("KASABA DEFTERİ\n\n");
                if (clues.Count == 0) sb.Append("(henüz ipucu yok)\n");
                foreach (var c in clues) sb.Append("• ").Append(c.title).Append(": ").Append(c.text).Append("\n\n");
                GUI.Label(new Rect(w - 600, 130, 570, 500), sb.ToString(), label);
            }

            if (Finished) DrawFinale(w, h);
        }

        void DrawFinale(float w, float h)
        {
            GUI.Box(new Rect(w / 2f - 560, 120, 1120, 760), "", box);
            big.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(w / 2f - 540, 140, 1080, 70), "KİM BOZDU?", big);
            label.normal.textColor = Color.white;
            var mins = Mathf.FloorToInt(elapsed / 60f);
            var secs = Mathf.FloorToInt(elapsed % 60f);
            var awards = "";
            if (Sparks > 0) awards += "• Kıvılcım Meraklısı (" + Sparks + " patlama)\n";
            if (Resets > 0) awards += "• Kurtarma Ustası (" + Resets + " sıfırlama)\n";
            if (elapsed < 600f) awards += "• Hızlı İşçi\n";
            if (awards == "") awards = "• Sakin ve Düzenli (şüpheli...)\n";
            GUI.Label(new Rect(w / 2f - 520, 230, 1040, 640),
                "Suçlu: SİNCAP MEHMET.\n\n" +
                "Kabloyu kemirip sigortayı attırdı. Böreği ise Muhtar bırakmıştı (kimse sormadı).\n" +
                "Alet çantası da Mehmet'in yuvasında bulundu, içinde yarım börek vardı.\n\n" +
                "Süre: " + mins + " dk " + secs + " sn\n\nÖdüller:\n" + awards +
                "\nSu kulesindeki forklift ise... bir sonraki vakaya kalıyor.\n\n[Enter] Yeniden oyna", label);
        }
    }
}
