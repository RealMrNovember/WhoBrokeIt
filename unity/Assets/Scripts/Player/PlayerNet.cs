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

        public NetworkVariable<byte> toolId = new NetworkVariable<byte>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> hidden = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public NetworkVariable<bool> stunned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public GameObject broomVisual, hammerVisual;
        public Renderer[] bodyRenderers;
        public Rigidbody proxyRb;
        public CarrySystem carry;
        public Vector3 spawnBase = new Vector3(0f, 1.1f, -14f);

        CharacterController cc;
        PlayerController pc;
        Color baseColor = Color.white;
        float immuneUntil, nextMelee;     // server side

        public bool IsHidden => hidden.Value;

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
            if (bodyRenderers != null && bodyRenderers.Length > 0 && bodyRenderers[0] != null) baseColor = bodyRenderers[0].sharedMaterial.color;
        }

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

            if (owner)
            {
                Local = this;
                var slot = (int)(OwnerClientId % 4);
                transform.position = spawnBase + new Vector3((slot - 1.5f) * 1.6f, 0f, 0f);
                cc.enabled = true;

                var tpc = ThirdPersonCamera.Instance;
                if (tpc != null)
                {
                    tpc.SetTarget(transform, new Vector3(0f, 1.6f, 0f), 5.5f);
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
            if (Local == this) Local = null;
            if (proxyRb != null) Destroy(proxyRb.gameObject);
        }

        void Update()
        {
            if (broomVisual != null) broomVisual.SetActive(toolId.Value == 1 && !hidden.Value);
            if (hammerVisual != null) hammerVisual.SetActive(toolId.Value == 2 && !hidden.Value);
            foreach (var r in bodyRenderers) if (r != null) r.enabled = !hidden.Value;
            if (proxyRb != null && IsServer && !IsOwner) proxyRb.gameObject.SetActive(!hidden.Value);

            // dazed players flash red for everyone
            if (bodyRenderers != null && bodyRenderers.Length > 0 && bodyRenderers[0] != null)
            {
                var t = stunned.Value ? 0.5f + 0.5f * Mathf.Sin(Time.time * 22f) : 0f;
                bodyRenderers[0].material.color = Color.Lerp(baseColor, new Color(1f, 0.25f, 0.2f), t);
            }
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
    }
}
