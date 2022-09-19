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

        public void SetServerPrivate(bool priv)
        {
            m_IsServerPrivate = priv;
        }

        public void OnCreatePressed()
        {
            //Disabled as it's a one-off butto call
#pragma warning disable 4014
            GameManager.Instance.CreateLobby(m_ServerName, m_IsServerPrivate);
#pragma warning restore 4014
        }
    }
}
