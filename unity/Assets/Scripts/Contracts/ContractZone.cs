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

        /// <summary>Items of this tag resting inside the zone. Items already spent on a finished contract here do not count until they have left and come back.</summary>
        public int Count(string itemTag) => Scan(itemTag, false);

        /// <summary>Server: marks the items currently counted as delivered, so the next contract cannot be completed with the same pile.</summary>
        public void Spend(string itemTag) => Scan(itemTag, true);

        /// <summary>Server housekeeping, also while no contract runs: an item that left the zone it was spent in counts again.</summary>
        public static void ClearSpent(ContractZone[] zones)
        {
            foreach (var it in Items())
            {
                if (it == null || string.IsNullOrEmpty(it.spentZone)) continue;
                ContractZone home = null;
                foreach (var z in zones) if (z != null && z.zoneId == it.spentZone) { home = z; break; }
                if (home == null || !home.Area.bounds.Contains(it.transform.position)) it.spentZone = "";
            }
        }

        BoxCollider Area { get { if (area == null) area = GetComponent<BoxCollider>(); return area; } }

        int Scan(string itemTag, bool spend)
        {
            if (area == null) area = GetComponent<BoxCollider>();
            var b = area.bounds;
            var n = 0;
            foreach (var it in Items())
            {
                if (it == null || it.itemTag != itemTag) continue;
                var inside = b.Contains(it.transform.position);
                if (!inside) { if (it.spentZone == zoneId) it.spentZone = ""; continue; }
                if (it.spentZone == zoneId) continue;                                  // delivered before: must leave the zone first
                var rb = it.Body;
                if (rb != null && (!rb.useGravity || rb.linearVelocity.sqrMagnitude > 1f)) continue;   // carried or still flying
                n++;
                if (spend) it.spentZone = zoneId;
            }
            return n;
        }
    }
}
