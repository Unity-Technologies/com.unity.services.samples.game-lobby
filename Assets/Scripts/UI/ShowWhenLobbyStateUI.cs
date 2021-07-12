using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// UI element that is displayed when the lobby is in a particular state (e.g. counting down, in-game).
    /// </summary>
    public class ShowWhenLobbyStateUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        private LobbyState m_ShowThisWhen;

        public override void ObservedUpdated(LocalLobby observed)
        {
            if (m_ShowThisWhen.HasFlag(observed.State))
                Show();
            else
                Hide();
        }
    }
}

