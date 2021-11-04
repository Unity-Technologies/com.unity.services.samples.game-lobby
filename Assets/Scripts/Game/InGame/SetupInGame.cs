using System;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    /// <summary>
    /// Once the local player is in a lobby and that lobby has entered the In-Game state, this will load in whatever is necessary to actually run the game part.
    /// This will exist in the game scene so that it can hold references to scene objects that spawned prefab instances will need.
    /// </summary>
    public class SetupInGame : MonoBehaviour, IReceiveMessages
    {
        [SerializeField] private GameObject m_prefabNetworkManager = default;
        [SerializeField] private GameObject[] m_disableWhileInGame = default;

        private GameObject m_inGameManagerObj;
        private NetworkManager m_networkManager;
        private InGameRunner m_inGameRunner;

        private bool m_isHost;
        private bool m_doesNeedCleanup = false;
        private bool m_hasConnectedViaNGO = false;

        // TEMP? Relay stuff
        private Action<UnityTransport> m_initializeTransport;
        private LocalLobby m_lobby;


        /*
         * Things to do:
         *
         * x Disable whatever menu behaviors. Maintain a back button with additional RPC calls?
         * --- Need to make RelayUtpClient not an MB so I can freely disable the menus? It is on the GameManager, as it happens, but...
         * x Spawn the object with the NetworkManager and allow that to connect.
         * - Wait for all players to connect, or boot a player after a few seconds (via Relay) if they did not connect.
         * x While waiting, server selects the target sequence, spawns the symbol container, and starts pooling/spawning the symbol objects.
         * - Once all players are in, show the target sequence and instructions, and then the server starts moving the symbol container and listening to click events.
         * - After the symbols are all passed (I guess tracking the symbol container position or a timeout), finish the game (set the winner flag).
         * x Clients clean up and return to the lobby screen. Host sets the lobby back to the regular state.
         * 
         */

        public void Start()
        {
            Locator.Get.Messenger.Subscribe(this);
        }
        public void OnDestroy()
        {
            Locator.Get.Messenger.Unsubscribe(this);
        }

        private void SetMenuVisibility(bool areVisible)
        {
            foreach (GameObject go in m_disableWhileInGame)
                go.SetActive(areVisible);
        }

        private void CreateNetworkManager()
        {
            m_inGameManagerObj = GameObject.Instantiate(m_prefabNetworkManager);
            m_networkManager = m_inGameManagerObj.GetComponentInChildren<NetworkManager>();
            m_inGameRunner = m_inGameManagerObj.GetComponentInChildren<InGameRunner>();
            m_inGameRunner.Initialize(OnConnectionVerified, m_lobby.PlayerCount, OnGameEnd);

            UnityTransport transport = m_inGameManagerObj.GetComponentInChildren<UnityTransport>();

            if (m_isHost)
                m_inGameManagerObj.AddComponent<relay.RelayUtpNGOSetupHost>().Initialize(this, m_lobby, () => { m_initializeTransport(transport); m_networkManager.StartHost(); });
            else
                m_inGameManagerObj.AddComponent<relay.RelayUtpNGOSetupClient>().Initialize(this, m_lobby, () => { m_initializeTransport(transport); m_networkManager.StartClient(); });
        }


        private void OnConnectionVerified()
        {
            m_hasConnectedViaNGO = true;
        }

        public void OnLobbyChange(LocalLobby lobby)
        {
            m_lobby = lobby; // Most of the time this is redundant, but we need to get multiple members of the lobby to the Relay setup components, so might as well just hold onto the whole thing.
        }
        public void OnLocalUserChange(LobbyUser user)
        {
            m_isHost = user.IsHost;
        }

        public void SetRelayServerData(string address, int port, byte[] allocationBytes, byte[] key, byte[] connectionData, byte[] hostConnectionData, bool isSecure)
        {
            m_initializeTransport = (transport) => { transport.SetRelayServerData(address, (ushort)port, allocationBytes, key, connectionData, hostConnectionData, isSecure); };
        }

        public void OnReceiveMessage(MessageType type, object msg)
        {
            if (type == MessageType.ConfirmInGameState)
            {
                m_doesNeedCleanup = true;
                SetMenuVisibility(false);
                CreateNetworkManager();
            }

            else if (type == MessageType.GameBeginning)
            {
                if (!m_hasConnectedViaNGO)
                {
                    // If this player hasn't successfully connected via NGO, get booted.
                    Locator.Get.Messenger.OnReceiveMessage(MessageType.DisplayErrorPopup, "Failed to join the game.");
                    // TODO: Need to handle both failing to connect and connecting but failing to initialize.
                    // I.e. cleaning up networked objects *might* be necessary.
                    OnGameEnd(); // TODO: This returns to the lobby. I think that's desirable?
                }
            }

            else if (type == MessageType.ChangeGameState)
            {
                // Once we're in-game, any state change reflects the player leaving the game, so we should clean up.
                OnGameEnd();
            }
        }

        private void OnGameEnd()
        {
            if (m_doesNeedCleanup)
            {
                GameObject.Destroy(m_inGameManagerObj); // Since this destroys the NetworkManager, that will kick off cleaning up networked objects.
                SetMenuVisibility(true);
                m_doesNeedCleanup = false;
            }
        }
    }
}
