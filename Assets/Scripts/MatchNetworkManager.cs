using Core.Singletons;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace ProjectY
{
    public class MatchNetworkManager : NetworkSingleton<MatchNetworkManager>
    {
        public UIManager UIManager;

        public NetworkVariable<int> playerCount = new NetworkVariable<int>();

        private bool hasServerStarted;

        public List<Transform> spawnPositions = new List<Transform>();

        public string matchPassword;

        private void Start()
        {
            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

            // STATUS TYPE CALLBACKS
            NetworkManager.Singleton.OnServerStarted += () =>
            {
                hasServerStarted = true;
            };
            NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
            {
                Logger.Instance.LogInfo($"{id} just connected...");
            };
            NetworkManager.Singleton.OnClientDisconnectCallback += (id) =>
            {
                Logger.Instance.LogInfo($"{id} just disconnected...");
            };
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

        public void StartServer()
        {
            if (NetworkManager.Singleton.StartServer())
            {
                matchPassword = UIManager.joinCodeInput.text;
                Logger.Instance.LogInfo("Server started...");
            }
            else
                Logger.Instance.LogInfo("Unable to start server...");
        }

        public async Task StartHost()
        {
            // this allows the UnityMultiplayer and UnityMultiplayerRelay scene to work with and without
            // relay features - if the Unity transport is found and is relay protocol then we redirect all the 
            // traffic through the relay, else it just uses a LAN type (UNET) communication.
            if (RelayManager.Instance.IsRelayEnabled)
                await RelayManager.Instance.SetupRelay();

            if (NetworkManager.Singleton.StartHost())
            {
                matchPassword = UIManager.joinCodeInput.text;
                Logger.Instance.LogInfo("Host started...");
            }
            else
                Logger.Instance.LogInfo("Unable to start host...");
        }

        public async Task StartClient(string joinCode)
        {
            if (RelayManager.Instance.IsRelayEnabled && !string.IsNullOrEmpty(joinCode))
                await RelayManager.Instance.JoinRelay(joinCode);

            NetworkManager.Singleton.NetworkConfig.ConnectionData =
                Encoding.ASCII.GetBytes(UIManager.joinCodeInput.text);

            if (NetworkManager.Singleton.StartClient())
                Logger.Instance.LogInfo("Client started...");
            else
                Logger.Instance.LogInfo("Unable to start client...");
        }

        public void LeaveMatch()
        {
            NetworkManager.Singleton.Shutdown();
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

            string password = Encoding.ASCII.GetString(connectionData);

            bool approve = password == UIManager.joinCodeInput.text;

            // Your approval logic determines the following values
            if (NetworkManager.Singleton.ConnectedClientsIds.Count < 10 && approve)
                response.Approved = true;
            else
                response.Approved = false;
            response.CreatePlayerObject = true;

            // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
            response.PlayerPrefabHash = null;

            // Position to spawn the player object (if null it uses default of Vector3.zero)
            var idx = Random.Range(0, spawnPositions.Count);
            response.Position = spawnPositions[idx].position;
            // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
            response.Rotation = Quaternion.identity;

            // If additional approval steps are needed, set this to true until the additional steps are complete
            // once it transitions from true to false the connection approval response will be processed.
            response.Pending = false;
        }
    }
}
