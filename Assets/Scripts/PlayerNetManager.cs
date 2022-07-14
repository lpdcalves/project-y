using Unity.Netcode;
using UnityEngine;

namespace ProjectY
{
    public class PlayerNetManager : NetworkBehaviour
    {
        public NetworkVariable<int> playerCount = new NetworkVariable<int>();

        private void Start()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }

        private void Update()
        {
            if (IsServer) {
                playerCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
            }
            else
            {
                UpdatePlayerCountOnClientServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void UpdatePlayerCountOnClientServerRpc()
        {
            playerCount.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            var clientId = request.ClientNetworkId;
            var connectionData = request.Payload;

            // Your approval logic determines the following values
            if (NetworkManager.Singleton.ConnectedClientsIds.Count < 4)
                response.Approved = true;
            else
                response.Approved = false;
            response.CreatePlayerObject = true;

            // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
            response.PlayerPrefabHash = null;

            // Position to spawn the player object (if null it uses default of Vector3.zero)
            response.Position = Vector3.zero;
            // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
            response.Rotation = Quaternion.identity;

            // If additional approval steps are needed, set this to true until the additional steps are complete
            // once it transitions from true to false the connection approval response will be processed.
            response.Pending = false;
        }
    }
}
