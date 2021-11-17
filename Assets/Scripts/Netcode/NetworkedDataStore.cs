using System;
using System.Collections.Generic;
using Unity.Netcode;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// A place to store data needed by networked behaviors. Each client has an instance, but the server's instance stores the actual data.
    /// </summary>
    public class NetworkedDataStore : NetworkBehaviour
    {
        // Using a singleton here since we need spawned PlayerCursors to be able to find it, but we don't need the flexibility offered by the Locator.
        public static NetworkedDataStore Instance;

        private Dictionary<ulong, string> m_playerNames = new Dictionary<ulong, string>();
        private ulong m_localId;

        public void Awake()
        {
            Instance = this;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (Instance == this)
                Instance = null;
        }

        public override void OnNetworkSpawn()
        {
            m_localId = NetworkManager.Singleton.LocalClientId;
        }

        public void AddPlayer(ulong id, string name)
        {
            if (!IsServer)
                return;

            if (!m_playerNames.ContainsKey(id))
                m_playerNames.Add(id, name);
            else
                m_playerNames[id] = name;
        }

        // NetworkList and NetworkDictionary are not considered suitable for production, so instead we use RPC calls to retrieve names from the host.
        private Action<string> m_onGetCurrent;
        public void GetPlayerName(ulong ownerId, Action<string> onGet)
        {
            m_onGetCurrent = onGet;
            GetPlayerName_ServerRpc(ownerId, m_localId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void GetPlayerName_ServerRpc(ulong id, ulong callerId)
        {
            string name = string.Empty;
            if (m_playerNames.ContainsKey(id))
                name = m_playerNames[id];
            GetPlayerName_ClientRpc(callerId, name);
        }

        [ClientRpc]
        public void GetPlayerName_ClientRpc(ulong callerId, string name)
        {
            if (callerId == m_localId)
            {   m_onGetCurrent?.Invoke(name);
                m_onGetCurrent = null;
            }
        }
    }
}
