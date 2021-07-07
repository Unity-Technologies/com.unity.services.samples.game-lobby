using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Watches a lobby or relay code for updates, displaying the current code to lobby members.
    /// </summary>
    public class DisplayCodeUI : ObserverPanel<LocalLobby>
    {
        public enum CodeType { Lobby = 0, Relay = 1 }

        [SerializeField]
        TMP_InputField m_outputText;
        [SerializeField]
        CodeType m_codeType;

        public override void ObservedUpdated(LocalLobby observed)
        {
            string code = m_codeType == CodeType.Lobby ? observed.LobbyCode : observed.RelayCode;

            if (!string.IsNullOrEmpty(code))
            {
                m_outputText.text = code;
                Show();
            }
            else
            {
                Hide();
            }
        }
    }
}
