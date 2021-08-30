using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

        [SerializeField]
        Image m_HostIcon;

        [SerializeField]
        MuteUI m_MuteUI;

        [SerializeField]
        LobbyUserVolumeUI m_lobbyUserVolumeUI;

        LobbyUserAudio m_userAudio = new LobbyUserAudio("None");

        public bool IsAssigned => UserId != null;

        public string UserId { get; private set; }
        private LobbyUserObserver m_observer;

        public void SetUser(LobbyUser myLobbyUser)
        {
            Show();
            if (m_observer == null)
                m_observer = GetComponent<LobbyUserObserver>();
            m_observer.BeginObserving(myLobbyUser);
            UserId = myLobbyUser.ID;
            m_userAudio = new LobbyUserAudio(UserId);
        }

        public void OnUserLeft()
        {
            UserId = null;
            Hide();
            m_observer.EndObserving();
        }

        public void OnMuteToggled(bool muted)
        {
            m_userAudio.Muted = muted;
            Locator.Get.Messenger.OnReceiveMessage(MessageType.SetPlayerSound, m_userAudio);
        }

        public void OnVolumeSlide(float volume)
        {
            m_userAudio.UserVolume = volume;
            Locator.Get.Messenger.OnReceiveMessage(MessageType.SetPlayerSound, m_userAudio);
        }

        public override void ObservedUpdated(LobbyUser observed)
        {
            m_DisplayNameText.SetText(observed.DisplayName);
            m_StatusText.SetText(SetStatusFancy(observed.UserStatus));
            m_EmoteText.SetText(observed.Emote.GetString());
            m_HostIcon.enabled = observed.IsHost;
            SetAudioState(observed.HasVoice);
        }

        /// <summary>
        ///  Disable or show the Volume Icons in sync with the package.
        /// </summary>
        void SetAudioState(bool hasVoice)
        {
            if (hasVoice)
            {
                m_MuteUI.EnableVoice();
                m_lobbyUserVolumeUI.EnableVoice();
            }
            else
            {
                m_MuteUI.DisableVoice();
                m_lobbyUserVolumeUI.DisableVoice();
            }
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
                    return "<color=#005500>In Game</color>"; // Green
                default:
                    return "";
            }
        }
    }
}
