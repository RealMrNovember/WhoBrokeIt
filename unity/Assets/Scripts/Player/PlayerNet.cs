using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Per-player network glue. The owner runs movement/camera/input locally; the server runs everything that changes the shared world
    /// (tool effects, resets, pushing via a kinematic proxy collider).
    /// </summary>
    public class PlayerNet : NetworkBehaviour
    {
        public static PlayerNet Local { get; private set; }

        /// <summary>The name shown above the player to everybody (from the profile; "Oyuncu N" when empty).</summary>
        public NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<byte> toolId = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> hidden = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<bool> stunned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        /// <summary>Index into CharacterCatalog; chosen in the menu and shown to everyone.</summary>
        public NetworkVariable<int> characterId = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        /// <summary>True while sitting on a vehicle (everyone animates the sitting pose; the server proxy collider is switched off).</summary>
        public NetworkVariable<bool> seated = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> climbing = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        /// <summary>What the player is riding: 0 nothing, 1 skateboard, 2 bicycle (everybody animates the matching stance).</summary>
        public NetworkVariable<byte> ride = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        /// <summary>Crank angle in degrees, written every frame by the bicycle the player rides so the legs follow the pedals.</summary>
        [System.NonSerialized] public float pedalAngle;
        /// <summary>Counts one-shot actions (throw, hit) so every client can play the swing: low bit of kind in the value, counter in the rest.</summary>
        public NetworkVariable<int> actionTick = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public GameObject broomVisual, hammerVisual;
        public Transform visualAnchor;
        public Renderer[] bodyRenderers = new Renderer[0];
        GameObject visualInstance;
        public Rigidbody proxyRb;
        public CarrySystem carry;
        public Vector3 spawnBase = new Vector3(0f, 1.1f, -14f);

        CharacterController cc;
        PlayerController pc;
        float immuneUntil, nextMelee;     // server side

        public bool IsHidden => hidden.Value;

        static readonly System.Collections.Generic.Dictionary<ulong, PlayerNet> byOwner = new System.Collections.Generic.Dictionary<ulong, PlayerNet>();
        static float lastOwnerScan = -10f;

        /// <summary>The player object of a client, on any machine (SpawnManager.GetPlayerNetworkObject only works for other clients on the server and throws elsewhere).</summary>
        public static PlayerNet ByOwner(ulong clientId)
        {
            if (Local != null && Local.OwnerClientId == clientId) return Local;
            if (byOwner.TryGetValue(clientId, out var p) && p != null) return p;
            if (Time.unscaledTime - lastOwnerScan < 0.3f) return null;
            lastOwnerScan = Time.unscaledTime;
            byOwner.Clear();
            foreach (var o in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None)) byOwner[o.OwnerClientId] = o;
            return byOwner.TryGetValue(clientId, out p) ? p : null;
        }

        /// <summary>Owner: announce a one-shot action to everyone (kind 0 = throw, 1 = hit).</summary>
        public void NoteAction(int kind) { if (IsOwner) actionTick.Value = ((actionTick.Value >> 1) + 1) << 1 | kind; }

        /// <summary>Finds the player a collider belongs to: the player itself (own controller) or its detached server proxy.</summary>
        public static PlayerNet FromCollider(Collider c)
        {
            var p = c.GetComponentInParent<PlayerNet>();
            if (p != null) return p;
            var proxy = c.GetComponent<Props.ProxyOwner>();
            return proxy != null ? proxy.owner : null;
        }

        public static PlayerNet ForClient(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.ConnectedClients.TryGetValue(clientId, out var c) || c.PlayerObject == null) return null;
            return c.PlayerObject.GetComponent<PlayerNet>();
        }

        /// <summary>Colliders of this player that held objects must ignore (server side).</summary>
        public Collider[] BodyColliders()
        {
            var list = new System.Collections.Generic.List<Collider>();
            if (cc != null) list.Add(cc);
            if (proxyRb != null) list.AddRange(proxyRb.GetComponentsInChildren<Collider>());
            return list.ToArray();
        }

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            pc = GetComponent<PlayerController>();
        }

        void RebuildVisual(int id)
        {
            if (visualInstance != null) Destroy(visualInstance);
            visualInstance = null;
            bodyRenderers = new Renderer[0];
            if (visualAnchor == null || Net.DedicatedServer.IsDedicated) return;     // servers never draw anything
            visualInstance = CharacterCatalog.Build(id, visualAnchor);
            if (visualInstance != null) bodyRenderers = visualInstance.GetComponentsInChildren<Renderer>();
        }

        void OnCharacterChanged(int previous, int current) => RebuildVisual(current);

        public override void OnNetworkSpawn()
        {
            var owner = IsOwner;
            cc.enabled = false;
            GetComponent<PlayerController>().enabled = owner;
            GetComponent<ToolController>().enabled = owner;
            GetComponent<InteractionSystem>().enabled = owner;

            if (proxyRb != null)
            {
                proxyRb.transform.SetParent(null, true);
                proxyRb.gameObject.SetActive(IsServer && !owner);
            }

            characterId.OnValueChanged += OnCharacterChanged;
            if (owner)
            {
                characterId.Value = CharacterCatalog.SavedChoice();
                var nm = PlayerPrefs.GetString("wbi_name", "").Trim();
                playerName.Value = nm.Length > 0 ? nm : "Oyuncu " + (OwnerClientId + 1);
            }
            RebuildVisual(characterId.Value);

            if (owner)
            {
                Local = this;
                var slot = (int)(OwnerClientId % 4);
                transform.position = spawnBase + new Vector3((slot - 1.5f) * 1.6f, 0f, 0f);
                cc.enabled = true;

                var tpc = ThirdPersonCamera.Instance;
                if (tpc != null)
                {
                    tpc.SetPlayer(transform);
                    tpc.enabled = true;
                }
                var pc = GetComponent<PlayerController>();
                if (tpc != null) pc.cameraTransform = tpc.transform;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            characterId.OnValueChanged -= OnCharacterChanged;
            if (Local == this) Local = null;
            if (proxyRb != null) Destroy(proxyRb.gameObject);
        }

        void Update()
        {
            if (broomVisual != null) broomVisual.SetActive(toolId.Value == 1 && !hidden.Value);
            if (hammerVisual != null) hammerVisual.SetActive(toolId.Value == 2 && !hidden.Value);
            // own body is hidden in first person (you would see the inside of your head); everyone is hidden while driving
            var firstPerson = IsOwner && ThirdPersonCamera.Instance != null && ThirdPersonCamera.Instance.IsFirstPerson;
            var show = !hidden.Value && !firstPerson;
            foreach (var r in bodyRenderers) if (r != null && r.enabled != show) r.enabled = show;
            if (proxyRb != null && IsServer && !IsOwner) proxyRb.gameObject.SetActive(!hidden.Value && !seated.Value && !climbing.Value && ride.Value == 0);
            if (IsOwner && pc != null && stunned.Value != pc.Stunned) stunned.Value = pc.Stunned;
        }

        void FixedUpdate()
        {
            if (proxyRb != null && proxyRb.gameObject.activeSelf && IsServer && !IsOwner)
                proxyRb.MovePosition(transform.position);
        }

        // ---------- owner -> server requests ----------

        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        public void UseToolRpc(byte tool, float dt)
        {
            if (tool == 1) Sweep();
            else if (tool == 2) Hammer(dt);
        }

        void Sweep()
        {
            var center = transform.position + transform.forward * 1.7f + Vector3.down * 0.5f;
            foreach (var c in Physics.OverlapSphere(center, 1.5f))
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb.isKinematic || rb.mass > 8f) continue;
                rb.AddForce(transform.forward * 24f + Vector3.up * 2f, ForceMode.Acceleration);
            }
        }

        void Hammer(float dt)
        {
            var center = transform.position + transform.forward * 1.0f;
            foreach (var c in Physics.OverlapSphere(center, 2.4f))
            {
                var rep = c.GetComponentInParent<Props.Repairable>();
                if (rep != null && rep.NeedsRepair)
                {
                    var rt = rep.transform.position - transform.position;
                    rt.y = 0f;
                    if (Vector3.Dot(transform.forward, rt.normalized) > 0.1f && rep.ServerRepair(this, Mathf.Clamp(dt, 0f, 0.1f))) return;
                }
                var fuse = c.GetComponentInParent<FuseBox>();
                if (fuse == null) continue;
                var to = fuse.transform.position - transform.position;
                to.y = 0f;
                if (Vector3.Dot(transform.forward, to.normalized) < 0.2f) continue;
                fuse.ServerRepair(Mathf.Clamp(dt, 0f, 0.1f));
                return;
            }
        }

        [Rpc(SendTo.Server)]
        public void ResetNearestRpc()
        {
            var r = Resettable.Nearest(transform.position, 8f);
            if (r == null) return;
            r.ResetNow();
            CaseManager.NoteReset();
            CaseManager.Toast("Sıfırlandı.");
        }

        [Rpc(SendTo.Server)]
        public void DropRpc() => carry.ServerDrop();

        // ---------- throwing and hitting (comedy knockback, no damage) ----------

        /// <summary>Owner asks to throw what it is carrying; the camera direction is sent along.</summary>
        [Rpc(SendTo.Server)]
        public void ThrowRpc(Vector3 aim)
        {
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            carry.ServerThrow(aim, this);
        }

        /// <summary>Owner swings at whoever stands in front (right mouse). Short cooldown so nobody gets stun-locked.</summary>
        [Rpc(SendTo.Server)]
        public void MeleeRpc()
        {
            if (Time.time < nextMelee || hidden.Value) return;
            nextMelee = Time.time + 0.7f;
            foreach (var other in FindObjectsByType<PlayerNet>(FindObjectsSortMode.None))
            {
                if (other == this || other.hidden.Value) continue;
                var to = other.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > 2.2f || Vector3.Dot(transform.forward, to.normalized) < 0.35f) continue;
                other.ServerBonk(to.normalized * 8f + Vector3.up * 2.5f, 0.7f);
            }
        }

        /// <summary>Server: knock this player back and stun them. Returns false while they are still immune from the last hit.</summary>
        public bool ServerBonk(Vector3 impulse, float stunSeconds)
        {
            if (!IsServer || Time.time < immuneUntil) return false;
            immuneUntil = Time.time + stunSeconds + 0.8f;
            BonkRpc(impulse, stunSeconds);
            Case.CaseManager.NoteBonk();
            return true;
        }

        [Rpc(SendTo.Owner)]
        void BonkRpc(Vector3 impulse, float stunSeconds)
        {
            if (pc != null) pc.ApplyBonk(impulse, stunSeconds);
        }

        // ---------- name tag above the other players' heads ----------
        static GUIStyle tagStyle;
        static Texture2D tagBg;

        // ---------- chat and emotes ----------
        public NetworkVariable<byte> emote = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        float lastChatAt;

        /// <summary>Owner: show an emote (1 wave, 2 point, 3 cheer, 4 dance); 0 stops it.</summary>
        public void SetEmote(byte e) { if (IsOwner && emote.Value != e) emote.Value = e; }

        public void SendChat(string text)
        {
            if (!IsOwner || string.IsNullOrEmpty(text)) return;
            if (text.Length > 100) text = text.Substring(0, 100);
            ChatRpc(new FixedString128Bytes(text));
        }

        [Rpc(SendTo.Server)]
        void ChatRpc(FixedString128Bytes text)
        {
            if (Time.time - lastChatAt < 0.7f) return;                       // rate limit per player
            lastChatAt = Time.time;
            var clean = new System.Text.StringBuilder();
            foreach (var ch in text.ToString()) if (!char.IsControl(ch)) clean.Append(ch);
            var msg = clean.ToString().Trim();
            if (msg.Length == 0 || msg.Length > 100) return;
            var who = playerName.Value.Length > 0 ? playerName.Value : new FixedString32Bytes("Oyuncu " + (OwnerClientId + 1));
            ChatShowRpc(who, new FixedString128Bytes(msg));
        }

        [Rpc(SendTo.Everyone)]
        void ChatShowRpc(FixedString32Bytes who, FixedString128Bytes text) => Net.ChatBox.Add(who.ToString(), text.ToString());

        public static bool DebugTagSelf;        // screenshot tour only: draw the tag over our own head too

        void OnGUI()
        {
            if ((IsOwner && !DebugTagSelf) || !IsSpawned || hidden.Value || Event.current.type != EventType.Repaint || Net.DedicatedServer.IsDedicated) return;
            var cam = Camera.main;
            if (cam == null || !cam.enabled) return;
            var feet = visualAnchor != null ? visualAnchor.position : transform.position - Vector3.up;
            var world = feet + Vector3.up * 2.0f;
            var dist = Vector3.Distance(cam.transform.position, world);
            if (dist > 60f) return;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f) return;
            var s = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f) * Mathf.Lerp(1.15f, 0.7f, dist / 60f), 0.5f, 1.3f);
            if (tagStyle == null) tagStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            if (tagBg == null) { tagBg = new Texture2D(1, 1); tagBg.SetPixel(0, 0, Color.white); tagBg.Apply(); }
            var label = playerName.Value.ToString();
            if (label.Length == 0) label = "Oyuncu " + (OwnerClientId + 1);
            tagStyle.fontSize = Mathf.RoundToInt(24f * s);
            var w = Mathf.Max(80f * s, label.Length * 13.5f * s + 28f * s); var h = 34f * s;
            var r = new Rect(sp.x - w / 2f, Screen.height - sp.y - h, w, h);
            var old = GUI.color;
            GUI.color = new Color(0.05f, 0.07f, 0.16f, 0.62f);
            GUI.DrawTexture(r, tagBg);
            GUI.color = Color.white;
            tagStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), label, tagStyle);
            tagStyle.normal.textColor = new Color(1f, 0.93f, 0.55f);
            GUI.Label(r, label, tagStyle);
            GUI.color = old;
        }
    }
}
