using ProjectY;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ChatBehaviour : NetworkBehaviour
{
    [SerializeField] private GameObject chatUI;
    [SerializeField] private TMP_Text chatText;
    [SerializeField] private TMP_InputField chatInput;

    private static event Action<string> OnMessage;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            chatUI.SetActive(true);
        }
        OnMessage += HandleNewMessage;
    }

    private void OnDestroy()
    {
        if(!IsOwner) { return; }
        OnMessage -= HandleNewMessage;
    }

    private void HandleNewMessage(string message)
    {
        chatText.text += message;
    }

    public void Send(string message)
    {
        if(!Input.GetKeyDown(KeyCode.Return)) { return; }
        if(string.IsNullOrWhiteSpace(message)) { return; }

        SendMessageServerRpc(MatchNetworkManager.Instance.UIManager.playerName, message);

        chatInput.text = string.Empty;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendMessageServerRpc(string playerName, string message)
    {
        BroadcastChatMessageClientRpc($"[{DateTime.Now.ToString("HH:mm")}] {playerName}: {message}");
    }

    [ClientRpc]
    private void BroadcastChatMessageClientRpc(string message)
    {
        OnMessage?.Invoke($"\n{message}");
    }
    
}
