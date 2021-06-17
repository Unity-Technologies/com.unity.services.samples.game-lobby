using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
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
        Dictionary<string, LobbyData> m_LobbyData = new Dictionary<string, LobbyData>();

        string m_targetRoomID;
        string m_targetRoomJoinCode;

        /// <summary>Contains some amount of information used to join an existing room.</summary>
        LobbyInfo m_lobbyDataSelected;

        public void LobbyButtonSelected(LobbyData lobby)
        {
            m_lobbyDataSelected = lobby.Data;
        }

        public void OnJoinCodeInputFieldChanged(string newCode) // TODO: Needs some new UI to show that an existing room is selected without being able to show its room code.
        {
            if (!string.IsNullOrEmpty(newCode))
                m_lobbyDataSelected = new LobbyInfo(newCode);
        }

        public void OnJoinButtonPressed()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.JoinRoomRequest, m_lobbyDataSelected);
        }

        public void OnRefresh()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryRooms, null);
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
                    AddNewLobbyButton(roomCodeKey, lobbyData);
                }
                previousKeys.Remove(roomCodeKey);
            }
            foreach (string key in previousKeys) // Need to remove any lobbies from the list that no longer exist.
                RemoveLobbyButton(m_LobbyData[key]);
        }

        /// <summary>
        /// Instaniates UI element and initializes the observer with the Data
        /// </summary>
        void AddNewLobbyButton(string roomCode, LobbyData lobby)
        {
            var lobbyButtonInstance = Instantiate(m_LobbyButtonPrefab, m_RoomButtonParent);
            lobbyButtonInstance.GetComponent<LobbyDataObserver>().BeginObserving(lobby);
            lobby.onDestroyed += RemoveLobbyButton; // Set up to clean itself
            lobbyButtonInstance.onLobbyPressed.AddListener(LobbyButtonSelected);
            m_LobbyButtons.Add(roomCode, lobbyButtonInstance);
            m_LobbyData.Add(roomCode, lobby);
        }

        void RemoveLobbyButton(LobbyData lobby)
        {
            var lobbyID = lobby.RoomID;
            var lobbyButton = m_LobbyButtons[lobbyID];
            m_LobbyButtons.Remove(lobbyID);
            m_LobbyData.Remove(lobbyID);
            Destroy(lobbyButton.gameObject);
        }
    }
}
