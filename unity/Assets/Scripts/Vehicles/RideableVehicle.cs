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
    public enum RideKind : byte { Skateboard = 1, Bicycle = 2, Cart = 3 }

    /// <summary>
    /// Skateboard and bicycle. Same arcade model as the forklift: the server controls heading and speed, the body follows the ground, wheels roll,
    /// and the rider hops on, stands (skateboard) or sits (bicycle) and is visible to everybody. W/S speed, A/D steer, Space jumps (skateboard), E gets off.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RideableVehicle : Interactable
    {
        public RideKind kind = RideKind.Skateboard;
        public string label = "Kaykay";
        public float maxSpeed = 8f;
        public float accel = 9f;
        public float brake = 9f;
        public float steerMax = 30f;
        public float wheelBase = 0.55f;
        public float jumpSpeed = 4.2f;

        [Header("Visuals / geometry (set by the scene builder)")]
        public Transform[] steerPivots;          // front wheel + handlebar: yaw
        public Transform[] spinPivots;           // wheels: roll
        public float[] wheelRadius;
        public Transform crank;                  // bicycle: pedal crank
        public Transform tiltPivot;              // skateboard: deck leans into turns
        public Vector3[] wheelPoints;            // local ground contact points
        public Vector3 mountLocal;               // standing: where the feet go; sitting: where the hips go
        public bool sitting;
        public Transform visual;                 // child of the hull holding every renderer (the driver sees it at the predicted pose)

        public NetworkVariable<ulong> driverId = new NetworkVariable<ulong>(ulong.MaxValue);
        public NetworkVariable<float> steerAngle = new NetworkVariable<float>(0f);

        Rigidbody body;
        float throttle, steer, jump;
        float driveSpeed, heading, steerNow, jumpUntil;
        Vector3 groundNormal = Vector3.up;
        bool localRiding, mounted;
        int enteredFrame;
        Behaviour[] disabled;
        Coroutine hopRoutine;
        float spin, crankAngle;
        Vector3 lastVisualPos;
        bool testInput;
        VehiclePrediction pred = new VehiclePrediction();
        bool predicting;
        float sendTimer, inT, inS, inJ, serverSpeedEst;
        Transform V => visual != null ? visual : transform;
        AudioSource loopSound;

        public override string Prompt => driverId.Value == ulong.MaxValue ? label + " kullan" : label + " dolu";
        public float Heading => heading;
        public Vector3 VisualPosition => V.position;

        bool testDrive; float testT, testS;
        /// <summary>Automated tests (client side): drive with these inputs instead of the keyboard.</summary>
        public void TestDrive(float t, float s) { testDrive = true; testT = t; testS = s; }
        public float Speed => driveSpeed;

        void Awake() { body = GetComponent<Rigidbody>(); }

        public override void OnNetworkSpawn()
        {
            // a cart must hold its load: grippy bed; boards and bikes stay slick
            var slick = kind == RideKind.Cart
                ? new PhysicsMaterial("CartGrip") { dynamicFriction = 0.9f, staticFriction = 1.1f, frictionCombine = PhysicsMaterialCombine.Maximum, bounciness = 0f }
                : new PhysicsMaterial("RideSlick") { dynamicFriction = 0.05f, staticFriction = 0.05f, frictionCombine = PhysicsMaterialCombine.Minimum, bounciness = 0f };
            foreach (var c in GetComponentsInChildren<Collider>()) c.sharedMaterial = slick;
            loopSound = Audio.GameAudio.AttachLoop(gameObject, kind == RideKind.Skateboard ? Audio.LoopKind.SkateRoll : Audio.LoopKind.BikeTick);
            lastVisualPos = transform.position;
            if (IsServer)
            {
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                body.linearDamping = 0.6f;
                heading = transform.eulerAngles.y;
                var r = GetComponent<Resettable>();
                if (r != null) r.onReset = () => { driveSpeed = 0f; heading = transform.eulerAngles.y; groundNormal = Vector3.up; };
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
        void DriveRpc(float t, float s, float j, RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != driverId.Value || testInput) return;
            throttle = Mathf.Clamp(t, -1f, 1f);
            steer = Mathf.Clamp(s, -1f, 1f);
            if (kind == RideKind.Cart && j > 0.5f && jump <= 0.5f) ServerLoadToggle();   // Space: load what is close / tip the load off
            jump = j;
        }

        // ---------- cart: load and unload ----------
        public Vector3 bedSize = new Vector3(0.9f, 0.08f, 1.3f);   // bed box (local), centre height = bedY
        public float bedY = 0.4f, maxLoadKg = 70f;

        Vector3 BedCentre => transform.TransformPoint(new Vector3(0f, bedY + 0.05f, 0.15f));

        /// <summary>Server: Space on the cart. Items lying on the bed are tipped off to the side; otherwise the closest light props are lifted onto the bed.</summary>
        void ServerLoadToggle()
        {
            var onBed = new System.Collections.Generic.List<Rigidbody>();
            foreach (var c in Physics.OverlapBox(BedCentre + Vector3.up * 0.3f, new Vector3(bedSize.x * 0.5f, 0.4f, bedSize.z * 0.5f), transform.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb == body || rb.isKinematic || onBed.Contains(rb) || rb.GetComponent<Carryable>() == null) continue;
                onBed.Add(rb);
            }
            if (onBed.Count > 0)
            {
                foreach (var rb in onBed) rb.linearVelocity = transform.right * 2.4f + Vector3.up * 2.6f;
                return;
            }
            var near = new System.Collections.Generic.List<Rigidbody>();
            foreach (var c in Physics.OverlapSphere(transform.TransformPoint(0f, 0.5f, 0.3f), 2.8f, ~0, QueryTriggerInteraction.Ignore))
            {
                var rb = c.attachedRigidbody;
                if (rb == null || rb == body || rb.isKinematic || !rb.useGravity || near.Contains(rb) || rb.GetComponent<Carryable>() == null) continue;
                near.Add(rb);
            }
            near.Sort((a, b) => (a.position - transform.position).sqrMagnitude.CompareTo((b.position - transform.position).sqrMagnitude));
            var load = 0f; var n = 0;
            foreach (var rb in near)
            {
                if (n >= 3 || load + rb.mass > maxLoadKg) continue;
                var half = 0.3f;
                var cs = rb.GetComponentsInChildren<Collider>();
                if (cs.Length > 0) { var b = cs[0].bounds; foreach (var c in cs) b.Encapsulate(c.bounds); half = b.extents.y; }
                var slotZ = new[] { 0.15f, -0.2f, 0.5f }[n];
                var pos = transform.TransformPoint(new Vector3(0f, bedY + 0.05f + half + 0.03f + n * 0.01f, slotZ));
                rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero;
                rb.position = pos; rb.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                rb.transform.SetPositionAndRotation(pos, rb.rotation);
                load += rb.mass; n++;
            }
            if (n > 0) Physics.SyncTransforms();
            else CaseManager.Toast("Yanında yüklenecek hafif bir şey yok (en çok " + maxLoadKg.ToString("0") + " kg).");
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        void ExitRpc(RpcParams rpcParams = default)
        {
            if (rpcParams.Receive.SenderClientId != driverId.Value) return;
            throttle = steer = jump = 0f;
            driverId.Value = ulong.MaxValue;
        }

        public void RequestExit() => ExitRpc();

        public void ServerTestInput(float t, float s, float j) { testInput = true; throttle = t; steer = s; jump = j; }

        public void ServerTeleport(Vector3 pos, float yaw)
        {
            transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            body.position = pos; body.rotation = Quaternion.Euler(0f, yaw, 0f);
            body.linearVelocity = Vector3.zero;
            heading = yaw; driveSpeed = 0f; steerNow = 0f; groundNormal = Vector3.up;
            Physics.SyncTransforms();
        }

        int Probe(out Vector3 normal)
        {
            normal = Vector3.zero;
            var n = 0;
            foreach (var p in wheelPoints)
            {
                var o = transform.TransformPoint(p + Vector3.up * 0.5f);
                var hits = Physics.RaycastAll(o, Vector3.down, 0.95f, ~0, QueryTriggerInteraction.Ignore);
                var best = float.MaxValue; var bn = Vector3.up;
                foreach (var h in hits)
                {
                    if (h.collider.transform.IsChildOf(transform)) continue;
                    if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
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
            var driven = driverId.Value != ulong.MaxValue;
            body.useGravity = !driven;
            if (!driven)
            {
                // parked: plain physics, heading follows whatever pushed it
                driveSpeed = 0f; throttle = steer = jump = 0f;
                if (Mathf.Abs(steerAngle.Value) > 0.2f) steerAngle.Value = 0f;
                heading = transform.eulerAngles.y;
                return;
            }

            var touching = Probe(out var n);
            var grounded = touching >= 1 && Time.time > jumpUntil;
            groundNormal = Vector3.Slerp(groundNormal, grounded ? n : Vector3.up, 1f - Mathf.Exp(-9f * dt));
            if (groundNormal.y < 0.5f) groundNormal = Vector3.up;

            var target = throttle >= 0f ? throttle * maxSpeed : throttle * maxSpeed * 0.35f;
            driveSpeed = Mathf.MoveTowards(driveSpeed, target, (Mathf.Abs(target) > 0.01f ? accel : brake * 0.45f) * dt);

            steerNow = Mathf.MoveTowards(steerNow, steer * steerMax, 160f * dt);
            if (Mathf.Abs(steerAngle.Value - steerNow) > 0.2f) steerAngle.Value = steerNow;
            // a board/bike can turn on the spot a little: yaw rate has a floor so slow steering still works
            var turnSpeed = Mathf.Max(Mathf.Abs(driveSpeed), 1.2f) * Mathf.Sign(driveSpeed == 0f ? 1f : driveSpeed);
            var dYaw = Mathf.Rad2Deg * turnSpeed * Mathf.Tan(steerNow * Mathf.Deg2Rad) / wheelBase;
            var yawLim = Mathf.Lerp(150f, 65f, Mathf.Clamp01(Mathf.Abs(driveSpeed) / Mathf.Max(1f, maxSpeed)));   // same limit as VehiclePrediction: no spinning at speed
            heading += Mathf.Clamp(dYaw, -yawLim, yawLim) * dt;

            var rot = Quaternion.FromToRotation(Vector3.up, groundNormal) * Quaternion.Euler(0f, heading, 0f);
            body.MoveRotation(rot);
            body.angularVelocity = Vector3.zero;

            var fwd = rot * Vector3.forward;
            if (jump > 0.5f && grounded && kind == RideKind.Skateboard)
            {
                jumpUntil = Time.time + 0.35f;
                body.linearVelocity = fwd * driveSpeed + Vector3.up * jumpSpeed;
                grounded = false;
            }
            if (grounded) body.linearVelocity = fwd * driveSpeed - groundNormal * 1.0f;
            else
            {
                var v = body.linearVelocity;
                var flat = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized * driveSpeed;
                body.linearVelocity = new Vector3(flat.x, v.y - 9.81f * dt, flat.z);
            }
        }

        // ---------- visuals on every machine ----------
        void LateUpdate()
        {
            var dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var delta = transform.position - lastVisualPos;
            lastVisualPos = transform.position;
            var measured = Mathf.Clamp(Vector3.Dot(delta, transform.forward) / dt, -14f, 14f);
            serverSpeedEst = Mathf.Lerp(serverSpeedEst, measured, 1f - Mathf.Exp(-10f * dt));

            // the driver's machine shows its own predicted pose right away and eases toward the replicated one
            var fwdSpeed = measured;
            if (predicting)
            {
                pred.Reconcile(transform.position, transform.eulerAngles.y, serverSpeedEst, VehiclePrediction.Latency(), dt);
                var yawDelta = Mathf.DeltaAngle(transform.eulerAngles.y, pred.heading);
                V.SetPositionAndRotation(new Vector3(pred.pos.x, transform.position.y, pred.pos.z), Quaternion.AngleAxis(yawDelta, Vector3.up) * transform.rotation);
                fwdSpeed = pred.speed;
            }
            spin += fwdSpeed * dt;
            Audio.GameAudio.UpdateLoop(loopSound, driverId.Value != ulong.MaxValue, fwdSpeed, maxSpeed, kind == RideKind.Skateboard ? 0.22f : 0.14f, 1f);
            var steerDeg = predicting ? pred.steer : steerAngle.Value;

            if (spinPivots != null)
                for (var i = 0; i < spinPivots.Length; i++)
                {
                    var r = wheelRadius != null && i < wheelRadius.Length ? wheelRadius[i] : 0.3f;
                    if (spinPivots[i] != null) spinPivots[i].localRotation = Quaternion.Euler((spin / r) * Mathf.Rad2Deg, 0f, 0f);
                }
            if (steerPivots != null)
                foreach (var s in steerPivots) if (s != null) s.localRotation = Quaternion.Euler(0f, steerDeg, 0f);
            if (tiltPivot != null) tiltPivot.localRotation = Quaternion.Euler(0f, 0f, -steerDeg * 0.45f);

            // crank turns with the rear wheel (freewheel keeps it turning slowly when coasting)
            if (crank != null)
            {
                crankAngle += fwdSpeed * dt * 190f;           // degrees of crank per metre rolled
                crank.localRotation = Quaternion.Euler(crankAngle, 0f, 0f);
            }

            if (driverId.Value != ulong.MaxValue)
            {
                var rider = PlayerNet.ByOwner(driverId.Value);
                if (rider != null)
                {
                    rider.pedalAngle = crankAngle;       // legs follow the pedals
                    // other players: draw their body on the vehicle as we show it (their own transform arrives later than the vehicle's)
                    if (driverId.Value != NetworkManager.Singleton.LocalClientId)
                    {
                        if (rider.ride.Value != 0) RiderVisuals.Follow(rider, RiderRoot(rider) - Vector3.up, V.eulerAngles.y);
                        else RiderVisuals.Release(rider);
                    }
                }
            }

            if (localRiding && mounted && PlayerNet.Local != null)
                PlayerNet.Local.transform.SetPositionAndRotation(RiderRoot(PlayerNet.Local), Quaternion.Euler(0f, V.eulerAngles.y, 0f));
        }

        // ---------- local rider: hop on, ride, hop off ----------
        void OnDriverChanged(ulong previous, ulong current)
        {
            if (previous != ulong.MaxValue && previous != NetworkManager.Singleton.LocalClientId)
            {
                RiderVisuals.Release(PlayerNet.ByOwner(previous));
            }
            var me = NetworkManager.Singleton.LocalClientId;
            if (current == me && !localRiding) EnterLocal();
            else if (previous == me && current != me && localRiding) ExitLocal();
        }

        Vector3 RiderRoot(PlayerNet p)
        {
            var anim = p.GetComponentInChildren<CharacterAnimator>();
            var hip = anim != null ? anim.HipHeight : 0.86f;
            var point = V.TransformPoint(mountLocal);
            return sitting ? point - V.up * hip + Vector3.up * 1f : point + Vector3.up * 1f;
        }

        void EnterLocal()
        {
            var p = PlayerNet.Local;
            if (p == null) return;
            localRiding = true;
            mounted = false;
            enteredFrame = Time.frameCount;
            disabled = new Behaviour[] { p.GetComponent<PlayerController>(), p.GetComponent<ToolController>(), p.GetComponent<InteractionSystem>() };
            foreach (var b in disabled) if (b != null) b.enabled = false;
            p.GetComponent<CharacterController>().enabled = false;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(V, new Vector3(0f, 1.3f, 0f), 5f);
            hopRoutine = StartCoroutine(Hop(p));
            CaseManager.Toast(kind == RideKind.Cart ? label + ": W/S it-çek · A/D yön · Boşluk: yanındaki hafif şeyleri yükle / indir · E bırak" : label + ": W/S hız · A/D yön" + (kind == RideKind.Skateboard ? " · Boşluk zıpla" : "") + " · E in");
        }

        IEnumerator Hop(PlayerNet p)
        {
            var t = p.transform;
            var from = t.position;
            var d = 0.5f;
            for (var e = 0f; e < d; e += Time.deltaTime)
            {
                var k = Mathf.SmoothStep(0f, 1f, e / d);
                var pos = Vector3.Lerp(from, RiderRoot(p), k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.45f;
                t.SetPositionAndRotation(pos, Quaternion.Slerp(t.rotation, Quaternion.Euler(0f, V.eulerAngles.y, 0f), 12f * Time.deltaTime));
                yield return null;
            }
            p.ride.Value = (byte)kind;
            mounted = true;
            predicting = !IsServer;
            if (predicting) { pred.Begin(transform.position, transform.eulerAngles.y, serverSpeedEst); }
            hopRoutine = null;
        }

        void ExitLocal()
        {
            var p = PlayerNet.Local;
            localRiding = false;
            mounted = false;
            predicting = false;
            V.localPosition = Vector3.zero; V.localRotation = Quaternion.identity;
            if (hopRoutine != null) { StopCoroutine(hopRoutine); hopRoutine = null; }
            if (p == null) return;
            p.ride.Value = 0;
            p.transform.SetPositionAndRotation(transform.position + transform.right * 0.9f + Vector3.up * 1.1f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            Physics.SyncTransforms();
            foreach (var b in disabled) if (b != null) b.enabled = true;
            p.GetComponent<CharacterController>().enabled = true;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(p.transform, new Vector3(0f, 1.6f, 0f), ThirdPersonCamera.DistancePref);
        }

        void Update()
        {
            if (!localRiding) return;
            var kb = Keyboard.current;
            if (!testDrive && (kb == null || Net.PauseMenu.Open)) return;
            if (kb != null && kb.eKey.wasPressedThisFrame && Time.frameCount != enteredFrame && mounted) { ExitRpc(); return; }
            float t = 0f, s = 0f, j = 0f;
            if (mounted && testDrive) { t = testT; s = testS; }
            else if (mounted && kb != null)
            {
                if (kb.wKey.isPressed) t += 1f;
                if (kb.sKey.isPressed) t -= 1f;
                if (kb.dKey.isPressed) s += 1f;
                if (kb.aKey.isPressed) s -= 1f;
                if (kb.spaceKey.isPressed) j = 1f;
            }
            // predicted motion on the driver's machine (the server runs the same rules)
            if (predicting)
                pred.Step(Time.deltaTime, t, s, maxSpeed, accel, brake * 0.45f, 0.35f, steerMax, wheelBase, 1.2f);
            // send the input at 30 Hz (and at once when it changes), not once per rendered frame
            sendTimer += Time.deltaTime;
            var changed = !Mathf.Approximately(t, inT) || !Mathf.Approximately(s, inS) || !Mathf.Approximately(j, inJ);
            if (changed || sendTimer >= 1f / 30f)
            {
                sendTimer = 0f; inT = t; inS = s; inJ = j;
                DriveRpc(t, s, j);
            }
        }
    }
}
