using System.Collections;
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
    /// Arcade forklift. The server simulates it: the hull follows the ground (it tilts onto ramps instead of tipping over), speed and heading are
    /// controlled directly, the forks are a kinematic body that carries loads by friction. The driver climbs in, sits down and is visible.
    /// W/S drive, A/D steer, R/F fork up/down, E exit, hold X reset. Wheels roll and the front wheels steer on every machine.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Forklift : Interactable
    {
        public Rigidbody forks;
        public Vector3 forkOrigin = new Vector3(0f, -0.75f, 0f);
        public float maxSpeed = 5.5f;
        public float accel = 7f;
        public float brake = 14f;
        public float steerMax = 32f;
        public float wheelBase = 1.5f;
        public float forkSpeed = 0.7f;
        public float forkMin = 0.02f;
        public float forkMax = 2f;

        [Header("Visuals (set by the scene builder)")]
        public Transform[] steerPivots;          // front wheels: yaw
        public Transform[] spinPivots;           // all wheels: roll
        public float[] wheelRadius;
        public Transform steeringWheel;
        public Vector3[] wheelPoints;            // local positions where the wheels touch the ground (for the ground probe)
        public Vector3 seatLocal = new Vector3(0f, 0.98f, -0.14f);

        public NetworkVariable<ulong> driverId = new NetworkVariable<ulong>(ulong.MaxValue);
        public NetworkVariable<float> steerAngle = new NetworkVariable<float>(0f);

        Rigidbody body;
        float forkHeight;
        float throttle, steer, forkInput;       // server: last driver input
        float driveSpeed, heading, steerNow;    // server: arcade state
        Vector3 groundNormal = Vector3.up;
        float resetHold;                        // owner
        int enteredFrame;
        bool localDriving;
        Behaviour[] disabled;
        GUIStyle style;
        Coroutine climbRoutine;
        float spin;
        Vector3 lastVisualPos;

        public override string Prompt => driverId.Value == ulong.MaxValue ? "Forklifte bin" : "Forklift dolu";

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            forkHeight = forkMin;
        }

        public override void OnNetworkSpawn()
        {
            // The kinematic forks sit a few centimetres in front of the hull; they must not push the hull.
            if (forks != null)
                foreach (var hc in GetComponentsInChildren<Collider>())
                    foreach (var fc in forks.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(hc, fc);

            // wheels and hull slide freely, the arcade drive controls the motion
            var slick = new PhysicsMaterial("ForkliftSlick") { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
            foreach (var hc in GetComponentsInChildren<Collider>()) hc.sharedMaterial = slick;

            lastVisualPos = transform.position;
            if (IsServer)
            {
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.useGravity = false;                  // gravity is applied by hand so the hull can stick to ramps
                body.constraints = RigidbodyConstraints.FreezeRotation;
                heading = transform.eulerAngles.y;
                var r = GetComponent<Resettable>();
                if (r != null) r.onReset = () => { forkHeight = forkMin; driveSpeed = 0f; heading = transform.eulerAngles.y; groundNormal = Vector3.up; MoveForks(); };
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
            if (rpcParams.Receive.SenderClientId != driverId.Value || testInput) return;
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

        /// <summary>Server only: drive input injection used by the automated tests.</summary>
        public void ServerTestInput(float t, float s, float f) { testInput = true; throttle = t; steer = s; forkInput = f; }
        bool testInput;
        public float ForkHeight => forkHeight;
        public void RequestExit() => ExitRpc();

        /// <summary>Server only (tests): put the forklift somewhere facing a heading, standing still.</summary>
        public void ServerTeleport(Vector3 pos, float yaw)
        {
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            body.position = pos; body.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.linearVelocity = Vector3.zero;
            heading = yaw; driveSpeed = 0f; steerNow = 0f; groundNormal = Vector3.up;
            Physics.SyncTransforms();
        }
        public float Heading => heading;
        public Vector3 GroundNormal => groundNormal;
        public string Diag;

        /// <summary>Average normal of the ground under the wheels, and how many wheels touch.</summary>
        int Probe(out Vector3 normal)
        {
            normal = Vector3.zero;
            var n = 0;
            var pts = wheelPoints != null && wheelPoints.Length > 0 ? wheelPoints : new[] { new Vector3(-0.6f, 0f, 0.6f), new Vector3(0.6f, 0f, 0.6f), new Vector3(-0.6f, 0f, -0.8f), new Vector3(0.6f, 0f, -0.8f) };
            foreach (var p in pts)
            {
                var o = transform.TransformPoint(p + Vector3.up * 0.7f);
                var hits = Physics.RaycastAll(o, Vector3.down, 1.25f, ~0, QueryTriggerInteraction.Ignore);
                var best = float.MaxValue;
                var bn = Vector3.up;
                foreach (var h in hits)
                {
                    if (h.collider.transform.IsChildOf(transform) || (forks != null && h.collider.transform.IsChildOf(forks.transform))) continue;
                    if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;     // loose props are not ground
                    if (h.distance < best) { best = h.distance; bn = h.normal; }
                }
                if (best < float.MaxValue) { normal += bn; n++; }
            }
            if (n > 0) normal.Normalize();
            return n;
        }

        void FixedUpdate()
        {
            if (!IsServer) return;
            var dt = Time.fixedDeltaTime;
            if (driverId.Value == ulong.MaxValue) throttle = steer = forkInput = 0f;

            // ground contact and orientation: yaw is our own heading, pitch/roll follow the ground (smoothed), so the hull never tips over
            var touching = Probe(out var n);
            var grounded = touching >= 2;
            groundNormal = Vector3.Slerp(groundNormal, grounded ? n : Vector3.up, 1f - Mathf.Exp(-9f * dt));
            if (groundNormal.y < 0.5f) groundNormal = Vector3.up;                          // never lean beyond ~60 degrees

            var target = throttle * maxSpeed;
            driveSpeed = Mathf.MoveTowards(driveSpeed, target, (Mathf.Abs(target) > 0.01f ? accel : brake) * dt);

            steerNow = Mathf.MoveTowards(steerNow, steer * steerMax, 140f * dt);
            if (Mathf.Abs(steerAngle.Value - steerNow) > 0.2f) steerAngle.Value = steerNow;
            heading += Mathf.Rad2Deg * driveSpeed * Mathf.Tan(steerNow * Mathf.Deg2Rad) / wheelBase * dt;

            var rot = Quaternion.FromToRotation(Vector3.up, groundNormal) * Quaternion.Euler(0f, heading, 0f);
            body.MoveRotation(rot);
            body.angularVelocity = Vector3.zero;

            var fwd = rot * Vector3.forward;
            if (grounded)
                body.linearVelocity = fwd * driveSpeed - groundNormal * 1.2f;                // follow the slope, stay glued to it
            else
            {
                var v = body.linearVelocity;
                var flat = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized * driveSpeed;
                body.linearVelocity = new Vector3(flat.x, v.y - 9.81f * dt, flat.z);
            }

            forkHeight = Mathf.Clamp(forkHeight + forkInput * forkSpeed * dt, forkMin, forkMax);
            MoveForks();
        }

        void MoveForks()
        {
            if (forks == null) return;
            forks.MovePosition(body.position + body.rotation * (forkOrigin + Vector3.up * forkHeight));
            forks.MoveRotation(body.rotation);
        }

        // ---------- visuals on every machine: rolling wheels, steering ----------
        void LateUpdate()
        {
            var dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var delta = transform.position - lastVisualPos;
            lastVisualPos = transform.position;
            var fwdSpeed = Vector3.Dot(delta, transform.forward) / dt;
            fwdSpeed = Mathf.Clamp(fwdSpeed, -12f, 12f);
            spin += fwdSpeed * dt;                                 // metres rolled; converted per wheel radius below
            var steerDeg = steerAngle.Value;
            if (spinPivots != null)
                for (var i = 0; i < spinPivots.Length; i++)
                {
                    var r = wheelRadius != null && i < wheelRadius.Length ? wheelRadius[i] : 0.3f;
                    if (spinPivots[i] != null) spinPivots[i].localRotation = Quaternion.Euler((spin / r) * Mathf.Rad2Deg, 0f, 0f);
                }
            if (steerPivots != null)
                foreach (var s in steerPivots) if (s != null) s.localRotation = Quaternion.Euler(0f, steerDeg, 0f);
            if (steeringWheel != null) steeringWheel.localRotation = Quaternion.Euler(-35f, 0f, 0f) * Quaternion.Euler(0f, steerDeg * 2.6f, 0f);

            if (localDriving && seated && PlayerNet.Local != null)
            {
                var p = PlayerNet.Local.transform;
                p.SetPositionAndRotation(SeatRoot(PlayerNet.Local), Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            }
        }

        // ---------- local driver: climb in, sit, drive ----------
        bool seated;

        void OnDriverChanged(ulong previous, ulong current)
        {
            var me = NetworkManager.Singleton.LocalClientId;
            if (current == me && !localDriving) EnterLocal();
            else if (previous == me && current != me && localDriving) ExitLocal();
        }

        /// <summary>World position of the player's root (which hangs 1 m above the feet) when sitting on the seat.</summary>
        Vector3 SeatRoot(PlayerNet p)
        {
            var anim = p.GetComponentInChildren<CharacterAnimator>();
            var hip = anim != null ? anim.HipHeight : 0.86f;
            var feetAtSeat = transform.TransformPoint(seatLocal) - transform.up * hip;
            return feetAtSeat + Vector3.up * 1f;
        }

        void EnterLocal()
        {
            var p = PlayerNet.Local;
            if (p == null) return;
            localDriving = true;
            seated = false;
            enteredFrame = Time.frameCount;
            disabled = new Behaviour[] { p.GetComponent<PlayerController>(), p.GetComponent<ToolController>(), p.GetComponent<InteractionSystem>() };
            foreach (var b in disabled) if (b != null) b.enabled = false;
            p.GetComponent<CharacterController>().enabled = false;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(transform, new Vector3(0f, 1.8f, 0f), 8f);
            climbRoutine = StartCoroutine(Climb(p));
            CaseManager.Toast("Forklift: W/S sür · A/D dön · R/F çatalı kaldır/indir · E çık");
        }

        IEnumerator Climb(PlayerNet p)
        {
            p.climbing.Value = true;
            var t = p.transform;
            var start = t.position;
            var side = Vector3.Dot(start - transform.position, transform.right) >= 0f ? 1f : -1f;
            var step = transform.TransformPoint(new Vector3(side * 1.05f, 0f, -0.12f)) + Vector3.up * 1f;      // root height when standing beside the hull
            step.y = start.y;
            // 1) walk up to the side of the cab
            var d1 = 0.45f;
            for (var e = 0f; e < d1; e += Time.deltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, e / d1);
                t.position = Vector3.Lerp(start, step, k);
                var look = transform.position - t.position; look.y = 0f;
                if (look.sqrMagnitude > 0.01f) t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(look), 12f * Time.deltaTime);
                yield return null;
            }
            // 2) pull up onto the seat in an arc
            var d2 = 0.75f;
            var from = t.position;
            for (var e = 0f; e < d2; e += Time.deltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, e / d2);
                var seat = SeatRoot(p);
                var pos = Vector3.Lerp(from, seat, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
                t.SetPositionAndRotation(pos, Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, transform.eulerAngles.y, 0f), 10f * Time.deltaTime));
                yield return null;
            }
            p.climbing.Value = false;
            p.seated.Value = true;
            seated = true;
            climbRoutine = null;
        }

        void ExitLocal()
        {
            var p = PlayerNet.Local;
            localDriving = false;
            seated = false;
            if (climbRoutine != null) { StopCoroutine(climbRoutine); climbRoutine = null; }
            if (p == null) return;
            p.seated.Value = false;
            p.climbing.Value = false;
            var side = transform.right * 1.6f;
            p.transform.SetPositionAndRotation(transform.position + side + Vector3.up * 1.1f, Quaternion.Euler(0f, transform.eulerAngles.y + 90f, 0f));
            Physics.SyncTransforms();
            foreach (var b in disabled) if (b != null) b.enabled = true;
            p.GetComponent<CharacterController>().enabled = true;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(p.transform, new Vector3(0f, 1.6f, 0f), ThirdPersonCamera.DistancePref);
        }

        void Update()
        {
            if (!localDriving) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (PauseMenu_Open()) return;
            if (kb.eKey.wasPressedThisFrame && Time.frameCount != enteredFrame && seated) { ExitRpc(); return; }

            float t = 0f, s = 0f, f = 0f;
            if (seated)
            {
                if (kb.wKey.isPressed) t += 1f;
                if (kb.sKey.isPressed) t -= 1f;
                if (kb.dKey.isPressed) s += 1f;
                if (kb.aKey.isPressed) s -= 1f;
                if (kb.rKey.isPressed) f += 1f;
                if (kb.fKey.isPressed) f -= 1f;
            }
            DriveRpc(t, s, f);

            if (kb.xKey.isPressed)
            {
                resetHold += Time.deltaTime;
                if (resetHold > 1f) { resetHold = 0f; ResetRpc(); }
            }
            else resetHold = 0f;
        }

        static bool PauseMenu_Open() { return Net.PauseMenu.Open; }

        void OnGUI()
        {
            if (!localDriving || !seated) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            var h = forks != null ? forks.position.y - (body.position.y + forkOrigin.y) : 0f;
            GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 36),
                "Çatal yüksekliği: " + h.ToString("0.0") + " m   (R yukarı · F aşağı · E çık · X sıfırla)", style);
        }
    }
}
