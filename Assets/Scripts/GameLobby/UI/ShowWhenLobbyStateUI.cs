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

        public void LobbyChanged(LocalLobby lobby)
        {
            if (m_ShowThisWhen.HasFlag(lobby.LobbyState))
                Show();
            else
                Hide();
        }

        public override void Start()
        {
            base.Start();
            Manager.LocalLobby.onLobbyChanged += LobbyChanged;
        }

        public void OnDestroy()
        {
            if (GameManager.Instance == null)
                return;
            Manager.LocalLobby.onLobbyChanged -= LobbyChanged;
        }
    }
}