using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace WhoBrokeIt.Net
{
    /// <summary>Connection approval: same game version only, and a hard player cap.</summary>
    public class ConnectionGate : MonoBehaviour
    {
        public const int MaxPlayers = 4;

        void Start()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.NetworkConfig.ConnectionApproval = true;
            nm.ConnectionApprovalCallback = Approve;
        }

        /// <summary>Call before StartClient so the server can compare versions.</summary>
        public static void PrepareClient()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(Application.version);
        }

        static void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var nm = NetworkManager.Singleton;
            response.CreatePlayerObject = true;

            // the host's own connection is always accepted
            if (request.ClientNetworkId == nm.LocalClientId) { response.Approved = true; return; }

            var theirs = request.Payload != null ? Encoding.UTF8.GetString(request.Payload) : "";
            if (theirs != Application.version)
            {
                response.Approved = false;
                response.Reason = "Sürüm uyuşmuyor (sunucu " + Application.version + ", sen " + (theirs == "" ? "?" : theirs) + "). Oyunu güncelle.";
                return;
            }
            if (nm.ConnectedClientsIds.Count >= MaxPlayers)
            {
                response.Approved = false;
                response.Reason = "Oda dolu (en fazla " + MaxPlayers + " kişi).";
                return;
            }
            response.Approved = true;
        }
    }
}
