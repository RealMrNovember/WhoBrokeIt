using UnityEngine;

namespace WhoBrokeIt.Player
{
    /// <summary>Spins the propeller of the propeller beanie.</summary>
    public class HatSpin : MonoBehaviour
    {
        public Transform target;
        void Update() { if (target != null) target.Rotate(0f, 720f * Time.deltaTime, 0f, Space.Self); }
    }
}
