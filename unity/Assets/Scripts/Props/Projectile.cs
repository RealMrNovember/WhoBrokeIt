using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Props
{
    /// <summary>Added by the server to a thrown prop while it flies. A hit knocks a player back and stuns them briefly (no damage).</summary>
    public class Projectile : MonoBehaviour
    {
        PlayerNet thrower;
        Rigidbody rb;
        float age;

        public void Init(PlayerNet who)
        {
            thrower = who;
            rb = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;
            if (rb == null || age > 2.2f || (age > 0.4f && rb.linearVelocity.magnitude < 3f)) { Destroy(this); return; }

            foreach (var c in Physics.OverlapSphere(rb.position, 0.75f))
            {
                var victim = PlayerNet.FromCollider(c);
                if (victim == null || victim.IsHidden) continue;
                if (victim == thrower && age < 0.6f) continue;          // do not hit yourself on release
                var v = rb.linearVelocity;
                var dir = new Vector3(v.x, 0f, v.z).normalized;
                var power = Mathf.Clamp(v.magnitude * 0.7f, 5f, 11f);
                if (victim.ServerBonk(dir * power + Vector3.up * 3f, 1.0f))
                {
                    rb.linearVelocity = v * 0.4f;
                    Destroy(this);
                    return;
                }
            }
        }
    }
}
