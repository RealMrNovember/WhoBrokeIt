using UnityEngine;

namespace WhoBrokeIt.Contracts
{
    /// <summary>Marks a physics prop as something a contract can ask for (crate, pallet, planks, sack, barrel...). Scene object, present on server and clients.</summary>
    public class ContractItem : MonoBehaviour
    {
        public string itemTag = "";
        /// <summary>Server only: id of the zone this item was already delivered to. It counts again only after it has left that zone (see ContractZone).</summary>
        [System.NonSerialized] public string spentZone = "";
        Rigidbody rb;
        public Rigidbody Body => rb != null ? rb : (rb = GetComponent<Rigidbody>());

        /// <summary>Guesses the tag from a kit name: prop_crate_b -> crate, prop_planks_a -> planks.</summary>
        public static string TagFromName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "";
            var p = n.Split('_');
            return p.Length >= 2 ? p[1] : n;
        }
    }
}
