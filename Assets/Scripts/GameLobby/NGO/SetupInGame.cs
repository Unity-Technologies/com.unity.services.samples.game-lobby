using System;
using System.Threading.Tasks;
using LobbyRelaySample.relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Once the local player is in a lobby and that lobby has entered the In-Game state, this will load in whatever is necessary to actually run the game part.
    /// This will exist in the game scene so that it can hold references to scene objects that spawned prefab instances will need.
    /// </summary>
    public class SetupInGame : MonoBehaviour, IReceiveMessages
    {
        [SerializeField] private GameObject m_IngameRunnerPrefab = default;
        [SerializeField] private GameObject[] m_disableWhileInGame = default;


        private InGameRunner m_inGameRunner;

        private bool m_doesNeedCleanup = false;
        private bool m_hasConnectedViaNGO = false;

        private LocalLobby m_lobby;
        private LobbyUser m_localUser;


        public void Start()
        {   Locator.Get.Messenger.Subscribe(this);
        }
        public void OnDestroy()
        {   Locator.Get.Messenger.Unsubscribe(this);
        }

        private void SetMenuVisibility(bool areVisible)
        {
            foreach (GameObject go in m_disableWhileInGame)
                go.SetActive(areVisible);
        }

        /// <summary>
        /// The prefab with the NetworkManager contains all of the assets and logic needed to set up the NGO minigame.
        /// The UnityTransport needs to also be set up with a new Allocation from Relay.
        /// </summary>
        private async Task CreateNetworkManager()
        {
            m_inGameRunner = Instantiate(m_IngameRunnerPrefab).GetComponentInChildren<InGameRunner>();
            m_inGameRunner.Initialize(OnConnectionVerified, m_lobby.PlayerCount, OnGameEnd, m_localUser);
            if (m_localUser.IsHost)
            {
                await SetRelayHostData();
                NetworkManager.Singleton.StartHost();
            }
            else
            {
                await SetRelayClientData();
                NetworkManager.Singleton.StartClient();
            }

        }

        async Task SetRelayHostData()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

            var allocation = await Relay.Instance.CreateAllocationAsync(m_lobby.MaxPlayerCount);
            var joincode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            m_lobby.RelayNGOCode = joincode;

            bool isSecure = false;
            var endpoint = RelayUtpSetup.GetEndpointForAllocation(allocation.ServerEndpoints,
                allocation.RelayServer.IpV4, allocation.RelayServer.Port, out isSecure);

            transport.SetHostRelayData(RelayUtpSetup.AddressFromEndpoint(endpoint), endpoint.Port,
                allocation.AllocationIdBytes, allocation.Key, allocation.ConnectionData, isSecure);
        }

        async Task SetRelayClientData()
        {
            UnityTransport transport = NetworkManager.Singleton.GetComponentInChildren<UnityTransport>();

            var joinAllocation = await Relay.Instance.JoinAllocationAsync(m_lobby.RelayCode);
            bool isSecure = false;
            var endpoint = RelayUtpSetup.GetEndpointForAllocation(joinAllocation.ServerEndpoints,
                joinAllocation.RelayServer.IpV4, joinAllocation.RelayServer.Port, out isSecure);

            transport.SetClientRelayData(RelayUtpSetup.AddressFromEndpoint(endpoint), endpoint.Port,
                joinAllocation.AllocationIdBytes, joinAllocation.Key,
                joinAllocation.ConnectionData, joinAllocation.HostConnectionData, isSecure);
        }

        private void OnConnectionVerified()
        {   m_hasConnectedViaNGO = true;
        }

        // These are public for use in the Inspector.
        public void OnLobbyChange(LocalLobby lobby)
        {   m_lobby = lobby; // Most of the time this is redundant, but we need to get multiple members of the lobby to the Relay setup components, so might as well just hold onto the whole thing.
        }
        public void OnLocalUserChange(LobbyUser user)
        {   m_localUser = user; // Same, regarding redundancy.
        }


        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.ConfirmInGameState)
            {
                m_doesNeedCleanup = true;
                SetMenuVisibility(false);
#pragma warning disable 4014
                CreateNetworkManager();
#pragma warning restore 4014
            }

            else if (type == MessageType.MinigameBeginning)
            {
                if (!m_hasConnectedViaNGO)
                {
                    // If this player hasn't successfully connected via NGO, forcibly exit the minigame.
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, "Failed to join the game.");
                    OnGameEnd();
                }
            }

            else if (type == MessageType.ChangeMenuState)
            {
                // Once we're in-game, any state change reflects the player leaving the game, so we should clean up.
                OnGameEnd();
            }
        }

        /// <summary>
        /// Return to the lobby after the game, whether due to the game ending or due to a failed connection.
        /// </summary>
        private void OnGameEnd()
        {
            if (m_doesNeedCleanup)
            {
                NetworkManager.Singleton.Shutdown();
                GameObject.Destroy(m_inGameRunner.gameObject); // Since this destroys the NetworkManager, that will kick off cleaning up networked objects.
                SetMenuVisibility(true);
                m_lobby.RelayNGOCode = null;
                m_doesNeedCleanup = false;
            }
        }
    }
}
