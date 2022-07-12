using TMPro;
using Unity.Netcode;

public class PlayerHud : NetworkBehaviour
{
    private NetworkVariable<NetworkString> playerName = new NetworkVariable<NetworkString>();

    private bool playerNameIsSet = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerName.Value = $"Player {OwnerClientId}";
        }
    }

    public void SetPlayerName()
    {
        var localPlayerOverlay = gameObject.GetComponentInChildren<TextMeshPro>();
        localPlayerOverlay.text = playerName.Value;
    }

    private void Update()
    {
        if(!playerNameIsSet && !string.IsNullOrEmpty(playerName.Value))
        {
            SetPlayerName();
            playerNameIsSet = true;
        }
    }
}