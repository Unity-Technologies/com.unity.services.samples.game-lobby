using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Handles the list of LobbyButtons and ensures it stays synchronous with the list from the service.
    /// </summary>
    public class JoinMenuUI : ObserverPanel<LobbyServiceData>
    {
        [SerializeField]
        LobbyButtonUI m_LobbyButtonPrefab;

        [SerializeField]
        TMP_InputField m_RoomCodeField;

        [SerializeField]
        RectTransform m_RoomButtonParent;

        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        Dictionary<string, LobbyButtonUI> m_LobbyButtons = new Dictionary<string, LobbyButtonUI>();
        Dictionary<string, LocalLobby> m_LocalLobby = new Dictionary<string, LocalLobby>();

        /// <summary>Contains some amount of information used to join an existing room.</summary>
        LobbyInfo m_localLobbySelected;

        public void LobbyButtonSelected(LocalLobby lobby)
        {
            m_localLobbySelected = lobby.Data;
        }

        public void OnJoinCodeInputFieldChanged(string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
                m_localLobbySelected = new LobbyInfo(newCode.ToUpper());
        }

        public void OnJoinButtonPressed()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.JoinLobbyRequest, m_localLobbySelected);
        }

        public void OnRefresh()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryLobbies, null);
        }

        public override void ObservedUpdated(LobbyServiceData observed)
        {
            ///Check for new entries, We take CurrentLobbies as the source of truth
            List<string> previousKeys = new List<string>(m_LobbyButtons.Keys);
            foreach (var codeLobby in observed.CurrentLobbies)
            {
                var roomCodeKey = codeLobby.Key;
                var lobbyData = codeLobby.Value;
                if (!m_LobbyButtons.ContainsKey(roomCodeKey))
                {
                    if (CanDisplay(lobbyData))
                        AddNewLobbyButton(roomCodeKey, lobbyData);
                }
                else
                {
                    if (CanDisplay(lobbyData))
                        UpdateLobbyButton(roomCodeKey, lobbyData);
                    else
                        RemoveLobbyButton(lobbyData);
                }

                previousKeys.Remove(roomCodeKey);
            }

            foreach (string key in previousKeys) // Need to remove any lobbies from the list that no longer exist.
                RemoveLobbyButton(m_LocalLobby[key]);
        }

        bool CanDisplay(LocalLobby lobby)
        {
            return lobby.Data.State == LobbyState.Lobby && !lobby.Private;
        }

        /// <summary>
        /// Instantiates UI element and initializes the observer with the LobbyData
        /// </summary>
        void AddNewLobbyButton(string roomCode, LocalLobby lobby)
        {
            var lobbyButtonInstance = Instantiate(m_LobbyButtonPrefab, m_RoomButtonParent);
            lobbyButtonInstance.GetComponent<LocalLobbyObserver>().BeginObserving(lobby);
            lobby.onDestroyed += RemoveLobbyButton; // Set up to clean itself
            lobbyButtonInstance.onLobbyPressed.AddListener(LobbyButtonSelected);
            m_LobbyButtons.Add(roomCode, lobbyButtonInstance);
            m_LocalLobby.Add(roomCode, lobby);
        }

        void UpdateLobbyButton(string roomCode, LocalLobby lobby)
        {
            m_LobbyButtons[roomCode].UpdateLobby(lobby);
        }

        void RemoveLobbyButton(LocalLobby lobby)
        {
            var lobbyID = lobby.LobbyID;
            var lobbyButton = m_LobbyButtons[lobbyID];
            lobbyButton.GetComponent<LocalLobbyObserver>().EndObserving();
            m_LobbyButtons.Remove(lobbyID);
            m_LocalLobby.Remove(lobbyID);
            Destroy(lobbyButton.gameObject);
        }
    }
}
