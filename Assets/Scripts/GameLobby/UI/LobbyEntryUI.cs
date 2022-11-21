using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls an entry in the join menu's list of lobbies, acting as a clickable button as well as displaying info about the lobby.
    /// </summary>
    public class LobbyEntryUI : MonoBehaviour
    {
        [SerializeField]
        ColorLobbyUI m_ColorLobbyUI;
        [SerializeField]
        TMP_Text lobbyNameText;
        [SerializeField]
        TMP_Text lobbyCountText;

        /// <summary>
        /// Subscribed to on instantiation to pass our lobby data back
        /// </summary>
        public UnityEvent<LocalLobby> onLobbyPressed;
        LocalLobby m_Lobby;

        /// <summary>
        /// UI CallBack
        /// </summary>
        public void OnLobbyClicked()
        {
            onLobbyPressed.Invoke(m_Lobby);
        }

        public void SetLobby(LocalLobby lobby)
        {
            m_Lobby = lobby;
            SetLobbyname(m_Lobby.LobbyName.Value);
            SetLobbyCount(m_Lobby.PlayerCount);
            m_ColorLobbyUI.SetLobby(lobby);
            m_Lobby.LobbyName.onChanged += SetLobbyname;
            m_Lobby.onUserJoined += (_) =>
            {
                SetLobbyCount(m_Lobby.PlayerCount);
            };
            m_Lobby.onUserLeft += (_) =>
            {
                SetLobbyCount(m_Lobby.PlayerCount);
            };
        }

        void SetLobbyname(string lobbyName)
        {
            lobbyNameText.SetText(m_Lobby.LobbyName.Value);
        }

        void SetLobbyCount(int count)
        {
            lobbyCountText.SetText($"{count}/{m_Lobby.MaxPlayerCount.Value}");
        }
    }
}
