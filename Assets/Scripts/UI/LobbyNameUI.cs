using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Displays the name of the lobby.
    /// </summary>
    public class LobbyNameUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        TMP_Text m_lobbyNameText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            m_lobbyNameText.SetText(observed.LobbyName);
        }
    }
}
