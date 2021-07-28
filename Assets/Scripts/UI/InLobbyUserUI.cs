using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// When inside a lobby, this will show information about a player, whether local or remote.
    /// </summary>
    [RequireComponent(typeof(LobbyUserObserver))]
    public class InLobbyUserUI : ObserverPanel<LobbyUser>
    {
        [SerializeField]
        TMP_Text m_DisplayNameText;

        [SerializeField]
        TMP_Text m_StatusText;

        [SerializeField]
        TMP_Text m_EmoteText;

        public bool IsAssigned
        {
            get { return UserId != null; }
        }

        public string UserId { get; private set; }
        private LobbyUserObserver m_observer;

        public void SetUser(LobbyUser myLobbyUser)
        {
            Show();
            if (m_observer == null)
                m_observer = GetComponent<LobbyUserObserver>();
            m_observer.BeginObserving(myLobbyUser);
            UserId = myLobbyUser.ID;
        }

        public void OnUserLeft()
        {
            UserId = null;
            Hide();
            m_observer.EndObserving();
        }

        public override void ObservedUpdated(LobbyUser observed)
        {
            m_DisplayNameText.SetText(observed.DisplayName);
            m_StatusText.SetText(SetStatusFancy(observed.UserStatus));
            m_EmoteText.SetText(observed.Emote.GetString());
        }

        string SetStatusFancy(UserStatus status)
        {
            switch (status)
            {
                case UserStatus.Lobby:
                    return "<color=#56B4E9>In Lobby</color>"; // Light Blue
                case UserStatus.Ready:
                    return "<color=#009E73>Ready</color>"; // Light Mint
                case UserStatus.Connecting:
                    return "<color=#F0E442>Connecting...</color>"; // Bright Yellow
                case UserStatus.InGame:
                    return "<color=#005500>In Game</color>"; //Orange
                default:
                    return "";
            }
        }
    }
}
