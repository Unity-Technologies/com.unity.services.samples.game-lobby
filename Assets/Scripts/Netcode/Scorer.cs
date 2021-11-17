using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace LobbyRelaySample.ngo
{
    // TODO: I'm using host and server interchangeably...

    /// <summary>
    /// Used by the host to actually track scores for all players, and by each client to monitor for updates to their own score.
    /// </summary>
    public class Scorer : NetworkBehaviour
    {
        // TODO: Most of the ints could be bytes?
        private Dictionary<ulong, int> m_scoresByClientId = new Dictionary<ulong, int>();
        private ulong m_localId;
        [SerializeField] private TMP_Text m_scoreOutputText = default;

        public override void OnNetworkSpawn()
        {
            m_localId = NetworkManager.Singleton.LocalClientId;
            AddClient_ServerRpc(m_localId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void AddClient_ServerRpc(ulong id)
        {
            if (!m_scoresByClientId.ContainsKey(id))
                m_scoresByClientId.Add(id, 0);
        }

        public void ScoreSuccess(ulong id)
        {
            m_scoresByClientId[id] += 2;
            UpdateScoreOutput_ClientRpc(id, m_scoresByClientId[id]);
        }

        public void ScoreFailure(ulong id)
        {
            m_scoresByClientId[id] -= 1;
            UpdateScoreOutput_ClientRpc(id, m_scoresByClientId[id]);
        }

        [ClientRpc]
        private void UpdateScoreOutput_ClientRpc(ulong id, int score)
        {
            if (m_localId == id)
                m_scoreOutputText.text = score.ToString("00");
        }
    }
}
