using UnityEngine;
using Unity.Netcode;

namespace LobbyRelaySample.ngo
{
    /// <summary>
    /// Displays the results for all players after the NGO minigame.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))] // TODO: Include elsewhere?
    public class ResultsUserUI : NetworkBehaviour
    {
        [Tooltip("The containers for the player data outputs, in order, to be hidden until the game ends.")]
        [SerializeField] private CanvasGroup[] m_containers;
        [Tooltip("These should be in order of appearance, i.e. the 0th entry is the 1st-place player, and so on.")]
        [SerializeField] private TMPro.TMP_Text[] m_playerNameOutputs;
        [Tooltip("These should also be in order of appearance.")]
        [SerializeField] private TMPro.TMP_Text[] m_playerScoreOutputs;
        private int m_index = 0;

        public void Start()
        {
            foreach (var container in m_containers)
                container.alpha = 0;
        }

        // Assigned to an event in the Inspector.
        public void ReceiveScoreInOrder(LobbyUserData data)
        {
            m_containers[m_index].alpha = 1;
            m_playerNameOutputs[m_index].text = data.name;
            m_playerScoreOutputs[m_index].text = data.score.ToString("00");
            m_index++;
        }
    }
}
