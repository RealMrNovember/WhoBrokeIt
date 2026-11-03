using Unity.Netcode.Components;

namespace WhoBrokeIt.Net
{
    /// <summary>Owner-authoritative transform sync for the player (responsive movement; anti-cheat is out of scope for co-op).</summary>
    public class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
