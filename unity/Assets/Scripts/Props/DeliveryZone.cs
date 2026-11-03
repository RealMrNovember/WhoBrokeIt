using UnityEngine;

namespace WhoBrokeIt.Props
{
    /// <summary>True once the target body rests inside the zone for a moment.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class DeliveryZone : MonoBehaviour
    {
        public Rigidbody target;
        public float restSeconds = 1.5f;
        BoxCollider area;
        float rest;

        public bool Delivered { get; private set; }

        void Start() { area = GetComponent<BoxCollider>(); }

        void Update()
        {
            if (Delivered || target == null) return;
            var inside = area.bounds.Contains(target.position) && target.linearVelocity.magnitude < 0.4f;
            rest = inside ? rest + Time.deltaTime : 0f;
            if (rest >= restSeconds) Delivered = true;
        }
    }
}
