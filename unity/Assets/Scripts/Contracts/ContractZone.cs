using UnityEngine;

namespace WhoBrokeIt.Contracts
{
    /// <summary>A delivery area. Counts ContractItems of one tag that rest inside its box (not carried, not flying). Bounds check, no triggers. The server uses it; clients only see the marker.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ContractZone : MonoBehaviour
    {
        public string zoneId = "";
        public string label = "";
        BoxCollider area;
        static ContractItem[] items;
        static float refreshAt;

        void Awake() { area = GetComponent<BoxCollider>(); }

        static ContractItem[] Items()
        {
            if (items == null || Time.time > refreshAt) { items = FindObjectsByType<ContractItem>(FindObjectsSortMode.None); refreshAt = Time.time + 5f; }
            return items;
        }

        public int Count(string itemTag)
        {
            if (area == null) area = GetComponent<BoxCollider>();
            var b = area.bounds;
            var n = 0;
            foreach (var it in Items())
            {
                if (it == null || it.itemTag != itemTag) continue;
                var rb = it.Body;
                if (rb != null && (!rb.useGravity || rb.linearVelocity.sqrMagnitude > 1f)) continue;   // carried or still flying
                if (b.Contains(it.transform.position)) n++;
            }
            return n;
        }
    }
}
