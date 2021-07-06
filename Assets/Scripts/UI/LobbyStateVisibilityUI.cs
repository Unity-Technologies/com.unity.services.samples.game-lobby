using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class LobbyStateVisibilityUI : ObserverPanel<LocalLobby>
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

