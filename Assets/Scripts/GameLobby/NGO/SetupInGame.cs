using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Once the local localPlayer is in a localLobby and that localLobby has entered the In-Game state, this will load in whatever is necessary to actually run the game part.
    /// This will exist in the game scene so that it can hold references to scene objects that spawned prefab instances will need.
    /// </summary>
    public class SetupInGame : MonoBehaviour
    {
        [SerializeField]
        GameObject m_IngameRunnerPrefab = default;
        [SerializeField]
        private GameObject[] m_disableWhileInGame = default;

        private InGameRunner m_inGameRunner;

        private bool m_doesNeedCleanup = false;
        private bool m_hasConnectedViaNGO = false;

        private LocalLobby m_lobby;

        private void SetMenuVisibility(bool areVisible)
        {
            foreach (GameObject go in m_disableWhileInGame)
                go.SetActive(areVisible);
        }

        /// <summary>
        /// The prefab with the NetworkManager contains all of the assets and logic needed to set up the NGO minigame.
        /// The UnityTransport needs to also be set up with a new Allocation from Relay.
        /// </summary>
        async Task CreateNetworkManager(LocalLobby localLobby, LocalPlayer localPlayer)
        {
            m_lobby = localLobby;
            m_inGameRunner = Instantiate(m_IngameRunnerPrefab).GetComponentInChildren<InGameRunner>();
            m_inGameRunner.Initialize(OnConnectionVerified, m_lobby.PlayerCount, OnGameBegin, OnGameEnd,
                localPlayer);
            if (localPlayer.IsHost.Value)
            {
                await SetRelayHostData();
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                await AwaitRelayCode(localLobby);
                await SetRelayClientData();
                NetworkManager.Singleton.StartClient();
            }
        }

        async Task AwaitRelayCode(LocalLobby lobby)
        {
            string relayCode = lobby.RelayCode.Value;
            lobby.RelayCode.onChanged += (code) => relayCode = code;
            while (string.IsNullOrEmpty(relayCode))
            {
                await Task.Delay(100);
            }
        }

        async Task SetRelayHostData()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

            var allocation = await Relay.Instance.CreateAllocationAsync(m_lobby.MaxPlayerCount.Value);
            var joincode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            GameManager.Instance.HostSetRelayCode(joincode);

            bool isSecure = false;
            var endpoint = GetEndpointForAllocation(allocation.ServerEndpoints,
                allocation.RelayServer.IpV4, allocation.RelayServer.Port, out isSecure);

            transport.SetHostRelayData(AddressFromEndpoint(endpoint), endpoint.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure);
        }

        async Task SetRelayClientData()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

            var joinAllocation = await Relay.Instance.JoinAllocationAsync(m_lobby.RelayCode.Value);
            bool isSecure = false;
            var endpoint = GetEndpointForAllocation(joinAllocation.ServerEndpoints,
                joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port, out isSecure);

            transport.SetClientRelayData(AddressFromEndpoint(endpoint), endpoint.Port,
                joinAllocation.AllocationIdBytes, joinAllocation.Key,
                joinAllocation.ConnectionData, joinAllocation.HostConnectionData, isSecure);
        }

        /// <summary>
        /// Determine the server endpoint for connecting to the Relay server, for either an Allocation or a JoinAllocation.
        /// If DTLS encryption is available, and there's a secure server endpoint available, use that as a secure connection. Otherwise, just connect to the Relay IP unsecured.
        /// </summary>
        NetworkEndPoint GetEndpointForAllocation(
            List<RelayServerEndpoint> endpoints,
            string ip,
            int port,
            out bool isSecure)
        {
#if ENABLE_MANAGED_UNITYTLS
            foreach (RelayServerEndpoint endpoint in endpoints)
            {
                if (endpoint.Secure && endpoint.Network == RelayServerEndpoint.NetworkOptions.Udp)
                {
                    isSecure = true;
                    return NetworkEndPoint.Parse(endpoint.Host, (ushort)endpoint.Port);
                }
            }
#endif
            isSecure = false;
            return NetworkEndPoint.Parse(ip, (ushort)port);
        }

        string AddressFromEndpoint(NetworkEndPoint endpoint)
        {
            return endpoint.Address.Split(':')[0];
        }

        void OnConnectionVerified()
        {
            m_hasConnectedViaNGO = true;
        }

        public void StartNetworkedGame(LocalLobby localLobby, LocalPlayer localPlayer)
        {
            m_doesNeedCleanup = true;
            SetMenuVisibility(false);
#pragma warning disable 4014
            CreateNetworkManager(localLobby, localPlayer);
#pragma warning restore 4014
        }

        public void OnGameBegin()
        {
            if (!m_hasConnectedViaNGO)
            {
                // If this localPlayer hasn't successfully connected via NGO, forcibly exit the minigame.
                LogHandlerSettings.Instance.SpawnErrorPopup("Failed to join the game.");
                OnGameEnd();
            }
        }

        /// <summary>
        /// Return to the localLobby after the game, whether due to the game ending or due to a failed connection.
        /// </summary>
        public void OnGameEnd()
        {
            if (m_doesNeedCleanup)
            {
                NetworkManager.Singleton.Shutdown(true);
                Destroy(m_inGameRunner
                    .transform.parent
                    .gameObject); // Since this destroys the NetworkManager, that will kick off cleaning up networked objects.
                SetMenuVisibility(true);
                m_lobby.RelayCode.Value = "";

                m_doesNeedCleanup = false;
            }
        }
    }
}