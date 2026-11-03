using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Vehicles
{
    /// <summary>
    /// Arcade forklift. The server simulates it (arcade velocity control + kinematic forks that carry loads by friction);
    /// the driver sends input each frame. W/S drive, A/D steer, R/F fork up/down, E exit, hold X reset.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Forklift : Interactable
    {
        public Rigidbody forks;
        public Vector3 forkOrigin = new Vector3(0f, -0.75f, 0f);
        public float maxSpeed = 5.5f;
        public float accel = 7f;
        public float brake = 14f;
        public float steerSpeed = 55f;
        public float forkSpeed = 0.7f;
        public float forkMin = 0.02f;
        public float forkMax = 2f;

        public NetworkVariable<ulong> driverId = new NetworkVariable<ulong>(ulong.MaxValue);

        Rigidbody body;
        float forkHeight;
        float throttle, steer, forkInput;       // server: last driver input
        float resetHold;                        // owner
        int enteredFrame;
        bool localDriving;
        Behaviour[] disabled;
        GUIStyle style;

        public override string Prompt => driverId.Value == ulong.MaxValue ? "Forklifte bin" : "Forklift dolu";

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            forkHeight = forkMin;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                body.centerOfMass = new Vector3(0f, -0.4f, 0.3f);
                body.interpolation = RigidbodyInterpolation.Interpolate;
                var r = GetComponent<Resettable>();
                if (r != null) r.onReset = () => { forkHeight = forkMin; MoveForks(); };
            }
            driverId.OnValueChanged += OnDriverChanged;
        }

        public override void OnNetworkDespawn() { driverId.OnValueChanged -= OnDriverChanged; }

        // ---------- server ----------
        protected override void ServerInteract(ulong clientId)
        {
            if (driverId.Value == ulong.MaxValue) driverId.Value = clientId;
        }

        [Rpc(SendTo.Server, RequireOwnership = false, Delivery = RpcDelivery.Unreliable)]
        void DriveRpc(float t, float s, float f, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != driverId.Value) return;
            throttle = Mathf.Clamp(t, -1f, 1f);
            steer = Mathf.Clamp(s, -1f, 1f);
            forkInput = Mathf.Clamp(f, -1f, 1f);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ExitRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != driverId.Value) return;
            throttle = steer = forkInput = 0f;
            driverId.Value = ulong.MaxValue;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ResetRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != driverId.Value) return;
            var r = GetComponent<Resettable>();
            if (r != null) { r.ResetNow(); CaseManager.NoteReset(); CaseManager.Toast("Forklift sıfırlandı."); }
        }

        void FixedUpdate()
        {
            if (!IsServer) return;
            if (driverId.Value == ulong.MaxValue) throttle = steer = forkInput = 0f;

            var fwd = transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            var v = body.linearVelocity;
            var speed = Vector3.Dot(v, fwd);
            var target = throttle * maxSpeed;
            speed = Mathf.MoveTowards(speed, target, (Mathf.Abs(target) > 0.01f ? accel : brake) * Time.fixedDeltaTime);
            body.linearVelocity = fwd * speed + Vector3.up * v.y;

            var ang = body.angularVelocity;
            var yawRate = steer * steerSpeed * Mathf.Clamp(speed / 2f, -1f, 1f) * Mathf.Deg2Rad;
            body.angularVelocity = new Vector3(ang.x * 0.9f, yawRate, ang.z * 0.9f);

            forkHeight = Mathf.Clamp(forkHeight + forkInput * forkSpeed * Time.fixedDeltaTime, forkMin, forkMax);
            MoveForks();
        }

        void MoveForks()
        {
            if (forks == null) return;
            forks.MovePosition(body.position + body.rotation * (forkOrigin + Vector3.up * forkHeight));
            forks.MoveRotation(body.rotation);
        }

        // ---------- local driver ----------
        void OnDriverChanged(ulong previous, ulong current)
        {
            var me = NetworkManager.Singleton.LocalClientId;
            if (current == me && !localDriving) EnterLocal();
            else if (previous == me && current != me && localDriving) ExitLocal();
        }

        void EnterLocal()
        {
            var p = PlayerNet.Local;
            if (p == null) return;
            localDriving = true;
            enteredFrame = Time.frameCount;
            disabled = new Behaviour[] { p.GetComponent<PlayerController>(), p.GetComponent<ToolController>(), p.GetComponent<InteractionSystem>() };
            foreach (var b in disabled) if (b != null) b.enabled = false;
            p.GetComponent<CharacterController>().enabled = false;
            p.hidden.Value = true;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(transform, new Vector3(0f, 1.8f, 0f), 8f);
            CaseManager.Toast("Forklift: W/S sür · A/D dön · R/F çatalı kaldır/indir · E çık");
        }

        void ExitLocal()
        {
            var p = PlayerNet.Local;
            localDriving = false;
            if (p == null) return;
            p.transform.position = transform.position + transform.right * 2.4f + Vector3.up * 0.5f;
            Physics.SyncTransforms();
            foreach (var b in disabled) if (b != null) b.enabled = true;
            p.GetComponent<CharacterController>().enabled = true;
            p.hidden.Value = false;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(p.transform, new Vector3(0f, 1.6f, 0f), 5.5f);
        }

        void Update()
        {
            if (!localDriving) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.eKey.wasPressedThisFrame && Time.frameCount != enteredFrame) { ExitRpc(); return; }

            float t = 0f, s = 0f, f = 0f;
            if (kb.wKey.isPressed) t += 1f;
            if (kb.sKey.isPressed) t -= 1f;
            if (kb.dKey.isPressed) s += 1f;
            if (kb.aKey.isPressed) s -= 1f;
            if (kb.rKey.isPressed) f += 1f;
            if (kb.fKey.isPressed) f -= 1f;
            DriveRpc(t, s, f);

            if (kb.xKey.isPressed)
            {
                resetHold += Time.deltaTime;
                if (resetHold > 1f) { resetHold = 0f; ResetRpc(); }
            }
            else resetHold = 0f;
        }

        void OnGUI()
        {
            if (!localDriving) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            var h = forks != null ? forks.position.y - (body.position.y + forkOrigin.y) : 0f;
            GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 36),
                "Çatal yüksekliği: " + h.ToString("0.0") + " m   (R yukarı · F aşağı · E çık · X sıfırla)", style);
        }
    }
}
