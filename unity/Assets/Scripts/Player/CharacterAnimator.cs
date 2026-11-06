using UnityEngine;

namespace WhoBrokeIt.Player
{
    /// <summary>
    /// Procedural animation for every character (no animation clips): idle breathing and sway, walk/run cycle with swinging limbs, body bob and
    /// lean, jump pose in the air, carrying and tool poses, throw and hit swings, dizzy flailing, climbing and sitting on a vehicle.
    /// Everything is derived from the player's movement and a few synced flags, so remote players animate without extra network traffic.
    /// Works on limb pivots (primitive characters) or on the small skeleton built for the generated models (hips + two legs + two arms).
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public Transform hips, legL, legR, armL, armR;
        public float swingDegrees = 36f;
        public float armSwingDegrees = 32f;

        /// <summary>Editor/screenshot tool: freezes a pose (0 none, 1 walk, 2 sit, 3 jump, 4 climb, 5 throw, 6 carry).</summary>
        public static int DebugPose;
        public static float DebugPhase;

        PlayerNet net;
        Transform follow, feet;
        Vector3 lastPos;
        float speed, phase;
        Vector3 hipsBasePos;
        Quaternion hipsBaseRot;
        Vector3 hipsBaseScale;
        int lastTick;
        float actionStart = -10f;
        int actionKind;
        bool air;
        float hipHeight = 0.86f;

        /// <summary>Height of the hips above the feet (the seat position of a vehicle is derived from it).</summary>
        public float HipHeight => hipHeight;

        // smoothed pose values (degrees / metres)
        float lLx, lRx, aLx, aRx, aLz, aRz, bob, lean, twist, roll, sink;
        float speedForAcc, accSm, vySm, yawRateSm, lastYaw, pushPhase;
        bool wasStunned, wasMounted;
        float lastStepSin;

        Transform Body => hips != null ? hips : transform;

        void Start()
        {
            net = GetComponentInParent<PlayerNet>();
            follow = net != null ? net.transform : transform.parent;
            feet = net != null && net.visualAnchor != null ? net.visualAnchor : transform;
            lastPos = follow != null ? follow.position : Vector3.zero;
            hipsBasePos = Body.localPosition;
            hipsBaseRot = Body.localRotation;
            hipsBaseScale = Body.localScale;
            if (hips != null) hipHeight = hips.localPosition.y;
            lastTick = net != null ? net.actionTick.Value : 0;
        }

        static float Sm(float current, float target, float k) { return Mathf.Lerp(current, target, 1f - Mathf.Exp(-k * Time.deltaTime)); }

        /// <summary>Stance for riding: kind 1 = skateboard (side-on, knees soft, pushes off the ground now and then), 2 = bicycle (sitting, legs follow the pedals).</summary>
        void RidePose(int kind, ref float tLL, ref float tLR, ref float tAL, ref float tAR, ref float tAZL, ref float tAZR, ref float tBob, ref float tLean, ref float tTwist, ref float tRoll, ref float tSink, float dt, float debugPush = -1f)
        {
            if (kind == 3)                                                       // hand cart: walking behind it, both hands on the handle
            {
                tAL = -58f; tAR = -58f; tAZL = 6f; tAZR = -6f; tLean = 7f;
                return;
            }
            tRoll -= Mathf.Clamp(yawRateSm * 0.10f, -14f, 14f);               // lean into the turn
            if (kind == 1)
            {
                var tuck = Mathf.Abs(vySm) > 1.2f;
                var wantPush = debugPush < 0f ? (accSm > 0.35f || (speed < 0.6f && accSm > 0.05f)) && speed < 6.5f : debugPush > 0f;
                if (debugPush < 0f) { if (wantPush) pushPhase += dt * 1.45f; else pushPhase = Mathf.Ceil(pushPhase - 0.0001f); }
                var u = debugPush > 0f ? 0.3f : Mathf.Repeat(pushPhase, 1f);
                tTwist = 28f; tLean = 9f; tSink = -0.07f; tBob = 0f;
                tLL = -12f; tLR = 10f;                                                  // front foot forward, back foot behind
                tAZL = 30f; tAZR = -30f; tAL = -22f; tAR = -14f;                         // arms out for balance
                if (u < 0.6f && (wantPush || debugPush > 0f))               // push: back foot swings down to the ground and kicks
                {
                    var k = u / 0.6f;
                    tLR = Mathf.Lerp(10f, 62f, Mathf.Sin(k * Mathf.PI * 0.5f)) * (k < 0.8f ? 1f : Mathf.Lerp(1f, 0.2f, (k - 0.8f) / 0.2f));
                    tLean = 18f; tSink = -0.12f; tTwist = 12f; tAL = -50f; tAR = 28f;
                }
                if (tuck) { tLL = -34f; tLR = -34f; tSink = -0.16f; tAL = -35f; tAR = -35f; }
            }
            else
            {
                var a = (debugPush >= 0f ? 40f : net != null ? net.pedalAngle : 0f) * Mathf.Deg2Rad;
                tLL = -42f + Mathf.Sin(a) * 38f; tLR = -42f - Mathf.Sin(a) * 38f;     // thighs follow the pedals
                tAL = -60f; tAR = -60f; tAZL = 12f; tAZR = -12f;                      // hands on the handlebar
                tLean = 16f; tBob = 0f; tSink = 0f; tTwist = Mathf.Clamp(yawRateSm * 0.06f, -8f, 8f);
            }
        }

        void LateUpdate()
        {
            if (follow == null) return;
            var dt = Mathf.Max(Time.deltaTime, 0.0001f);
            var delta = follow.position - lastPos;
            lastPos = follow.position;
            var flat = new Vector3(delta.x, 0f, delta.z);
            speed = Mathf.Lerp(speed, Mathf.Min(flat.magnitude / dt, 12f), 1f - Mathf.Exp(-14f * dt));
            var prevSpeed = speedForAcc; speedForAcc = speed;
            accSm = Mathf.Lerp(accSm, (speed - prevSpeed) / dt, 1f - Mathf.Exp(-6f * dt));
            vySm = Mathf.Lerp(vySm, delta.y / dt, 1f - Mathf.Exp(-12f * dt));
            var yawNow = follow.eulerAngles.y;
            yawRateSm = Mathf.Lerp(yawRateSm, Mathf.DeltaAngle(lastYaw, yawNow) / dt, 1f - Mathf.Exp(-10f * dt));
            lastYaw = yawNow;

            if (net != null && net.actionTick.Value != lastTick)
            {
                lastTick = net.actionTick.Value;
                actionStart = Time.time;
                actionKind = lastTick & 1;
                Audio.GameAudio.Play(actionKind == 0 ? Audio.Sfx.Whoosh : Audio.Sfx.Swing, follow.position + Vector3.up, 0.9f, Random.Range(0.92f, 1.08f));
            }

            var stunned = net != null && net.stunned.Value;
            var carrying = net != null && net.carry != null && net.carry.IsCarrying;
            var seated = net != null && net.seated.Value;
            var climbing = net != null && net.climbing.Value;
            var tool = net != null ? net.toolId.Value : 0;
            // sounds from the same state the animation reads (so other players are heard too)
            if (stunned && !wasStunned) Audio.GameAudio.Play(Audio.Sfx.Bonk, follow.position + Vector3.up, 1f, Random.Range(0.95f, 1.05f));
            wasStunned = stunned;
            var mountedNow = seated || (net != null && net.ride.Value != 0);
            if (mountedNow && !wasMounted) Audio.GameAudio.Play(Audio.Sfx.Mount, follow.position, 0.8f);
            wasMounted = mountedNow;
            air = !Physics.Raycast(feet.position + Vector3.up * 0.15f, Vector3.down, 0.45f, ~0, QueryTriggerInteraction.Ignore);

            var moving = Mathf.Clamp01(speed / 2.5f);
            var run = Mathf.Clamp01((speed - 4.5f) / 3f);
            phase += (speed * 1.25f + (climbing ? 5.5f : 0f)) * dt;
            var swing = Mathf.Sin(phase) * swingDegrees * moving;
            // footsteps: every time the leg swing passes the middle while walking on the ground
            var stepSin = Mathf.Sin(phase);
            if (moving > 0.4f && !air && !mountedNow && !climbing && (stepSin > 0f) != (lastStepSin > 0f))
                Audio.GameAudio.Play(Audio.Sfx.Step, feet.position, 0.55f + 0.25f * run, Random.Range(0.9f, 1.1f));
            lastStepSin = stepSin;
            var armSwing = Mathf.Sin(phase) * armSwingDegrees * moving;

            // ---- targets ----
            float tLL = swing, tLR = -swing, tAL = -armSwing, tAR = armSwing, tAZL = 4f, tAZR = -4f;
            float tBob = -Mathf.Abs(Mathf.Sin(phase)) * 0.035f * moving;
            float tLean = moving * 4f + run * 6f;
            float tTwist = Mathf.Sin(phase) * 6f * moving;
            float tRoll = Mathf.Cos(phase) * 2f * moving;
            float tSink = 0f;

            // idle: slow breathing, weight shift, loosely swinging arms
            if (moving < 0.2f)
            {
                var t = Time.time;
                tRoll += Mathf.Sin(t * 0.9f) * 1.8f;
                tTwist += Mathf.Sin(t * 0.6f + 1f) * 3f;
                tAL += Mathf.Sin(t * 1.3f) * 3f;
                tAR += Mathf.Sin(t * 1.3f + 1.7f) * 3f;
                tBob += Mathf.Sin(t * 2.2f) * 0.006f;
            }

            if (tool != 0 && !carrying)
            {
                tAR = -55f + Mathf.Sin(Time.time * 4f) * 4f * moving;
                tAL = -35f;
            }
            if (carrying)
            {
                tAL = -80f; tAR = -80f; tAZL = 8f; tAZR = -8f;
                tLean += 2f;
            }

            if (air)
            {
                tLL = -38f; tLR = 22f;
                tAL = -25f; tAR = -25f; tAZL = 24f; tAZR = -24f;
                tBob = 0.02f; tLean = 4f;
            }

            var since = Time.time - actionStart;
            if (since < 0.5f)
            {
                var u = Mathf.Clamp01(since / 0.5f);
                if (actionKind == 0)          // throw: both arms wind back then fling forward
                {
                    var a = u < 0.35f ? Mathf.Lerp(-30f, -125f, u / 0.35f) : Mathf.Lerp(-125f, 35f, (u - 0.35f) / 0.65f);
                    tAL = a; tAR = a; tAZL = 10f; tAZR = -10f; tLean = u < 0.35f ? -6f : 14f;
                }
                else                          // hit: right arm winds up and chops down, body twists
                {
                    var a = u < 0.3f ? Mathf.Lerp(-30f, -120f, u / 0.3f) : Mathf.Lerp(-120f, 45f, (u - 0.3f) / 0.7f);
                    tAR = a; tTwist = u < 0.3f ? 14f : -22f; tLean = u < 0.3f ? -4f : 12f;
                }
            }

            if (stunned)
            {
                var flail = Mathf.Sin(Time.time * 20f) * 40f;
                tAL = -30f + flail; tAR = -30f - flail; tAZL = 35f; tAZR = -35f;
                tLL = Mathf.Sin(Time.time * 9f) * 14f; tLR = -tLL;
                tRoll = Mathf.Sin(Time.time * 18f) * 14f;
            }

            if (climbing)                     // reaching up and stepping with high knees
            {
                tAL = -115f + Mathf.Sin(phase) * 22f; tAR = -115f - Mathf.Sin(phase) * 22f; tAZL = 8f; tAZR = -8f;
                tLL = -55f + Mathf.Sin(phase) * 30f; tLR = -55f - Mathf.Sin(phase) * 30f;
                tLean = 8f; tBob = 0f; tTwist = 0f;
            }
            else if (seated)                  // hands on the wheel, legs forward
            {
                tLL = -88f; tLR = -88f; tAL = -62f; tAR = -62f; tAZL = 10f; tAZR = -10f;
                tBob = 0f; tLean = -4f; tTwist = 0f; tRoll = 0f; tSink = -0.05f;
            }

            // emotes (owner-chosen, replicated as one byte): 1 wave, 2 point, 3 cheer, 4 dance
            var emote = net != null ? net.emote.Value : 0;
            if (emote != 0 && !seated && !climbing && !stunned && net.ride.Value == 0 && !carrying)
            {
                var te = Time.time;
                switch (emote)
                {
                    case 1: tAR = -168f; tAZR = -18f + Mathf.Sin(te * 11f) * 24f; tAL = -armSwing * 0.3f; tLean = 2f; break;
                    case 2: tAR = -88f; tAZR = -4f; tAL = 6f; tLean = 5f; tTwist = -8f; break;
                    case 3: tAL = -172f; tAR = -172f; tAZL = 22f + Mathf.Sin(te * 9f) * 8f; tAZR = -22f - Mathf.Sin(te * 9f) * 8f; tBob = Mathf.Abs(Mathf.Sin(te * 8f)) * 0.09f; tLL = Mathf.Sin(te * 8f) * 10f; tLR = -tLL; break;
                    case 4: tAL = -95f + Mathf.Sin(te * 6.5f) * 70f; tAR = -95f - Mathf.Sin(te * 6.5f) * 70f; tAZL = 20f; tAZR = -20f; tTwist = Mathf.Sin(te * 3.2f) * 22f; tRoll = Mathf.Sin(te * 6.5f) * 8f; tBob = -Mathf.Abs(Mathf.Sin(te * 6.5f)) * 0.06f; tLL = Mathf.Sin(te * 6.5f) * 22f; tLR = -tLL; break;
                }
            }

            var ride = net != null ? net.ride.Value : 0;
            if (ride != 0 && !climbing && !seated) RidePose(ride, ref tLL, ref tLR, ref tAL, ref tAR, ref tAZL, ref tAZR, ref tBob, ref tLean, ref tTwist, ref tRoll, ref tSink, dt);

            switch (DebugPose)
            {
                case 1: tLL = Mathf.Sin(DebugPhase) * swingDegrees; tLR = -tLL; tAL = -Mathf.Sin(DebugPhase) * armSwingDegrees; tAR = -tAL; tLean = 5f; tBob = -Mathf.Abs(Mathf.Sin(DebugPhase)) * 0.035f; break;
                case 2: tLL = -88f; tLR = -88f; tAL = -62f; tAR = -62f; tLean = -4f; break;
                case 3: tLL = -38f; tLR = 22f; tAL = -25f; tAR = -25f; tAZL = 24f; tAZR = -24f; break;
                case 4: tAL = -115f; tAR = -115f; tLL = -55f; tLR = -20f; tLean = 8f; break;
                case 5: tAL = -125f; tAR = -125f; tLean = -6f; break;
                case 6: tAL = -80f; tAR = -80f; break;
                case 7: RidePose(1, ref tLL, ref tLR, ref tAL, ref tAR, ref tAZL, ref tAZR, ref tBob, ref tLean, ref tTwist, ref tRoll, ref tSink, 0f, 0f); break;
                case 8: RidePose(1, ref tLL, ref tLR, ref tAL, ref tAR, ref tAZL, ref tAZR, ref tBob, ref tLean, ref tTwist, ref tRoll, ref tSink, 0f, 0.3f); break;
                case 9: RidePose(2, ref tLL, ref tLR, ref tAL, ref tAR, ref tAZL, ref tAZR, ref tBob, ref tLean, ref tTwist, ref tRoll, ref tSink, 0f, 0f); break;
            }

            // ---- apply (smoothed) ----
            var k = DebugPose != 0 ? 80f : 16f;
            lLx = Sm(lLx, tLL, k); lRx = Sm(lRx, tLR, k);
            aLx = Sm(aLx, tAL, k); aRx = Sm(aRx, tAR, k);
            aLz = Sm(aLz, tAZL, k); aRz = Sm(aRz, tAZR, k);
            bob = Sm(bob, tBob, 20f); lean = Sm(lean, tLean, 10f); twist = Sm(twist, tTwist, 12f); roll = Sm(roll, tRoll, 12f); sink = Sm(sink, tSink, 10f);

            var body = Body;
            body.localPosition = hipsBasePos + Vector3.up * (bob + sink);
            body.localRotation = hipsBaseRot * Quaternion.Euler(lean, twist, roll);
            var breathe = 1f + Mathf.Sin(Time.time * 2.2f) * 0.01f * (1f - moving);
            body.localScale = new Vector3(hipsBaseScale.x, hipsBaseScale.y * breathe, hipsBaseScale.z);

            if (legL != null) legL.localRotation = Quaternion.Euler(lLx, 0f, 0f);
            if (legR != null) legR.localRotation = Quaternion.Euler(lRx, 0f, 0f);
            if (armL != null) armL.localRotation = Quaternion.Euler(aLx, 0f, aLz);
            if (armR != null) armR.localRotation = Quaternion.Euler(aRx, 0f, aRz);
        }
    }
}
