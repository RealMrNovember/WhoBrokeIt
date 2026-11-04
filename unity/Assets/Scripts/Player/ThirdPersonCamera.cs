using UnityEngine;
using UnityEngine.InputSystem;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Follow camera: third person (orbit with collision) or first person (V toggles, the choice is remembered).
    /// While driving a vehicle the camera is always third person. Esc frees the cursor.
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        const string PrefKey = "wbi_firstperson";

        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        public float distance = 5.5f;
        public float minDistance = 2f, maxDistance = 12f;
        public float sensitivity = 0.12f;
        public float minPitch = -20f, maxPitch = 70f;
        public Vector3 headOffset = new Vector3(0f, 0.68f, 0f);

        Transform playerTarget;
        float yaw, pitch = 20f;

        /// <summary>The player's own choice; applies whenever the camera follows the player (not a vehicle).</summary>
        public static bool FirstPersonPreferred
        {
            get { return PlayerPrefs.GetInt(PrefKey, 0) == 1; }
            set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // player-tunable camera settings (saved, edited in the pause menu)
        public static float Sensitivity { get { return PlayerPrefs.GetFloat("wbi_sens", 0.12f); } set { PlayerPrefs.SetFloat("wbi_sens", value); } }
        public static float Fov { get { return PlayerPrefs.GetFloat("wbi_fov", 70f); } set { PlayerPrefs.SetFloat("wbi_fov", value); } }
        public static float HeightAdjust { get { return PlayerPrefs.GetFloat("wbi_camh", 0f); } set { PlayerPrefs.SetFloat("wbi_camh", value); } }
        public static float ShoulderAdjust { get { return PlayerPrefs.GetFloat("wbi_cams", 0f); } set { PlayerPrefs.SetFloat("wbi_cams", value); } }
        public static float DistancePref { get { return PlayerPrefs.GetFloat("wbi_camd", 5.5f); } set { PlayerPrefs.SetFloat("wbi_camd", value); } }

        public bool IsFirstPerson => FirstPersonPreferred && target != null && target == playerTarget;
        public float Yaw => yaw;
        public bool OnPlayer => target != null && target == playerTarget;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
        /// <summary>Current follow distance (third person, on foot); also remembered when changed.</summary>
        public float Distance { get { return distance; } set { distance = Mathf.Clamp(value, minDistance, maxDistance); } }

        void Awake() { Instance = this; }

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Follow the local player (camera mode follows the player's preference).</summary>
        public void SetPlayer(Transform player)
        {
            playerTarget = player;
            target = player;
            pivotOffset = new Vector3(0f, 1.6f, 0f);
            distance = Mathf.Clamp(DistancePref, minDistance, maxDistance);
        }

        public void SetTarget(Transform t, Vector3 offset, float dist)
        {
            target = t;
            pivotOffset = offset;
            distance = dist;
        }

        void LateUpdate()
        {
            if (target == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.vKey.wasPressedThisFrame && target == playerTarget && !Net.PauseMenu.Open)
                FirstPersonPreferred = !FirstPersonPreferred;

            var cam = GetComponent<Camera>();
            if (cam != null) cam.fieldOfView = Fov;

            var fp = IsFirstPerson;
            var lo = fp ? -85f : minPitch;
            var hi = fp ? 85f : maxPitch;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var d = mouse.delta.ReadValue();
                var sens = Sensitivity;
                yaw += d.x * sens;
                pitch = Mathf.Clamp(pitch - d.y * sens, lo, hi);
                if (!fp)
                {
                    distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * 0.002f, minDistance, maxDistance);
                    if (target == playerTarget) DistancePref = distance;
                }
            }
            // keyboard camera tilt: [ and ] (also works when the mouse feels too touchy)
            if (kb != null && !Net.PauseMenu.Open)
            {
                if (kb.leftBracketKey.isPressed) pitch -= 40f * Time.deltaTime;
                if (kb.rightBracketKey.isPressed) pitch += 40f * Time.deltaTime;
            }
            pitch = Mathf.Clamp(pitch, lo, hi);

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            if (fp)
            {
                var flat = Quaternion.Euler(0f, yaw, 0f);
                transform.SetPositionAndRotation(target.position + headOffset + flat * new Vector3(0f, 0f, 0.14f), rot);
                return;
            }

            var pivot = target.position + pivotOffset;
            if (target == playerTarget) pivot += Vector3.up * HeightAdjust + Quaternion.Euler(0f, yaw, 0f) * Vector3.right * ShoulderAdjust;
            var dir = rot * Vector3.back;
            var dist = distance;
            if (Physics.SphereCast(pivot, 0.25f, dir, out var hit, distance, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.attachedRigidbody == null)
                dist = Mathf.Max(0.6f, hit.distance);
            transform.SetPositionAndRotation(pivot + dir * dist, rot);
        }
    }
}
