using UnityEngine;
using UnityEngine.InputSystem;

namespace WhoBrokeIt.Player
{
    /// <summary>Mouse-orbit follow camera with simple collision (static geometry only). Esc frees the cursor.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public static ThirdPersonCamera Instance { get; private set; }

        public Transform target;
        public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
        public float distance = 5.5f;
        public float minDistance = 2f, maxDistance = 12f;
        public float sensitivity = 0.12f;
        public float minPitch = -20f, maxPitch = 70f;

        float yaw, pitch = 20f;

        void Awake() { Instance = this; }

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                var locked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = locked;
            }
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var d = mouse.delta.ReadValue();
                yaw += d.x * sensitivity;
                pitch = Mathf.Clamp(pitch - d.y * sensitivity, minPitch, maxPitch);
                distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * 0.002f, minDistance, maxDistance);
            }

            var pivot = target.position + pivotOffset;
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var dir = rot * Vector3.back;
            var dist = distance;
            if (Physics.SphereCast(pivot, 0.25f, dir, out var hit, distance, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.attachedRigidbody == null)
                dist = Mathf.Max(0.6f, hit.distance);
            transform.SetPositionAndRotation(pivot + dir * dist, rot);
        }
    }
}
