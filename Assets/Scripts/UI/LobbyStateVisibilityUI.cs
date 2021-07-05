using UnityEngine;

namespace LobbyRooms.UI
{
    public class LobbyStateVisibilityUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        private LobbyState m_ShowThisWhen;

        public override void ObservedUpdated(LobbyData observed)
        {
            if (m_ShowThisWhen.HasFlag(observed.State))
                Show();
            else
                Hide();
        }
    }
}

