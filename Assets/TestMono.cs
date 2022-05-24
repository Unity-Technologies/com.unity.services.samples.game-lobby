using System;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;

public class TestMono : MonoBehaviour
{
    string m_WorkingLobbyId;
    string playerID;
    bool m_DidSigninComplete = false;

    // This is handled in the LobbyAsyncRequest calls normally, but we need to supply this for the dir
    Dictionary<string, PlayerDataObject> m_MockUserData;
    UnityTransport m_UnityTransport;
    NetworkManager m_NetworkManager;

    // Start is called before the first frame update
    public async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        m_NetworkManager = NetworkManager.Singleton;

        m_UnityTransport = m_NetworkManager.GetComponent<UnityTransport>();
        playerID = AuthenticationService.Instance.PlayerId;

        await Lobby_Relay_Disconnect_Async_Test();
    }

    async Task Lobby_Relay_Disconnect_Async_Test()
    {
        var lobby = await Create_Relay_And_Lobby();
        Debug.Log($"Created Lobby {lobby.Players.Count}");
        var kicked = false;
        var callbacks = new LobbyEventCallbacks();
        callbacks.KickedFromLobby += () =>
        {
            Debug.Log("GOT KICKED!");
            kicked = true;
        };
        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);

        await Task.Delay(5000); // Give it a moment

        //Local Disconnect
        NetworkManager.Singleton.Shutdown();

        //Wait for either the Lobby to signal that we were kicked, or for the time to run out. (Tested with up to 30s, still
        await Task.WhenAny(Task.Delay(10000), WaitForKicked());
        async Task WaitForKicked()
        {
            while (!kicked)
            {
                await Task.Delay(50);
            }
        }

        var getPostKickLobbyData = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
        try
        {
            var getRelay = await Relay.Instance.JoinAllocationAsync(getPostKickLobbyData.Data["joinCode"].Value);

            Debug.Log($"RelayAllocation : {getRelay.AllocationId} - {getRelay.RelayServer.IpV4} : {getRelay.RelayServer.Port}");
            StringBuilder sb = new StringBuilder("Endpoints: \n");
            foreach (var endpoint in getRelay.ServerEndpoints)
            {
                sb.AppendLine($"Host: {endpoint.Host} - ConnectionType: {endpoint.ConnectionType} -Port: {endpoint.Port} - Secure?:{endpoint.Secure}");
            }
        }
        catch(Exception ex)
        {
            Debug.Log($"Error getting Relay after disconnect: {ex}.");
        }
        Debug.Log($"Was {playerID} kicked? Lobby has {getPostKickLobbyData.Players.Count} player(s)");
        foreach (var player in getPostKickLobbyData.Players)
        {
            Debug.Log($"Player: {player.Id}");
        }
    }

    public async Task<Lobby> Create_Relay_And_Lobby()
    {
        // Allocate Relay
        var allocation = await Relay.Instance.CreateAllocationAsync(2, null);

        try
        {

            Debug.Log($"RelayAllocation : {allocation.AllocationId} - {allocation.RelayServer.IpV4} : {allocation.RelayServer.Port}");
            StringBuilder sb = new StringBuilder("Endpoints: \n");
            foreach (var endpoint in allocation.ServerEndpoints)
            {
                sb.AppendLine($"Host: {endpoint.Host} - ConnectionType: {endpoint.ConnectionType} -Port: {endpoint.Port} - Secure?:{endpoint.Secure}");
            }
        }
        catch(Exception ex)
        {
            Debug.Log("Error getting Relay after disconnect: {ex}.");
        }
        // Generate Join Code
        var joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // Create Lobby
        var options = new CreateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
            },
            Player = new Player(playerID, allocationId: allocation.AllocationId.ToString())
        };
        var lobby = await LobbyService.Instance.CreateLobbyAsync("testLobby", 2, options);
        m_UnityTransport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, true);

        // Start Host
        m_NetworkManager.StartHost();
        return lobby;
    }

    public async Task<Lobby> Join(string lobbyId, string password = "")
    {
        var player = new Player(playerID);
        var options = new JoinLobbyByIdOptions()
        {
            Player = player,
        };
        var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);

        // Join Relay
        var joinCode = lobby.Data["joinCode"].Value;
        var join = await Relay.Instance.JoinAllocationAsync(joinCode);

        await Lobbies.Instance.UpdatePlayerAsync(lobby.Id, player.Id, new UpdatePlayerOptions
        {
            AllocationId = join.AllocationId.ToString(),
            ConnectionInfo = joinCode
        });

        m_UnityTransport.SetClientRelayData(join.RelayServer.IpV4, (ushort)join.RelayServer.Port,
            join.AllocationIdBytes, join.Key, join.ConnectionData, join.HostConnectionData, true);

        m_NetworkManager.StartClient();
        return lobby;
    }
}