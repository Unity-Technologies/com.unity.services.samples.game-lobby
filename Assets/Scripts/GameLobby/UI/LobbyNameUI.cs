using TMPro;
using UnityEngine;

namespace GamelobbySample.UI
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
