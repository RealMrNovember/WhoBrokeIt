using UnityEngine;

namespace WhoBrokeIt.Props
{
    /// <summary>Counts Debris currently inside its box (bounds check, no triggers).</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CollectionZone : MonoBehaviour
    {
        Debris[] all;
        BoxCollider area;

        void Start()
        {
            area = GetComponent<BoxCollider>();
            all = FindObjectsByType<Debris>(FindObjectsSortMode.None);
        }

        public int Count
        {
            get
            {
                if (all == null || area == null) return 0;
                var n = 0;
                var b = area.bounds;
                foreach (var d in all) if (d != null && b.Contains(d.transform.position)) n++;
                return n;
            }
        }
    }
}
