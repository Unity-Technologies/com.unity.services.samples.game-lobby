using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Once the NetworkManager has been spawned, we need something to manage the game state and setup other in-game objects
    /// that is itself a networked object, to track things like network connect events.
    /// </summary>
    public class InGameRunner : NetworkBehaviour, IInGameInputHandler
    {
        private Action m_onConnectionVerified, m_onGameEnd;
        private int m_expectedPlayerCount; // Used by the host, but we can't call the RPC until the network connection completes.
        private bool? m_canSpawnInGameObjects;
        private Queue<Vector2> m_pendingSymbolPositions = new Queue<Vector2>();
        private float m_symbolSpawnTimer = 0.5f; // Initial time buffer to ensure connectivity before loading objects.
        private int m_remainingSymbolCount = 0; // Only used by the host.
        private float m_timeout = 10;
        private bool m_hasConnected = false;

        [SerializeField] private NetworkObject      m_playerCursorPrefab = default;
        [SerializeField] private NetworkObject      m_symbolContainerPrefab = default;
        [SerializeField] private NetworkObject      m_symbolObjectPrefab = default;
        [SerializeField] private SequenceSelector   m_sequenceSelector = default;
        [SerializeField] private Scorer             m_scorer = default;
        [SerializeField] private SymbolKillVolume   m_killVolume = default;
        [SerializeField] private IntroOutroRunner   m_introOutroRunner = default;
        [SerializeField] private NetworkedDataStore m_dataStore = default;
        private Transform m_symbolContainerInstance;

        private PlayerData m_localUserData; // This has an ID that's not necessarily the OwnerClientId, since all clients will see all spawned objects regardless of ownership.

        public void Initialize(Action onConnectionVerified, int expectedPlayerCount, Action onGameEnd, LobbyUser localUser)
        {
            m_onConnectionVerified = onConnectionVerified;
            m_expectedPlayerCount = expectedPlayerCount;
            m_onGameEnd = onGameEnd;
            m_canSpawnInGameObjects = null;
            m_localUserData = new PlayerData(localUser.DisplayName, 0);
            Locator.Get.Provide(this); // Simplifies access since some networked objects can't easily communicate locally (e.g. the host might call a ClientRpc without that client knowing where the call originated).
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
                FinishInitialize();
            m_localUserData = new PlayerData(m_localUserData.name, NetworkManager.Singleton.LocalClientId);
            VerifyConnection_ServerRpc(m_localUserData.id);
        }

        public override void OnNetworkDespawn()
        {
            m_onGameEnd(); // As a backup to ensure in-game objects get cleaned up, if this is disconnected unexpectedly.
        }

        private void FinishInitialize()
        {
            m_symbolContainerInstance = NetworkObject.Instantiate(m_symbolContainerPrefab).transform;
            ResetPendingSymbolPositions();
            m_killVolume.Initialize(OnSymbolDeactivated);
        }

        private void ResetPendingSymbolPositions()
        {
            m_pendingSymbolPositions.Clear();
            IList<Vector2> points = m_sequenceSelector.GenerateRandomSpawnPoints(new Rect(-15, 0, 30, 120), 2);
            foreach (Vector2 point in points)
                m_pendingSymbolPositions.Enqueue(point);
        }

        /// <summary>
        /// To verify the connection, invoke a server RPC call that then invokes a client RPC call. After this, the actual setup occurs.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void VerifyConnection_ServerRpc(ulong clientId)
        {
            VerifyConnection_ClientRpc(clientId);
            // While we could start pooling symbol objects now, incoming clients would be flooded with the Spawn calls.
            // This could lead to dropped packets such that the InGameRunner's Spawn call fails to occur, so we'll wait until all players join.
            // (Besides, we will need to display instructions, which has downtime during which symbol objects can be spawned.)
        }
        [ClientRpc]
        private void VerifyConnection_ClientRpc(ulong clientId)
        {
            if (clientId == m_localUserData.id)
                VerifyConnectionConfirm_ServerRpc(m_localUserData);
        }
        /// <summary>
        /// Once the connection is confirmed, spawn a player cursor and check if all players have connected.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void VerifyConnectionConfirm_ServerRpc(PlayerData clientData)
        {
            NetworkObject playerCursor = NetworkObject.Instantiate(m_playerCursorPrefab); // Note that the client will not receive the cursor object reference, so the cursor must handle initializing itself.
            playerCursor.SpawnWithOwnership(clientData.id);
            playerCursor.name += clientData.name;
            m_dataStore.AddPlayer(clientData.id, clientData.name);

            bool areAllPlayersConnected = NetworkManager.ConnectedClients.Count >= m_expectedPlayerCount; // The game will begin at this point, or else there's a timeout for booting any unconnected players.
            VerifyConnectionConfirm_ClientRpc(clientData.id, areAllPlayersConnected);
        }
        [ClientRpc]
        private void VerifyConnectionConfirm_ClientRpc(ulong clientId, bool canBeginGame)
        {
            if (clientId == m_localUserData.id)
            {
                m_onConnectionVerified?.Invoke();
                m_hasConnected = true;
            }
            if (canBeginGame && m_hasConnected)
            {
                m_timeout = -1;
                BeginGame();
            }
        }

        /// <summary>
        /// The game will begin either when all players have connected successfully or after a timeout.
        /// </summary>
        private void BeginGame()
        {
            m_canSpawnInGameObjects = true;
            Locator.Get.Messenger.OnReceiveMessage(MessageType.MinigameBeginning, null);
            m_introOutroRunner.DoIntro();
        }

        public void Update()
        {
            CheckIfCanSpawnNewSymbol();
            if (m_timeout >= 0)
            {
                m_timeout -= Time.deltaTime;
                if (m_timeout < 0)
                    BeginGame();
            }

            void CheckIfCanSpawnNewSymbol()
            {
                if (!m_canSpawnInGameObjects.GetValueOrDefault() || m_remainingSymbolCount >= SequenceSelector.k_symbolCount || !IsHost)
                    return;
                if (m_pendingSymbolPositions.Count > 0)
                {
                    m_symbolSpawnTimer -= Time.deltaTime;
                    if (m_symbolSpawnTimer < 0)
                    {
                        m_symbolSpawnTimer = 0.02f; // Space out the object spawning a little to prevent a lag spike.
                        SpawnNewSymbol();
                        if (m_remainingSymbolCount >= SequenceSelector.k_symbolCount)
                            m_canSpawnInGameObjects = false;
                    }
                }
            }
            void SpawnNewSymbol()
            {
                int index = SequenceSelector.k_symbolCount - m_pendingSymbolPositions.Count;
                Vector3 pendingPos = m_pendingSymbolPositions.Dequeue();
                NetworkObject symbolObj = NetworkObject.Instantiate(m_symbolObjectPrefab);
                symbolObj.Spawn();
                symbolObj.name = "Symbol" + index;
                symbolObj.TrySetParent(m_symbolContainerInstance, false);
                symbolObj.transform.localPosition = pendingPos;
                symbolObj.GetComponent<SymbolObject>().symbolIndex.Value = m_sequenceSelector.GetNextSymbol(index);
                m_remainingSymbolCount++;
            }
        }

        /// <summary>
        /// Called while on the host to determine if incoming input has scored or not.
        /// </summary>
        public void OnPlayerInput(ulong id, SymbolObject selectedSymbol)
        {
            if (m_sequenceSelector.ConfirmSymbolCorrect(id, selectedSymbol.symbolIndex.Value))
            {
                selectedSymbol.Destroy_ServerRpc();
                m_scorer.ScoreSuccess(id);
                OnSymbolDeactivated();
            }
            else
                m_scorer.ScoreFailure(id);
        }

        public void OnSymbolDeactivated()
        {
            if (--m_remainingSymbolCount <= 0)
                WaitForEndingSequence_ClientRpc();
        }

        /// <summary>
        /// The server determines when the game should end. Once it does, it needs to inform the clients to clean up their networked objects first,
        /// since disconnecting before that happens will prevent them from doing so (since they can't receive despawn events from the disconnected server).
        /// </summary>
        [ClientRpc]
        private void WaitForEndingSequence_ClientRpc()
        {
            m_scorer.OnGameEnd();
            m_introOutroRunner.DoOutro(EndGame);
        }

        private void EndGame()
        {
            if (IsHost)
                StartCoroutine(EndGame_ClientsFirst());
        }

        private IEnumerator EndGame_ClientsFirst()
        {
            EndGame_ClientRpc();
            yield return null;
            SendLocalEndGameSignal();
        }

        [ClientRpc]
        private void EndGame_ClientRpc()
        {
            if (IsHost)
                return;
            SendLocalEndGameSignal();
        }

        private void SendLocalEndGameSignal()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.EndGame, null); // We only send this message if the game completes, since the player remains in the lobby in that case. If the player leaves with the back button, that instead sends them to the menu.
            m_onGameEnd();
        }

        public void OnReProvided(IInGameInputHandler previousProvider) { /*No-op*/ }
    }
}
