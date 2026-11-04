using UnityEngine;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Procedural animation for any character: walk cycle (limbs swing, body bobs and leans), carrying pose (arms forward) and a dizzy
    /// wobble while stunned. Works from the player's movement, so remote players animate without any extra network data.
    /// Custom models can leave the limb references empty and still get bob/lean/wobble.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public Transform legL, legR, armL, armR;
        public float swingDegrees = 38f;

        PlayerNet net;
        Transform follow;
        Vector3 lastPos;
        float speed, phase;
        Vector3 basePos;
        Quaternion baseRot;

        void Start()
        {
            net = GetComponentInParent<PlayerNet>();
            follow = net != null ? net.transform : transform.parent;
            lastPos = follow != null ? follow.position : Vector3.zero;
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
        }

        void LateUpdate()
        {
            if (follow == null) return;
            var delta = follow.position - lastPos;
            delta.y = 0f;
            lastPos = follow.position;
            var v = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            speed = Mathf.Lerp(speed, Mathf.Min(v, 12f), 12f * Time.deltaTime);

            var moving = Mathf.Clamp01(speed / 4f);
            phase += speed * 1.5f * Time.deltaTime;

            var stunned = net != null && net.stunned.Value;
            var carrying = net != null && net.carry != null && net.carry.IsCarrying;

            // body: bob while walking, tiny breathing when idle, lean into the run, wobble when dazed
            var bob = Mathf.Abs(Mathf.Sin(phase * 2f)) * 0.05f * moving + Mathf.Sin(Time.time * 2f) * 0.004f * (1f - moving);
            transform.localPosition = basePos + Vector3.up * bob;
            var wobble = stunned ? Mathf.Sin(Time.time * 18f) * 16f : 0f;
            transform.localRotation = baseRot * Quaternion.Euler(moving * 7f, 0f, wobble);

            var swing = Mathf.Sin(phase) * swingDegrees * moving;
            if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0f, 0f);

            if (armL != null && armR != null)
            {
                if (carrying)
                {
                    armL.localRotation = Quaternion.Euler(-80f, 0f, 8f);
                    armR.localRotation = Quaternion.Euler(-80f, 0f, -8f);
                }
                else if (stunned)
                {
                    var flail = Mathf.Sin(Time.time * 20f) * 40f;
                    armL.localRotation = Quaternion.Euler(-30f + flail, 0f, 35f);
                    armR.localRotation = Quaternion.Euler(-30f - flail, 0f, -35f);
                }
                else
                {
                    armL.localRotation = Quaternion.Euler(-swing * 0.9f, 0f, 4f);
                    armR.localRotation = Quaternion.Euler(swing * 0.9f, 0f, -4f);
                }
            }
        }
    }
}
