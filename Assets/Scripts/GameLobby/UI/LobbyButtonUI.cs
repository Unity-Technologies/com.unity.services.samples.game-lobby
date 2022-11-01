using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls an entry in the join menu's list of lobbies, acting as a clickable button as well as displaying info about the lobby.
    /// </summary>

    //TODO WHAT WAS THIS OBSERVING??!?
    public class LobbyButtonUI : MonoBehaviour
    {
        [SerializeField]
        TMP_Text lobbyNameText;
        [SerializeField]
        TMP_Text lobbyCountText;

        /// <summary>
        /// Subscribed to on instantiation to pass our lobby data back
        /// </summary>
        public UnityEvent<LocalLobby> onLobbyPressed;
        LocalLobbyObserver m_DataObserver;
        LocalLobby m_Lobby;

        /// <summary>
        /// UI CallBack
        /// </summary>
        public void OnLobbyClicked()
        {
            //TODO Select Lobby
        }

        public void SetLobby(LocalLobby lobby)
        {
            m_Lobby = lobby;
            lobbyNameText.SetText(m_Lobby.LobbyName.Value);
            lobbyCountText.SetText($"{m_Lobby.PlayerCount}/{m_Lobby.MaxPlayerCount}");
        }
    }
}