using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Vehicles
{
    /// <summary>
    /// Client-side prediction for the vehicle the local player drives. The server stays the authority, but the driver's machine runs the same
    /// arcade kinematics on the vehicle's visual root straight from the keyboard, so the vehicle reacts at once instead of one round trip later,
    /// and eases back toward the (delayed) server pose.
    /// </summary>
    public class VehiclePrediction
    {
        public Vector3 pos;
        public float heading, speed, steer;
        float serverYawPrev, serverYawRate;

        public void Begin(Vector3 p, float h, float s)
        {
            pos = p; heading = h; speed = s; steer = 0f;
            serverYawPrev = h; serverYawRate = 0f;
        }

        /// <summary>Same speed / steering rules as the server simulation.</summary>
        public void Step(float dt, float throttle, float steerInput, float maxSpeed, float accel, float brake, float reverseScale, float steerMax, float wheelBase, float minTurnSpeed)
        {
            var target = throttle >= 0f ? throttle * maxSpeed : throttle * maxSpeed * reverseScale;
            speed = Mathf.MoveTowards(speed, target, (Mathf.Abs(target) > 0.01f ? accel : brake) * dt);
            steer = Mathf.MoveTowards(steer, steerInput * steerMax, 150f * dt);
            var turnSpeed = minTurnSpeed > 0f ? Mathf.Max(Mathf.Abs(speed), minTurnSpeed) * (speed < 0f ? -1f : 1f) : speed;
            var dYaw = Mathf.Rad2Deg * turnSpeed * Mathf.Tan(steer * Mathf.Deg2Rad) / wheelBase;
            if (minTurnSpeed > 0f) { var lim = Mathf.Lerp(150f, 65f, Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(1f, maxSpeed))); dYaw = Mathf.Clamp(dYaw, -lim, lim); }   // boards and bikes: no spinning at speed
            heading += dYaw * dt;
            pos += Quaternion.Euler(0f, heading, 0f) * Vector3.forward * (speed * dt);
        }

        /// <summary>Ease toward where the server pose will be by the time it arrives (pose + velocity x latency).</summary>
        public void Reconcile(Vector3 serverPos, float serverYaw, float serverSpeed, float latency, float dt)
        {
            var yr = Mathf.DeltaAngle(serverYawPrev, serverYaw) / Mathf.Max(dt, 0.0001f);
            serverYawPrev = serverYaw;
            serverYawRate = Mathf.Lerp(serverYawRate, yr, 1f - Mathf.Exp(-8f * dt));
            var target = serverPos + Quaternion.Euler(0f, serverYaw, 0f) * Vector3.forward * (serverSpeed * latency);
            var err = target - pos; err.y = 0f;
            if (err.magnitude > 4f)                       // blocked or teleported (reset): snap
            {
                pos.x = target.x; pos.z = target.z; heading = serverYaw; speed = serverSpeed;
                return;
            }
            pos += err * (1f - Mathf.Exp(-3f * dt));
            heading += Mathf.DeltaAngle(heading, serverYaw + serverYawRate * latency) * (1f - Mathf.Exp(-2.5f * dt));
        }

        /// <summary>One-way latency estimate in seconds (half the round trip, plus the interpolation delay of the replicated pose).</summary>
        public static float Latency()
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null || nm.IsServer) return 0f;
            var ut = nm.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
            var rtt = ut != null ? ut.GetCurrentRtt(Unity.Netcode.NetworkManager.ServerClientId) : 100UL;
            return Mathf.Clamp(rtt / 2000f + 0.08f, 0.05f, 0.6f);
        }
    }

    /// <summary>Other players' riders: their body is drawn relative to the locally shown vehicle instead of through their own (later) network transform.</summary>
    public static class RiderVisuals
    {
        public static void Follow(PlayerNet rider, Vector3 feetWorld, float yaw)
        {
            if (rider == null || rider.visualAnchor == null) return;
            rider.visualAnchor.SetPositionAndRotation(feetWorld, Quaternion.Euler(0f, yaw, 0f));
        }

        public static void Release(PlayerNet rider)
        {
            if (rider == null || rider.visualAnchor == null) return;
            rider.visualAnchor.localPosition = new Vector3(0f, -1f, 0f);
            rider.visualAnchor.localRotation = Quaternion.identity;
        }
    }
}
