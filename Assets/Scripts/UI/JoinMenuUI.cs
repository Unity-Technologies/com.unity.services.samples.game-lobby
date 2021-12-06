using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Handles the list of LobbyButtons and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class JoinMenuUI : ObserverPanel<LobbyServiceData>
    {
        [SerializeField]
        LobbyButtonUI m_LobbyButtonPrefab;

        [SerializeField]
        RectTransform m_LobbyButtonParent;

        [SerializeField]
        TMP_InputField m_JoinCodeField;

        public JoinCreateLobbyUI m_JoinCreateLobbyUI;
        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        Dictionary<string, LobbyButtonUI> m_LobbyButtons = new Dictionary<string, LobbyButtonUI>();
        Dictionary<string, LocalLobby> m_LocalLobby = new Dictionary<string, LocalLobby>();

        /// <summary>Contains some amount of information used to join an existing lobby.</summary>
        LocalLobby.LobbyData m_LocalLobbySelected;

        public override void Start()
        {
            base.Start();
            m_JoinCreateLobbyUI.m_OnTabChanged.AddListener(OnTabChanged);
        }

        void OnTabChanged(JoinCreateTabs tabState)
        {
            if (tabState == JoinCreateTabs.Join)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        public void LobbyButtonSelected(LocalLobby lobby)
        {
            m_LocalLobbySelected = lobby.Data;
        }

        public void OnLobbyCodeInputFieldChanged(string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
                m_LocalLobbySelected = new LocalLobby.LobbyData(newCode.ToUpper());
        }

        public void OnJoinButtonPressed()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.JoinLobbyRequest, m_LocalLobbySelected);
            m_LocalLobbySelected = default;
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
                var lobbyCodeKey = codeLobby.Key;
                var lobbyData = codeLobby.Value;
                if (!m_LobbyButtons.ContainsKey(lobbyCodeKey))
                {
                    if (CanDisplay(lobbyData))
                        AddNewLobbyButton(lobbyCodeKey, lobbyData);
                }
                else
                {
                    if (CanDisplay(lobbyData))
                        UpdateLobbyButton(lobbyCodeKey, lobbyData);
                    else
                        RemoveLobbyButton(lobbyData);
                }

                previousKeys.Remove(lobbyCodeKey);
            }

            foreach (string key in previousKeys) // Need to remove any lobbies from the list that no longer exist.
                RemoveLobbyButton(m_LocalLobby[key]);
        }

        public void JoinMenuChangedVisibility(bool show)
        {
            if (show)
            {
                m_JoinCodeField.text = "";
                OnRefresh();
            }
        }

        public void OnQuickJoin()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.QuickJoin, null);
        }

        private bool CanDisplay(LocalLobby lobby)
        {
            return lobby.Data.State == LobbyState.Lobby && !lobby.Private;
        }

        /// <summary>
        /// Instantiates UI element and initializes the observer with the LobbyData
        /// </summary>
        private void AddNewLobbyButton(string lobbyCode, LocalLobby lobby)
        {
            var lobbyButtonInstance = Instantiate(m_LobbyButtonPrefab, m_LobbyButtonParent);
            lobbyButtonInstance.GetComponent<LocalLobbyObserver>().BeginObserving(lobby);
            lobby.onDestroyed += RemoveLobbyButton; // Set up to clean itself
            lobbyButtonInstance.onLobbyPressed.AddListener(LobbyButtonSelected);
            m_LobbyButtons.Add(lobbyCode, lobbyButtonInstance);
            m_LocalLobby.Add(lobbyCode, lobby);
        }

        private void UpdateLobbyButton(string lobbyCode, LocalLobby lobby)
        {
            m_LobbyButtons[lobbyCode].UpdateLobby(lobby);
        }

        private void RemoveLobbyButton(LocalLobby lobby)
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
