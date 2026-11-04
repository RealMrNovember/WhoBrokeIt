using UnityEngine;
using WhoBrokeIt.Player;

namespace WhoBrokeIt.Props
{
    /// <summary>Links a remote player's server-side proxy collider back to its PlayerNet (the proxy is detached from the player).</summary>
    public class ProxyOwner : MonoBehaviour
    {
        public PlayerNet owner;
    }
}
