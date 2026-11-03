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

        public GameObject broomVisual, hammerVisual;
        public Renderer[] bodyRenderers;
        public Rigidbody proxyRb;
        public CarrySystem carry;
        public Vector3 spawnBase = new Vector3(0f, 1.1f, -14f);

        CharacterController cc;

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

        void Awake() => cc = GetComponent<CharacterController>();

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
    }
}
