using System;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// UI element that is displayed when the lobby is in a particular state (e.g. counting down, in-game).
    /// </summary>
    public class ShowWhenLobbyStateUI : UIPanelBase
    {
        [SerializeField]
        LobbyState m_ShowThisWhen;

        public void LobbyChanged(LobbyState lobbyState)
        {
            if (m_ShowThisWhen.HasFlag(lobbyState))
                Show();
            else
                Hide();
        }

        public override void Start()
        {
            base.Start();
            Manager.LocalLobby.LocalLobbyState.onChanged += LobbyChanged;
        }
        
    }
}
