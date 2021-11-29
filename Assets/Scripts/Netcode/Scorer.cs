using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.ngo
{
    // TODO: I'm using host and server interchangeably...which in part I have to since it's ServerRpc but I think IsHost vs. IsServer yield different results in some places?

    /// <summary>
    /// Used by the host to actually track scores for all players, and by each client to monitor for updates to their own score.
    /// </summary>
    public class Scorer : NetworkBehaviour
    {
        // TODO: Most of the ints could be bytes?
        [SerializeField] private NetworkedDataStore m_dataStore = default;
        private ulong m_localId;
        [SerializeField] private TMP_Text m_scoreOutputText = default;

        [Tooltip("When the game ends, this will be provided the results in order of rank (1st-place first, and so on).")]
        [SerializeField] private UnityEvent<PlayerData> m_onGameEnd = default;

        public override void OnNetworkSpawn()
        {
            m_localId = NetworkManager.Singleton.LocalClientId;
        }

        // Called on the host.
        public void ScoreSuccess(ulong id)
        {
            int newScore = m_dataStore.UpdateScore(id, 1);
            UpdateScoreOutput_ClientRpc(id, newScore);
        }

        public void ScoreFailure(ulong id)
        {
            int newScore = m_dataStore.UpdateScore(id, -1);
            UpdateScoreOutput_ClientRpc(id, newScore);
        }

        [ClientRpc]
        private void UpdateScoreOutput_ClientRpc(ulong id, int score)
        {
            if (m_localId == id)
                m_scoreOutputText.text = score.ToString("00");
        }

        public void OnGameEnd()
        {
            m_dataStore.GetAllPlayerData(m_onGameEnd);
        }
    }
}
