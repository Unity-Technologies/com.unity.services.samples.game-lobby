using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.inGame
{
    /// <summary>
    /// Once the NetworkManager has been spawned, we need something to manage the game state and setup other in-game objects
    /// that is itself a networked object, to track things like network connect events.
    /// </summary>
    public class InGameRunner : NetworkBehaviour
    {
        private Action m_onConnectionVerified;
        private int m_expectedPlayerCount; // Used by the host, but we can't call the RPC until the network connection completes.
        private bool m_canSpawnInGameObjects;
        private const int k_symbolCount = 100;
        private Queue<Vector2> m_pendingSymbolPositions = new Queue<Vector2>();

        [SerializeField] private NetworkObject m_playerCursorPrefab;
        [SerializeField] private NetworkObject m_symbolContainerPrefab;
        private Transform m_symbolContainerInstance;
        [SerializeField] private NetworkObject m_symbolObjectPrefab;

        private NetworkList<ulong> m_connectedPlayerIds;
        private ulong m_localClientId; // This is not necessarily the same as the OwnerClientId, since all clients will see all spawned objects regardless of ownership.

        public void Initialize(Action onConnectionVerified, int expectedPlayerCount)
        {
            m_onConnectionVerified = onConnectionVerified;
            m_expectedPlayerCount = expectedPlayerCount;
            m_canSpawnInGameObjects = false;
        }

        public override void OnNetworkSpawn()
        {
            if (IsHost)
                FinishInitialize();
            m_localClientId = NetworkManager.Singleton.LocalClientId;
            VerifyConnection_ServerRpc(m_localClientId);
        }

        public override void OnNetworkDespawn()
        {
            // This will be where to do full clean up?
            UnityEngine.Debug.LogError("InGameRunner despawn");
        }

        private void FinishInitialize()
        {
            if (m_connectedPlayerIds != null)
                m_connectedPlayerIds.Clear();
            else
                m_connectedPlayerIds = new NetworkList<ulong>();
            m_symbolContainerInstance = NetworkObject.Instantiate(m_symbolContainerPrefab).transform;
            ResetPendingSymbolPositions();
        }

        private void ResetPendingSymbolPositions()
        {
            m_pendingSymbolPositions.Clear();
            for (int n = 0; n < k_symbolCount; n++)
            {
                // TEMP we need to do a BSP or some such to mix up the positions.
                m_pendingSymbolPositions.Enqueue(new Vector2(-9 + (n % 10) * 2, n / 10 * 3));
            }
        }

        /// <summary>
        /// To verify the connection, invoke a server RPC call that then invokes a client RPC call.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void VerifyConnection_ServerRpc(ulong clientId)
        {
            VerifyConnection_ClientRpc(clientId);

            // If not spawning things in the background, start that.
            m_canSpawnInGameObjects = true;
        }
        [ClientRpc]
        private void VerifyConnection_ClientRpc(ulong clientId)
        {
            if (clientId == m_localClientId)
                VerifyConnectionConfirm_ServerRpc(m_localClientId);
        }
        /// <summary>
        /// Once the connection is confirmed, check if all players have connected.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void VerifyConnectionConfirm_ServerRpc(ulong clientId)
        {
            NetworkObject playerCursor = NetworkObject.Instantiate(m_playerCursorPrefab);
            playerCursor.SpawnWithOwnership(clientId);
            playerCursor.name += clientId;

            if (!m_connectedPlayerIds.Contains(clientId))
                m_connectedPlayerIds.Add(clientId);
            bool areAllPlayersConnected = m_connectedPlayerIds.Count >= m_expectedPlayerCount; // The game will begin at this point, or else there's a timeout for booting any unconnected players.
            VerifyConnectionConfirm_ClientRpc(clientId, areAllPlayersConnected);
        }
        [ClientRpc]
        private void VerifyConnectionConfirm_ClientRpc(ulong clientId, bool shouldStartImmediately)
        {
            if (clientId == m_localClientId)
                m_onConnectionVerified?.Invoke();
            if (shouldStartImmediately)
                Locator.Get.Messenger.OnReceiveMessage(MessageType.GameBeginning, null);
        }

        // TODO: BSP for choosing symbol spawn positions?
        // TODO: Remove the timer to test for packet loss.
        private float m_timer = 0.04f; // We'll want to space out the object spawning a little to reduce the risk of packet loss. It will happen in the background, so we have time.
        public void Update()
        {
            if (!m_canSpawnInGameObjects || m_symbolContainerInstance?.childCount >= k_symbolCount || !IsHost)
                return;
            if (m_pendingSymbolPositions.Count > 0)
            {
                m_timer -= Time.deltaTime;
                if (m_timer < 0)
                {
                    m_timer = 0.04f;
                    Vector3 pendingPos = m_pendingSymbolPositions.Dequeue();
                    NetworkObject symbolObj = NetworkObject.Instantiate(m_symbolObjectPrefab);
                    symbolObj.Spawn();
                    symbolObj.name = "Symbol" + (k_symbolCount - m_pendingSymbolPositions.Count);
                    symbolObj.TrySetParent(m_symbolContainerInstance, false);
                    symbolObj.transform.localPosition = pendingPos;
                }
            }
        }
    }
}
