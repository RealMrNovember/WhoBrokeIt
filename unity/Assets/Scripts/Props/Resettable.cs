using System.Collections.Generic;
using UnityEngine;

namespace WhoBrokeIt.Props
{
    /// <summary>Remembers a start pose so stuck props/vehicles can be reset (hold X).</summary>
    public class Resettable : MonoBehaviour
    {
        Vector3 pos;
        Quaternion rot;
        Rigidbody rb;
        public System.Action onReset;

        static readonly List<Resettable> all = new List<Resettable>();

        void Awake()
        {
            pos = transform.position;
            rot = transform.rotation;
            rb = GetComponent<Rigidbody>();
            all.Add(this);
        }

        void OnDestroy() { all.Remove(this); }

        public void ResetNow()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = pos;
                rb.rotation = rot;
            }
            transform.SetPositionAndRotation(pos, rot);
            if (onReset != null) onReset();
        }

        public static Resettable Nearest(Vector3 from, float maxDistance)
        {
            Resettable best = null;
            var bestD = maxDistance;
            foreach (var r in all)
            {
                if (r == null) continue;
                var d = Vector3.Distance(from, r.transform.position);
                if (d < bestD) { bestD = d; best = r; }
            }
            return best;
        }
    }
}
