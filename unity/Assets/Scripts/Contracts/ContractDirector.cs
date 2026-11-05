using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Case;
using WhoBrokeIt.Net;

namespace WhoBrokeIt.Contracts
{
    /// <summary>One row of the contract pool. Data only: new contracts are added to ContractPool (later: ScriptableObjects).</summary>
    public struct ContractDef
    {
        public string title, text, itemTag, zoneId, zoneName;
        public int goal;
        public float seconds;
        public int points;
    }

    public static class ContractPool
    {
        public static readonly ContractDef[] All =
        {
            new ContractDef { title = "Palet Taşıma",    text = "Paletleri inşaat alanındaki işaretli alana götür.", itemTag = "pallet", zoneId = "construction", zoneName = "inşaat alanı", goal = 4, seconds = 300f, points = 400 },
            new ContractDef { title = "Tahta İstifi",    text = "Tahta yığınlarını atölyedeki alana taşı.",          itemTag = "planks", zoneId = "workshop",     zoneName = "atölye",        goal = 3, seconds = 240f, points = 300 },
            new ContractDef { title = "Çuval Yarışı",    text = "Çimento çuvallarını depo rampasına getir.",          itemTag = "sack",   zoneId = "dock",         zoneName = "depo rampası",  goal = 6, seconds = 240f, points = 350 },
            new ContractDef { title = "Kasa Dizilimi",   text = "Kasaları meydandaki alana dizip bırak.",              itemTag = "crate",  zoneId = "plaza",        zoneName = "meydan",        goal = 6, seconds = 240f, points = 300 },
            new ContractDef { title = "Varil Yuvarlama", text = "Varilleri tepenin dibindeki alana indir.",           itemTag = "barrel", zoneId = "hill",         zoneName = "tepe dibi",     goal = 4, seconds = 240f, points = 350 },
        };
    }

    /// <summary>
    /// Server-authoritative contract state: three offered contracts on the boards, one active at a time (progress counted from ContractZone),
    /// a countdown, result and score. Everything shown comes from NetworkVariables, so late joiners see the same HUD.
    /// </summary>
    public class ContractDirector : NetworkBehaviour
    {
        public static ContractDirector I { get; private set; }

        public const byte Idle = 0, Active = 1, Success = 2, Failed = 3;
        public NetworkVariable<byte> state = new NetworkVariable<byte>(Idle);
        public NetworkVariable<byte> activeId = new NetworkVariable<byte>(0);
        public NetworkVariable<int> offers = new NetworkVariable<int>(0);     // three ids packed in 8-bit slots
        public NetworkVariable<int> progress = new NetworkVariable<int>(0);
        public NetworkVariable<float> timeLeft = new NetworkVariable<float>(0f);
        public NetworkVariable<int> score = new NetworkVariable<int>(0);
        public NetworkVariable<int> completed = new NetworkVariable<int>(0);

        ContractZone[] zones;
        float resultAt, nextCount;
        System.Random rng;

        void Awake() { I = this; }

        public static int Offer(int packed, int slot) { return (packed >> (8 * slot)) & 0xFF; }
        public ContractDef Def(int id) { return ContractPool.All[Mathf.Clamp(id, 0, ContractPool.All.Length - 1)]; }

        public override void OnNetworkSpawn()
        {
            I = this;
            zones = FindObjectsByType<ContractZone>(FindObjectsSortMode.None);
            if (IsServer) { rng = new System.Random(System.Environment.TickCount); Reroll(); }
            state.OnValueChanged += (a, b) =>
            {
                if (b == Success) { Audio.GameAudio.PlayUi(Audio.Sfx.Pickup); Player.ThirdPersonCamera.AddTrauma(0.15f); }
                if (b == Failed) Audio.GameAudio.PlayUi(Audio.Sfx.UiBack);
            };
        }

        void Reroll()
        {
            var n = ContractPool.All.Length;
            var a = rng.Next(n);
            int b, c;
            do { b = rng.Next(n); } while (b == a);
            do { c = rng.Next(n); } while (c == a || c == b);
            offers.Value = a | (b << 8) | (c << 16);
        }

        /// <summary>Server: a board asks to start the contract in its slot.</summary>
        public void ServerStart(int slot)
        {
            if (!IsServer || state.Value == Active) return;
            var id = Offer(offers.Value, Mathf.Clamp(slot, 0, 2));
            var d = Def(id);
            activeId.Value = (byte)id;
            progress.Value = 0;
            timeLeft.Value = d.seconds;
            state.Value = Active;
            CaseManager.Toast("Sözleşme başladı: " + d.title + " (" + d.goal + " adet, " + Mathf.RoundToInt(d.seconds / 60f) + " dk)", 4f);
        }

        ContractZone ZoneOf(string id)
        {
            if (zones == null || zones.Length == 0) zones = FindObjectsByType<ContractZone>(FindObjectsSortMode.None);
            foreach (var z in zones) if (z != null && z.zoneId == id) return z;
            return null;
        }

        void Update()
        {
            if (!IsServer || !IsSpawned) return;
            if (state.Value == Active)
            {
                var d = Def(activeId.Value);
                timeLeft.Value = Mathf.Max(0f, timeLeft.Value - Time.deltaTime);
                if (Time.time >= nextCount)
                {
                    nextCount = Time.time + 0.25f;
                    var z = ZoneOf(d.zoneId);
                    var n = z != null ? z.Count(d.itemTag) : 0;
                    if (n != progress.Value) progress.Value = n;
                }
                if (progress.Value >= d.goal)
                {
                    var bonus = Mathf.RoundToInt(timeLeft.Value);
                    score.Value += d.points + bonus;
                    completed.Value++;
                    state.Value = Success;
                    resultAt = Time.time;
                    CaseManager.Toast("Sözleşme tamam: " + d.title + "  +" + (d.points + bonus) + " puan", 5f);
                }
                else if (timeLeft.Value <= 0f)
                {
                    state.Value = Failed;
                    resultAt = Time.time;
                    CaseManager.Toast("Süre doldu: " + d.title + ". Panodan yenisini başlat.", 5f);
                }
            }
            else if ((state.Value == Success || state.Value == Failed) && Time.time - resultAt > 8f)
            {
                state.Value = Idle;
                Reroll();
            }
        }

        // ---------- HUD (every peer, drawn from the NetworkVariables) ----------
        void OnGUI()
        {
            if (!IsSpawned || state.Value == Idle) return;
            var prevMatrix = GUI.matrix;
            var s = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            var w = Screen.width / s;
            var d = Def(activeId.Value);
            var r = new Rect(w / 2f - 330, 18, 660, 112);
            var ok = state.Value == Success; var fail = state.Value == Failed;
            UiKit.Card(r, ok ? new Color(0.12f, 0.45f, 0.3f, 0.95f) : fail ? new Color(0.55f, 0.15f, 0.18f, 0.95f) : new Color(0.09f, 0.11f, 0.22f, 0.9f), 26f);
            UiKit.Text(new Rect(r.x + 24, r.y + 8, 440, 40), d.title, 30, UiKit.Sun, TextAnchor.MiddleLeft, FontStyle.Bold);
            var secs = Mathf.CeilToInt(timeLeft.Value);
            UiKit.Text(new Rect(r.xMax - 190, r.y + 8, 166, 40), (secs / 60) + ":" + (secs % 60).ToString("00"), 30, secs < 30 && state.Value == Active ? UiKit.Coral : Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
            var text = ok ? "Tamamlandı!  Puan: " + score.Value : fail ? "Süre doldu." : d.text;
            UiKit.Text(new Rect(r.x + 24, r.y + 46, r.width - 48, 30), text, 20, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleLeft, FontStyle.Normal, false);
            var bar = new Rect(r.x + 24, r.y + 82, r.width - 48, 16);
            UiKit.Fill(bar, new Color(1, 1, 1, 0.16f), 8f);
            var f = Mathf.Clamp01(progress.Value / (float)Mathf.Max(1, d.goal));
            UiKit.Fill(new Rect(bar.x, bar.y, Mathf.Max(16f, bar.width * f), bar.height), ok ? UiKit.Mint : UiKit.Sun, 8f);
            UiKit.Text(new Rect(bar.x, bar.y - 4, bar.width, 24), progress.Value + " / " + d.goal + "  →  " + d.zoneName, 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            GUI.matrix = prevMatrix;
        }
    }
}
