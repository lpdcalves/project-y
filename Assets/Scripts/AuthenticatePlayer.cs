using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class AuthenticatePlayer : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();

        SetupEvents();

        await SignInAnonymouslyAsync();
    }

    #region Authentication
    private void SetupEvents()
    {
        AuthenticationService.Instance.SignedIn += () =>
        {
            Logger.Instance.LogInfo(AuthenticationService.Instance.PlayerId);
            Logger.Instance.LogInfo(AuthenticationService.Instance.AccessToken);
        };

        AuthenticationService.Instance.SignInFailed += (err) =>
        {
            Logger.Instance.LogInfo(err.ToString());
        };

        AuthenticationService.Instance.SignedOut += () =>
        {
            Logger.Instance.LogInfo("Player Signed Out");
        };
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception ex)
        {
            Logger.Instance.LogInfo(ex.ToString());
        }
    }
    #endregion

    #region Lobby

    public async void FindMatch()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();
            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
        }
        catch (LobbyServiceException e)
        {
            CreateMatch();
        }
    }

    private async void CreateMatch()
    {
        try
        {
            string lobbyName = "aaa";
            int maxPlayers = 4;
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = false;

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            StartCoroutine(HeartBeatLobbyCoroutine(lobby.Id, 15));
        }
        catch(LobbyServiceException e)
        {

        }
    }

    IEnumerator HeartBeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    #endregion
}
