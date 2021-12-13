using System;
using UnityEngine;
using UnityEngine.Events;

namespace LobbyRelaySample.UI
{
    public enum JoinCreateTabs
    {
        Join,
        Create
    }

    /// <summary>
    /// The panel that holds the lobby joining and creation panels.
    /// </summary>
    public class JoinCreateLobbyUI : ObserverPanel<LocalGameState>
    {
        public UnityEvent<JoinCreateTabs> m_OnTabChanged;

        [SerializeField] //Serialized for Visisbility in Editor
        JoinCreateTabs m_CurrentTab = JoinCreateTabs.Join;

        public JoinCreateTabs CurrentTab
        {
            get => m_CurrentTab;
            set
            {
                m_CurrentTab = value;
                m_OnTabChanged?.Invoke(m_CurrentTab);
            }
        }

        public void SetJoinTab()
        {
            CurrentTab = JoinCreateTabs.Join;
        }

        public void SetCreateTab()
        {
            CurrentTab = JoinCreateTabs.Create;
        }

        public override void ObservedUpdated(LocalGameState observed)
        {
            if (observed.State == GameState.JoinMenu)
            {
                m_OnTabChanged?.Invoke(m_CurrentTab);
                Show(false);
            }
            else
            {
                Hide();
            }
        }
    }
}
