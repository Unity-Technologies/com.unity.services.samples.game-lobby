using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Handles the list of LobbyButtons and ensures it stays synchronized with the lobby list from the service.
    /// </summary>
    public class JoinMenuUI : UIPanelBase
    {
        [SerializeField] LobbyEntryUI m_LobbyEntryPrefab;

        [SerializeField] RectTransform m_LobbyButtonParent;

        [SerializeField] TMP_InputField m_JoinCodeField;

        public JoinCreateLobbyUI m_JoinCreateLobbyUI;
        /// <summary>
        /// Key: Lobby ID, Value Lobby UI
        /// </summary>
        Dictionary<string, LobbyEntryUI> m_LobbyButtons = new Dictionary<string, LobbyEntryUI>();

        /// <summary>Contains some amount of information used to join an existing lobby.</summary>
        LocalLobby m_LocalLobbySelected;
        string m_InputLobbyCode;
        string m_InputLobbyPwd;

        public override void Start()
        {
            base.Start();
            m_JoinCreateLobbyUI.m_OnTabChanged.AddListener(OnTabChanged);
            Manager.LobbyList.onLobbyListChange += OnLobbyListChanged;
        }

        void OnDestroy()
        {
            if (Manager == null)
                return;
            Manager.LobbyList.onLobbyListChange -= OnLobbyListChanged;
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
            m_LocalLobbySelected = lobby;
        }

        public void OnLobbyCodeInputFieldChanged(string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
                m_InputLobbyCode = newCode.ToUpper();
        }
        
        public void OnLobbyPasswordInputFieldChanged(string newPwd)
        {
            if (!string.IsNullOrEmpty(newPwd))
                m_InputLobbyPwd = newPwd;
        }

        public void OnJoinButtonPressed()
        {
            string selectedLobbyID = null;
            if (m_LocalLobbySelected != null)
            {
                selectedLobbyID = m_LocalLobbySelected.LobbyID.Value;
            }

            Manager.JoinLobby(selectedLobbyID, m_InputLobbyCode, m_InputLobbyPwd);
            m_LocalLobbySelected = null;
        }

        public void OnRefresh()
        {
            Manager.QueryLobbies();
        }

        void OnLobbyListChanged(Dictionary<string, LocalLobby> lobbyList)
        {
            PruneMissingLobbies(lobbyList.Keys.ToList());
            PopulateLobbyButtonList(lobbyList);
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
            Manager.QuickJoin();
        }

        void PruneMissingLobbies(List<string> lobbyIDs)
        {
            var removalList = new List<string>();
            foreach (var lobbyID in lobbyIDs)
            {
                if (!lobbyIDs.Contains(lobbyID))
                    removalList.Add(lobbyID);
            }

            foreach (var lobbyID in removalList)
                RemoveLobbyButton(lobbyID);
        }

        void PopulateLobbyButtonList(Dictionary<string, LocalLobby> lobbyList)
        {
            ///Check for new entries, We take CurrentLobbies as the source of truth
            foreach (var lobbyKVP in lobbyList)
            {
                var lobbyID = lobbyKVP.Key;
                var lobby = lobbyKVP.Value;
                if (!m_LobbyButtons.ContainsKey(lobbyID))
                {
                    if (CanDisplay(lobby))
                        AddNewLobbyButton(lobbyID, lobby);
                }
                else
                {
                    if (CanDisplay(lobby))
                        SetLobbyButton(lobbyID, lobby);
                    else
                        RemoveLobbyButton(lobbyID);
                }
            }
        }

        bool CanDisplay(LocalLobby lobby)
        {
            return lobby.LocalLobbyState.Value == LobbyState.Lobby && !lobby.Private.Value;
        }

        /// <summary>
        /// Instantiates UI element and initializes the observer with the LobbyData
        /// </summary>
        void AddNewLobbyButton(string lobbyCode, LocalLobby lobby)
        {
            var lobbyButtonInstance = Instantiate(m_LobbyEntryPrefab, m_LobbyButtonParent);
            lobbyButtonInstance.onLobbyPressed.AddListener(LobbyButtonSelected);
            m_LobbyButtons.Add(lobbyCode, lobbyButtonInstance);
            lobbyButtonInstance.SetLobby(lobby);
        }

        void SetLobbyButton(string lobbyCode, LocalLobby lobby)
        {
            m_LobbyButtons[lobbyCode].SetLobby(lobby);
        }

        void RemoveLobbyButton(string lobbyID)
        {
            var lobbyButton = m_LobbyButtons[lobbyID];
            m_LobbyButtons.Remove(lobbyID);
            Destroy(lobbyButton.gameObject);
        }
    }
}