using System;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LobbyAndRelayTester : MonoBehaviour
{
    [SerializeField]
    string m_JoinCode;

    [SerializeField]
    string m_RelayCode;

    [SerializeField]
    bool m_Host;

    Guid m_RelayAllocationId;
    bool m_InLobby;
    string m_WorkingLobbyId;

    string m_PlayerId;
    Timer m_Time;
    Lobby m_CurrentLobby;

    // This is handled in the LobbyAsyncRequest calls normally, but we need to supply this for the dir
    Dictionary<string, PlayerDataObject> m_MockUserData;
    UnityTransport m_UnityTransport;
    NetworkManager m_NetworkManager;
    CancellationTokenSource m_CancellationTokenSource;
    Stopwatch m_timer;

    // Start is called before the first frame update
    public async void Start()
    {
        var profileName = m_Host ? "host" : "client";
        var initOptions = new InitializationOptions().SetProfile(profileName);
        await UnityServices.InitializeAsync(initOptions);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        m_NetworkManager = NetworkManager.Singleton;
        m_timer = new Stopwatch();
        m_timer.Start();
        m_UnityTransport = m_NetworkManager.GetComponent<UnityTransport>();
        m_PlayerId = AuthenticationService.Instance.PlayerId;
        m_CancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = m_CancellationTokenSource.Token;
        Debug.Log($"Starting as: {profileName} - {m_PlayerId}");
        if (m_Host)
        {
            var hostLobby = await Host_Relay_Lobby();
            if (hostLobby == null)
                return;
            m_JoinCode = hostLobby.LobbyCode;
#pragma warning disable 4014
            KeepLobbyAlive(hostLobby, cancellationToken);
#pragma warning restore 4014
        }
        else
        {
            await Join_Disconnect_Test();
        }

        await PollLobby(cancellationToken);
    }

    async Task KeepLobbyAlive(Lobby lobby, CancellationToken cancel)
    {
        m_InLobby = true;
        var kickedPlayer = false;
        while (m_InLobby && !cancel.IsCancellationRequested)
        {
            await Task.Delay(9000);
            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);

            Debug.Log("HeartBeat.");
        }
    }

    async Task PollLobby(CancellationToken cancel)
    {
        m_InLobby = true;
        while (m_InLobby && !cancel.IsCancellationRequested)
        {
            await Task.Delay(3000);
            m_CurrentLobby = await LobbyService.Instance.GetLobbyAsync(m_CurrentLobby.Id);
            Debug.Log(
                $"Polled Lobby @ {m_timer.Elapsed.Minutes} : {m_timer.Elapsed.Seconds}.\nID:{m_CurrentLobby.Id} HostID:{m_CurrentLobby.HostId} " +
                $"- Code: {m_CurrentLobby.LobbyCode} - Players: {m_CurrentLobby.Players.Count} - Private: {m_CurrentLobby.IsPrivate}");
        }
    }

    async Task<Lobby> Host_Relay_Lobby()
    {
        var allocation = await Relay.Instance.CreateAllocationAsync(3, null);
        m_RelayAllocationId = allocation.AllocationId;
        Debug.Log($"RelayAllocation : {m_RelayAllocationId} " +
            $"- {allocation.RelayServer.IpV4} : {allocation.RelayServer.Port}");

        StringBuilder sb = new StringBuilder("Endpoints: \n");
        foreach (var endpoint in allocation.ServerEndpoints)
        {
            sb.AppendLine($"Host: {endpoint.Host} - ConnectionType: {endpoint.ConnectionType} " +
                $"- Port: {endpoint.Port} - Secure?:{endpoint.Secure}");
        }

        Debug.Log(sb);

        m_UnityTransport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData);

        // Start Host
        m_NetworkManager.StartHost();

        // Generate Join_Relay_Lobby Code
        m_RelayCode = await Relay.Instance.GetJoinCodeAsync(m_RelayAllocationId);

        // Create Lobby
        var options = new CreateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "relayJoinCode", new DataObject(DataObject.VisibilityOptions.Public, m_RelayCode) },
            },
            Player = new Player(m_PlayerId, allocationId: m_RelayAllocationId.ToString())
        };

        m_CurrentLobby = await LobbyService.Instance.CreateLobbyAsync("testLobby", 3, options);
        return m_CurrentLobby;
    }

    async Task<Lobby> Join_Relay_Lobby(string lobbyId, string password = "")
    {
        var player = new Player(m_PlayerId);
        var options = new JoinLobbyByCodeOptions
        {
            Player = player,
        };
        m_CurrentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyId, options);

        Debug.Log($"Joined Lobby: {m_CurrentLobby.Id} - {m_CurrentLobby.Players.Count}");

        m_RelayCode = m_CurrentLobby.Data["relayJoinCode"].Value;

        Debug.Log($"Joined Lobby: {m_CurrentLobby.Id} - {m_CurrentLobby.Players.Count} - RelayCode:{m_RelayCode}");

        var joinAllocation = await Relay.Instance.JoinAllocationAsync(m_RelayCode);

        await Lobbies.Instance.UpdatePlayerAsync(m_CurrentLobby.Id, player.Id,
            new UpdatePlayerOptions
            {
                AllocationId = joinAllocation.AllocationId.ToString()
            });
        Debug.Log($"Joined Lobby: {m_CurrentLobby.Id} - {m_CurrentLobby.Players.Count} - RelayCode:{m_RelayCode}");
        m_UnityTransport.SetClientRelayData(joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes, joinAllocation.Key, joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData, false);

        if (!m_NetworkManager.StartClient())
        {
            Debug.Log("Could not start client?");
        }

        return m_CurrentLobby;
    }

    async Task Host_Disconnect_Test()
    {
        var lobby = await Host_Relay_Lobby();
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
                await Task.Delay(50);
        }

        var getPostKickLobbyData = await LobbyService.Instance.GetLobbyAsync(lobby.Id);

        var getRelay = await Relay.Instance.JoinAllocationAsync(getPostKickLobbyData.Data["relayJoinCode"].Value);

        Debug.Log(
            $"RelayAllocation : {getRelay.AllocationId} - {getRelay.RelayServer.IpV4} : {getRelay.RelayServer.Port}");
        StringBuilder sb = new StringBuilder("Endpoints: \n");
        foreach (var endpoint in getRelay.ServerEndpoints)
        {
            sb.AppendLine(
                $"Host: {endpoint.Host} - ConnectionType: {endpoint.ConnectionType} -Port: {endpoint.Port} - Secure?:{endpoint.Secure}");
        }

        Debug.Log($"Was {m_PlayerId} kicked? Lobby has {getPostKickLobbyData.Players.Count} player(s)");
        foreach (var player in getPostKickLobbyData.Players)
        {
            Debug.Log($"Player: {player.Id}");
        }
    }

    //need to start the lobby host in one instance and join from another
    async Task Join_Disconnect_Test()
    {
        var lobby = await Join_Relay_Lobby(m_JoinCode);

        Debug.Log($"Created Lobby {lobby.Players.Count}");
        var kicked = false;
        var callbacks = new LobbyEventCallbacks();
        callbacks.KickedFromLobby += () =>
        {
            Debug.Log($"GOT KICKED! @ {m_timer.Elapsed.Minutes} : {m_timer.Elapsed.Seconds}");
            kicked = true;
        };

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);

        await Task.Delay(3000);

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

        if (!kicked)
        {
            Debug.LogError($"Player was not kicked");
        }

        var getPostKickLobbyData = await LobbyService.Instance.GetLobbyAsync(lobby.Id);

        var getRelay = await Relay.Instance.JoinAllocationAsync(getPostKickLobbyData.Data["relayJoinCode"].Value);

        Debug.Log(
            $"RelayAllocation : {getRelay.AllocationId} - {getRelay.RelayServer.IpV4} : {getRelay.RelayServer.Port}");
        StringBuilder sb = new StringBuilder("Endpoints: \n");
        foreach (var endpoint in getRelay.ServerEndpoints)
        {
            sb.AppendLine(
                $"Host: {endpoint.Host} - ConnectionType: {endpoint.ConnectionType} -Port: {endpoint.Port} - Secure?:{endpoint.Secure}");
        }

        Debug.Log($"Lobby has {getPostKickLobbyData.Players.Count} player(s)");
        foreach (var player in getPostKickLobbyData.Players)
        {
            Debug.Log($"Player: {player.Id}");
        }
    }

    public void OnDestroy()
    {
        m_CancellationTokenSource?.Cancel();
        Lobbies.Instance?.DeleteLobbyAsync(m_CurrentLobby.Id);
    }
}