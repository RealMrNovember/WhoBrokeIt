using UnityEngine;
using UnityEngine.InputSystem;
using WhoBrokeIt.Case;
using WhoBrokeIt.Interaction;
using WhoBrokeIt.Player;
using WhoBrokeIt.Props;

namespace WhoBrokeIt.Vehicles
{
    /// <summary>Arcade forklift: W/S drive, A/D steer, R/F fork up/down, E exit, hold X reset. Forks are a kinematic body that carries loads by friction.</summary>
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

        Rigidbody body;
        GameObject driver;
        bool driven;
        float forkHeight;
        float input, steer, forkInput;
        Behaviour[] disabledBehaviours;
        Renderer[] driverRenderers;
        GUIStyle style;

        public override string Prompt => driven ? "Çık" : "Forklifte bin";

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.centerOfMass = new Vector3(0f, -0.4f, 0.3f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (forks != null) forks.interpolation = RigidbodyInterpolation.Interpolate;
            forkHeight = forkMin;
            var r = GetComponent<Resettable>();
            if (r != null) r.onReset = () => { forkHeight = forkMin; MoveForks(); };
        }

        public override void Interact(GameObject user)
        {
            if (!driven) Enter(user);
        }

        void Enter(GameObject user)
        {
            driver = user;
            driven = true;
            var carry = user.GetComponent<CarrySystem>();
            if (carry != null) carry.Drop();
            disabledBehaviours = new Behaviour[]
            {
                user.GetComponent<PlayerController>(), user.GetComponent<ToolController>(),
                user.GetComponent<InteractionSystem>()
            };
            var ccEnter = user.GetComponent<CharacterController>();
            if (ccEnter != null) ccEnter.enabled = false;
            enteredFrame = Time.frameCount;
            foreach (var b in disabledBehaviours) if (b != null) b.enabled = false;
            driverRenderers = user.GetComponentsInChildren<Renderer>();
            foreach (var r in driverRenderers) r.enabled = false;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(transform, new Vector3(0f, 1.8f, 0f), 8f);
            CaseManager.Toast("Forklift: W/S sür · A/D dön · R/F çatalı kaldır/indir · E çık", 4f);
        }

        void Exit()
        {
            driven = false;
            var cc = driver.GetComponent<CharacterController>();
            driver.transform.position = transform.position + transform.right * 2.4f + Vector3.up * 0.5f;
            Physics.SyncTransforms();
            foreach (var b in disabledBehaviours) if (b != null) b.enabled = true;
            foreach (var r in driverRenderers) r.enabled = true;
            if (cc != null) cc.enabled = true;
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.SetTarget(driver.transform, new Vector3(0f, 1.6f, 0f), 5.5f);
            driver = null;
        }

        void Update()
        {
            input = steer = forkInput = 0f;
            if (!driven) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.eKey.wasPressedThisFrame && Time.frameCount != enteredFrame) { Exit(); return; }
            if (kb.wKey.isPressed) input += 1f;
            if (kb.sKey.isPressed) input -= 1f;
            if (kb.dKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed) steer -= 1f;
            if (kb.rKey.isPressed) forkInput += 1f;
            if (kb.fKey.isPressed) forkInput -= 1f;
            if (kb.xKey.isPressed)
            {
                resetHold += Time.deltaTime;
                if (resetHold > 1f)
                {
                    resetHold = 0f;
                    var r = GetComponent<Resettable>();
                    if (r != null) { r.ResetNow(); CaseManager.NoteReset(); CaseManager.Toast("Forklift sıfırlandı.", 2f); }
                }
            }
            else resetHold = 0f;
        }

        float resetHold;
        int enteredFrame;

        void FixedUpdate()
        {
            var fwd = transform.forward;
            fwd.y = 0f;
            fwd.Normalize();
            var v = body.linearVelocity;
            var speed = Vector3.Dot(v, fwd);
            var target = input * maxSpeed;
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

        void OnGUI()
        {
            if (!driven) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.8f, Screen.width, 36),
                "Çatal yüksekliği: " + forkHeight.ToString("0.0") + " m   (R yukarı · F aşağı · E çık)", style);
        }
    }
}
