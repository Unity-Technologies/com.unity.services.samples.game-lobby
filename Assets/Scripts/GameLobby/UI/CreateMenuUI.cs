using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Handles the menu for a player creating a new lobby.
    /// </summary>
    public class CreateMenuUI : UIPanelBase
    {
        public JoinCreateLobbyUI m_JoinCreateLobbyUI;
        string m_ServerName;
        string m_ServerPassword;
        bool m_IsServerPrivate;

        public override void Start()
        {
            base.Start();
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
        }
        
        public void SetServerPassword(string password)
        {
            m_ServerPassword = password;
        }

        public void SetServerPrivate(bool priv)
        {
            m_IsServerPrivate = priv;
        }

        public void OnCreatePressed()
        {
            Manager.CreateLobby(m_ServerName, m_IsServerPrivate, m_ServerPassword);
        }
    }
}