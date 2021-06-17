using Player;
using TMPro;
using UnityEngine;

namespace LobbyRooms.UI
{
    /// <summary>
    /// Shows the player info in-lobby and game
    /// </summary>
    [RequireComponent(typeof(LobbyUserObserver))]
    public class PlayerCardUI : ObserverPanel<LobbyUser>
    {
        [SerializeField]
        TMP_Text m_DisplayNameText;

        [SerializeField]
        TMP_Text m_StatusText;

        [SerializeField]
        TMP_Text m_EmoteText;

        public bool IsAssigned { get { return UserId != null; } }
        public string UserId { get; private set; }

        public void SetUser(LobbyUser myLobbyUser)
        {
            Show();
            myLobbyUser.onDestroyed += OnUserLeft;
            GetComponent<LobbyUserObserver>().BeginObserving(myLobbyUser);
            UserId = myLobbyUser.ID;
        }

        public void OnUserLeft(LobbyUser user)
        {
            OnUserLeft(user?.ID);
        }
        public void OnUserLeft(string userId)
        {
            if (userId == UserId)
            {
                UserId = null;
                Hide();
            }
        }

        public override void ObservedUpdated(LobbyUser observed)
        {
            m_DisplayNameText.SetText(observed.DisplayName);
            m_StatusText.SetText(SetStatusFancy(observed.UserStatus));
            m_EmoteText.SetText(observed.Emote);
        }

        string SetStatusFancy(UserStatus status)
        {
            switch (status)
            {
                case UserStatus.Lobby:
                    return "<color=#56B4E9>Lobby.</color>"; // Light Blue
                case UserStatus.ReadyCheck:
                    return "<color=#E69F00>Ready?</color>"; //Light Orange
                case UserStatus.Ready:
                    return "<color=#009E73>Ready!</color>"; // Light Mint
                case UserStatus.Connecting:
                    return "<color=#F0E442>Connecting.</color>"; // Bright Yellow
                case UserStatus.Connected:
                    return "<color=#005500>Connected.</color>"; //Orange
                default:
                    return "<color=#56B4E9>In Lobby.</color>";
            }
        }
    }
}
