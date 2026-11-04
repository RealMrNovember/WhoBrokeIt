using UnityEngine;
using UnityEngine.InputSystem;

namespace WhoBrokeIt.Player
{
    /// <summary>Third-person walk/run/jump + pushing physics props. P0: single player, local input.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public Transform cameraTransform;
        public float walkSpeed = 4.5f;
        public float runSpeed = 7f;
        public float jumpHeight = 1.2f;
        public float gravity = -22f;
        public float turnSharpness = 12f;
        public float pushPower = 8f;

        CharacterController controller;
        float verticalVelocity;
        Vector3 knock;
        float stunUntil;
        GUIStyle dazedStyle;

        public bool Stunned => Time.time < stunUntil;

        void Awake() => controller = GetComponent<CharacterController>();

        /// <summary>Called on the owning client when something hits this player: knock back and lose control for a moment.</summary>
        public void ApplyBonk(Vector3 impulse, float stunSeconds)
        {
            ThirdPersonCamera.AddTrauma(0.75f);
            knock = new Vector3(impulse.x, 0f, impulse.z);
            verticalVelocity = Mathf.Max(verticalVelocity, impulse.y);
            stunUntil = Mathf.Max(stunUntil, Time.time + stunSeconds);
        }

        void OnGUI()
        {
            if (!Stunned) return;
            if (dazedStyle == null)
                dazedStyle = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            dazedStyle.normal.textColor = new Color(1f, 0.5f, 0.45f);
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 70), "BONK! Sersemledin...", dazedStyle);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            var input = Vector2.zero;
            if (!Stunned && !Net.PauseMenu.Open)
            {
                if (kb.wKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed) input.y -= 1f;
                if (kb.dKey.isPressed) input.x += 1f;
                if (kb.aKey.isPressed) input.x -= 1f;
            }

            var forward = cameraTransform != null
                ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
                : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var move = forward * input.y + right * input.x;
            if (move.sqrMagnitude > 1f) move.Normalize();

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (controller.isGrounded && !Stunned && !Net.PauseMenu.Open && kb.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity += gravity * Time.deltaTime;

            var speed = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;
            controller.Move((move * speed + knock + Vector3.up * verticalVelocity) * Time.deltaTime);
            knock = Vector3.MoveTowards(knock, Vector3.zero, 16f * Time.deltaTime);

            var cam = ThirdPersonCamera.Instance;
            if (cam != null && cam.IsFirstPerson)
                transform.rotation = Quaternion.Euler(0f, cam.Yaw, 0f);        // first person: the body always faces where you look
            else if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSharpness * Time.deltaTime);
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic || hit.moveDirection.y < -0.3f) return;
            var dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            body.AddForceAtPosition(dir * pushPower, hit.point, ForceMode.Force);
        }
    }
}
