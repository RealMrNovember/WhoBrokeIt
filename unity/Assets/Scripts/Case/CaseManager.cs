using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Case
{
    /// <summary>
    /// The fixed "Kimin Alet Çantası?" case. The server owns all progress (NetworkVariables); every peer renders the same HUD,
    /// notebook and finale from it. Toasts are broadcast by the server.
    /// </summary>
    public class CaseManager : NetworkBehaviour
    {
        public static CaseManager I { get; private set; }

        public int cluesTotal = 3;
        public int debrisNeeded = 16;
        public CollectionZone collectionZone;
        public DeliveryZone deliveryZone;

        public NetworkVariable<int> clueMask = new NetworkVariable<int>(0);
        public NetworkVariable<int> debrisCount = new NetworkVariable<int>(0);
        public NetworkVariable<int> sparks = new NetworkVariable<int>(0);
        public NetworkVariable<int> resets = new NetworkVariable<int>(0);
        public NetworkVariable<int> bonks = new NetworkVariable<int>(0);
        public NetworkVariable<float> elapsed = new NetworkVariable<float>(0f);
        public NetworkVariable<bool> DebrisCleared = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> FuseRepaired = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> CabinetDelivered = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> Finished = new NetworkVariable<bool>(false);
        public NetworkVariable<FixedString32Bytes> roomCode = new NetworkVariable<FixedString32Bytes>(default);
        public NetworkVariable<float> restartIn = new NetworkVariable<float>(-1f);

        float lastCount;
        int playerCount = 1;

        readonly Queue<(string msg, float sec)> toasts = new Queue<(string, float)>();
        string toastText;
        float toastUntil;
        bool notebookOpen, cursorFreed;
        GUIStyle label, big, small, box;

        void Awake() { I = this; }

        public override void OnNetworkSpawn()
        {
            I = this;
            LocalToast("Fitil (telsiz): Greenfield'a hoş geldiniz. İki iyi haberim var: kasabada kimse yok...", 5f);
            LocalToast("Fitil: ...ve kimse şikâyet etmiyor. Su kulesindeki forklifti sonra konuşuruz.", 5f);
            LocalToast("Fitil: Atölyeyi açın. Önce etrafı inceleyin. [Tab] defter, [1][2][3] aletler.", 6f);
        }

        // ---------- toasts ----------
        public static void Toast(string msg, float sec = 3.5f)
        {
            if (I == null) return;
            if (I.IsServer && I.IsSpawned) I.ToastRpc(msg, sec);
            else I.LocalToast(msg, sec);
        }

        [Rpc(SendTo.ClientsAndHost)]
        void ToastRpc(string msg, float sec) => LocalToast(msg, sec);

        void LocalToast(string msg, float sec) => toasts.Enqueue((msg, sec));

        // ---------- server-side state changes ----------
        public static void NoteSpark() { if (I != null && I.IsServer) I.sparks.Value++; }
        public static void NoteReset() { if (I != null && I.IsServer) I.resets.Value++; }
        public static void NoteBonk() { if (I != null && I.IsServer) I.bonks.Value++; }

        public bool HasClue(int index) => (clueMask.Value & (1 << index)) != 0;
        public int ClueCount
        {
            get { var n = 0; for (var i = 0; i < 16; i++) if (HasClue(i)) n++; return n; }
        }

        public bool ServerAddClue(int index, string title)
        {
            if (!IsServer || HasClue(index)) return false;
            clueMask.Value |= 1 << index;
            Toast("İpucu bulundu: " + title + " (" + ClueCount + "/" + cluesTotal + ")");
            return true;
        }

        public void ServerFuseRepaired()
        {
            if (!IsServer) return;
            FuseRepaired.Value = true;
            Toast("Atölyenin ışıkları yandı!");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame) notebookOpen = !notebookOpen;
                if (Finished.Value && IsServer && kb.enterKey.wasPressedThisFrame)
                    NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            }

            if (Finished.Value && !cursorFreed)
            {
                cursorFreed = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Time.time - lastCount > 0.5f)
            {
                lastCount = Time.time;
                playerCount = Mathf.Max(1, FindObjectsByType<Player.PlayerNet>(FindObjectsSortMode.None).Length);
            }

            if (toastText != null && Time.time > toastUntil) toastText = null;
            if (toastText == null && toasts.Count > 0)
            {
                var t = toasts.Dequeue();
                toastText = t.msg;
                toastUntil = Time.time + t.sec;
            }

            if (!IsServer) return;

            if (!Finished.Value) elapsed.Value += Time.deltaTime;
            if (collectionZone != null) debrisCount.Value = collectionZone.Count;
            if (!DebrisCleared.Value && debrisCount.Value >= debrisNeeded)
            {
                DebrisCleared.Value = true;
                Toast("Giriş temiz! Şimdi sigorta kutusu.");
            }
            if (!CabinetDelivered.Value && deliveryZone != null && deliveryZone.Delivered)
            {
                CabinetDelivered.Value = true;
                Toast("Alet dolabı yerinde. Harika iş!");
            }
            if (!Finished.Value && ClueCount >= cluesTotal && DebrisCleared.Value && FuseRepaired.Value && CabinetDelivered.Value)
                Finished.Value = true;

            // dedicated servers have nobody to press Enter: start the next case automatically
            if (Finished.Value && !IsClient)
            {
                if (restartIn.Value < 0f) restartIn.Value = 45f;
                restartIn.Value -= Time.deltaTime;
                if (restartIn.Value <= 0f)
                    NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
            }
        }

        // ---------- HUD ----------
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
            if (!IsSpawned) return;
            EnsureStyles();
            var s = Screen.height / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            var h = 1080f;

            GUI.Box(new Rect(20, 20, 560, 190), "", box);
            label.normal.textColor = Color.white;
            GUI.Label(new Rect(34, 28, 540, 180),
                "GÖREV: Atölyeyi Aç\n" +
                Tick(ClueCount >= cluesTotal) + "İpuçlarını bul (" + ClueCount + "/" + cluesTotal + ")\n" +
                Tick(DebrisCleared.Value) + "Enkazı toplama alanına süpür (" + Mathf.Min(debrisCount.Value, debrisNeeded) + "/" + debrisNeeded + ")\n" +
                Tick(FuseRepaired.Value) + "Sigorta kutusunu çekiçle onar\n" +
                Tick(CabinetDelivered.Value) + "Alet dolabını atölyeye taşı", label);

            small.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(20, h - 60, w - 40, 50),
                "WASD yürü · Shift koş · Space zıpla · E kullan/al/bırak · 1 eller 2 süpürge 3 çekiç · Sol tık kullan (taşırken: fırlat) · Sağ tık vur · Tab defter · X sıfırla (basılı tut)", small);

            if (roomCode.Value.Length > 0)
            {
                GUI.Box(new Rect(w - 360, 20, 340, 112), "", box);
                small.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
                GUI.Label(new Rect(w - 350, 24, 320, 24), "ODA KODU · " + playerCount + "/4 oyuncu", small);
                big.normal.textColor = new Color(1f, 0.85f, 0.2f);
                var oldSize = big.fontSize;
                big.fontSize = 38;
                GUI.Label(new Rect(w - 350, 44, 320, 48), roomCode.Value.ToString(), big);
                big.fontSize = oldSize;
                if (GUI.Button(new Rect(w - 280, 94, 180, 30), "Kodu kopyala")) GUIUtility.systemCopyBuffer = roomCode.Value.ToString();
            }

            if (toastText != null)
            {
                GUI.Box(new Rect(w / 2f - 480, 24, 960, 90), "", box);
                label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(w / 2f - 470, 28, 940, 84), toastText, label);
                label.alignment = TextAnchor.UpperLeft;
            }

            if (notebookOpen && !Finished.Value)
            {
                GUI.Box(new Rect(w - 620, 120, 600, 520), "", box);
                var sb = new System.Text.StringBuilder("KASABA DEFTERİ\n\n");
                var any = false;
                foreach (var c in FindObjectsByType<Clue>(FindObjectsSortMode.InstanceID))
                {
                    if (!HasClue(c.index)) continue;
                    any = true;
                    sb.Append("• ").Append(c.title).Append(": ").Append(c.text).Append("\n\n");
                }
                if (!any) sb.Append("(henüz ipucu yok)\n");
                GUI.Label(new Rect(w - 600, 130, 570, 500), sb.ToString(), label);
            }

            if (Finished.Value) DrawFinale(w);
        }

        void DrawFinale(float w)
        {
            GUI.Box(new Rect(w / 2f - 560, 120, 1120, 760), "", box);
            big.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(w / 2f - 540, 140, 1080, 70), "KİM BOZDU?", big);
            label.normal.textColor = Color.white;
            var mins = Mathf.FloorToInt(elapsed.Value / 60f);
            var secs = Mathf.FloorToInt(elapsed.Value % 60f);
            var awards = "";
            if (sparks.Value > 0) awards += "• Kıvılcım Meraklısı (" + sparks.Value + " patlama)\n";
            if (resets.Value > 0) awards += "• Kurtarma Ustası (" + resets.Value + " sıfırlama)\n";
            if (bonks.Value > 0) awards += "• Dostane Bonk Şampiyonu (" + bonks.Value + " bonk)\n";
            if (elapsed.Value < 600f) awards += "• Hızlı İşçi\n";
            if (awards == "") awards = "• Sakin ve Düzenli (şüpheli...)\n";
            GUI.Label(new Rect(w / 2f - 520, 230, 1040, 640),
                "Suçlu: SİNCAP MEHMET.\n\n" +
                "Kabloyu kemirip sigortayı attırdı. Böreği ise Muhtar bırakmıştı (kimse sormadı).\n" +
                "Alet çantası da Mehmet'in yuvasında bulundu, içinde yarım börek vardı.\n\n" +
                "Süre: " + mins + " dk " + secs + " sn\n\nÖdüller:\n" + awards +
                "\nSu kulesindeki forklift ise... bir sonraki vakaya kalıyor.\n\n" +
                (restartIn.Value > 0f ? "Yeni vaka " + Mathf.CeilToInt(restartIn.Value) + " sn içinde başlıyor..." :
                 IsServer ? "[Enter] Yeniden oyna" : "Oda sahibi yeniden başlatabilir."), label);
        }
    }
}
