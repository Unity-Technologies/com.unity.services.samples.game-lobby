using UnityEngine;
using UnityEngine.UI;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Handles the menu for a player creating a new lobby.
    /// </summary>
    public class CreateMenuUI : UIPanelBase
    {
        public Button m_CreateButton;
        public JoinCreateLobbyUI m_JoinCreateLobbyUI;
        string m_ServerName;
        string m_ServerPassword;
        bool m_IsServerPrivate;

        public override void Start()
        {
            base.Start();
            m_CreateButton.interactable = false;
            m_JoinCreateLobbyUI.m_OnTabChanged.AddListener(OnTabChanged);
        }

        void OnTabChanged(JoinCreateTabs tabState)
        {
            if (tabState == JoinCreateTabs.Create)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        public void SetServerName(string serverName)
        {
            m_ServerName = serverName;
            m_CreateButton.interactable = ValidateServerName(m_ServerName) && ValidatePassword(m_ServerPassword);
        }

        public void SetServerPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                password = null;
            m_ServerPassword = password;
            m_CreateButton.interactable = ValidatePassword(m_ServerPassword) && ValidateServerName(m_ServerName);
        }

        public void SetServerPrivate(bool priv)
        {
            m_IsServerPrivate = priv;
        }

        public void OnCreatePressed()
        {
            Manager.CreateLobby(m_ServerName, m_IsServerPrivate, m_ServerPassword);
        }

        /// <summary>
        /// Lobby Service only allows passwords greater than 8 and less than 64 characters
        /// Null is also an option, meaning No password.
        /// </summary>
        bool ValidatePassword(string password)
        {
            if (password == null)
                return true;
            var passwordLength = password.Length;
            if (passwordLength < 1)
                return true;
            return passwordLength is >= 8 and <= 64;
        }

        bool ValidateServerName(string serverName)
        {
            var serverNameLength = serverName.Length;

            return serverNameLength is > 0 and <= 64;
        }
    }
}