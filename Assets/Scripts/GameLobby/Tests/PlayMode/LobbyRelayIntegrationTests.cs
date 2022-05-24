using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{
    public class LobbyRelayIntegrationTests
    {
        string m_WorkingLobbyId;
        LobbyRelaySample.Auth.SubIdentity_Authentication m_Auth;
        bool m_DidSigninComplete = false;

        // This is handled in the LobbyAsyncRequest calls normally, but we need to supply this for the dir
        Dictionary<string, PlayerDataObject> m_MockUserData;
        UnityTransport m_UnityTransport;
        NetworkManager m_NetworkManager;

        [OneTimeSetUp]
        public void Setup()
        {
            m_MockUserData = new Dictionary<string, PlayerDataObject>();
            m_MockUserData.Add("DisplayName",
                new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "TestUser123"));
            m_NetworkManager = GameObject.Instantiate(Resources.Load<NetworkManager>("TestNetworkManager"));
            m_UnityTransport = m_NetworkManager.GetComponent<UnityTransport>();
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            m_Auth?.Dispose();
        }

        [UnityTest]
        public IEnumerator Lobby_Relay_Disconnect_Link()
        {
            m_Auth = new LobbyRelaySample.Auth.SubIdentity_Authentication(() => { m_DidSigninComplete = true; });
            var task = Task.Run(async () =>
            {
                await Lobby_Relay_Disconnect_Async();
            });

            while (!task.IsCompleted) { yield return null; }

            if (task.IsFaulted)
            {
                Debug.LogError($"Error while Running task:{task.Exception}");
            }
        }

        async Task Lobby_Relay_Disconnect_Async()
        {
            await WaitForLogin();
            var lobby = await Create();
            Assert.AreEqual(1, lobby.Players.Count);
            var kicked = false;
            var callbacks = new LobbyEventCallbacks();
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
            callbacks.KickedFromLobby += () =>
            {
                Debug.Log("GOT KICKED!");
                kicked = true;
            };

            NetworkManager.Singleton.Shutdown();
            await Task.WhenAny(Task.Delay(10000), WaitForKicked());

            async Task WaitForKicked()
            {
                while (!kicked)
                {
                    await Task.Delay(50);
                }
            }

            var getLobbyData = await LobbyService.Instance.GetLobbyAsync(lobby.Id);
            foreach (var player in getLobbyData.Players)
            {
                Debug.Log(player.Id);
            }

            Assert.AreEqual(0, getLobbyData.Players.Count);
        }

        async Task WaitForLogin()
        {
            while (!m_DidSigninComplete)
            {
                await Task.Delay(50);
            }
        }

        public async Task<Lobby> Create()
        {
            // Allocate Relay
            var allocation = await Relay.Instance.CreateAllocationAsync(2);
            m_UnityTransport.SetHostRelayData(allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, true);

            // Generate Join Code
            var joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Create Lobby
            var options = new CreateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>()
                {
                    { "joinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
                },
                Player = new Player(m_Auth.ID, allocationId: allocation.AllocationId.ToString())
            };
            var lobby = await LobbyService.Instance.CreateLobbyAsync("testLobby", 2, options);

            // Start Host
            m_NetworkManager.StartHost();
            return lobby;
        }

        public async Task<Lobby> Join(string lobbyId, string password = "")
        {
            var player = new Player(m_Auth.ID);
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
}