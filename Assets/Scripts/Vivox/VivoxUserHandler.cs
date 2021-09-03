using UnityEngine;
using Unity.Services.Vivox;
using VivoxUnity;

namespace LobbyRelaySample.vivox
{
    /// <summary>
    /// Listens for changes to Vivox state for one user in the lobby.
    /// Instead of going through Relay, this will listen to the Vivox service since it will already transmit state changes for all clients.
    /// </summary>
    public class VivoxUserHandler : MonoBehaviour
    {
        [SerializeField]
        private UI.LobbyUserVolumeUI m_lobbyUserVolumeUI;

        private IChannelSession m_channelSession;
        private string m_id;
        private string m_vivoxId;

        private const int k_volumeMin = -50, k_volumeMax = 20; // From the Vivox docs, the valid range is [-50, 50] but anything above 25 risks being painfully loud.

        public void Start()
        {
            m_lobbyUserVolumeUI.DisableVoice();
        }

        public void SetId(string id)
        {
            m_id = id;
            // Vivox appends additional info to the ID we provide, in order to associate it with a specific channel. We'll construct m_vivoxId to match the ID used by Vivox.
            // FUTURE: This isn't yet available. When using Auth, the Vivox ID will match this format:
            // Account account = new Account(id);
            // m_vivoxId = $"sip:.{account.Issuer}.{m_id}.{environmentId}.@{account.Domain}";
            // However, the environment ID from Auth is not exposed anywhere, and Vivox doesn't provide a way to retrieve the ID, either.
            // Instead, when needed, we'll search for the Vivox ID containing this user's Auth ID, which is a GUID so collisions are extremely unlikely.
            // In the future, remove FindVivoxId and pass the environment ID here instead.
            m_vivoxId = null;

            // SetID might be called after we've received the IChannelSession for remote players, which would mean after OnParticipant Added. So, duplicate the VivoxID work here.
            if (m_channelSession != null)
            {
                foreach (var participant in m_channelSession.Participants)
                {
                    if (m_id == participant.Account.DisplayName)
                    {
                        m_vivoxId = participant.Key;
                        m_lobbyUserVolumeUI.IsLocalPlayer = participant.IsSelf;
                        m_lobbyUserVolumeUI.EnableVoice();
                        break;
                    }
                }
            }
        }

        public void OnChannelJoined(IChannelSession channelSession)
        {
            m_channelSession = channelSession;
            m_channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            m_channelSession.Participants.BeforeKeyRemoved += BeforeParticipantRemoved;
            m_channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
        }

        public void OnChannelLeft()
        {
            m_channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            m_channelSession.Participants.BeforeKeyRemoved -= BeforeParticipantRemoved;
            m_channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
            m_channelSession = null;
        }

        /// <summary>
        /// To be called whenever a new Participant is added to the channel, using the events from Vivox's custom dictionary.
        /// </summary>
        private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
        {
            var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
            var participant = source[keyEventArg.Key];
            var username = participant.Account.DisplayName;

            bool isThisUser = username == m_id;
            if (isThisUser)
            {   m_vivoxId = keyEventArg.Key; // Since we couldn't construct the Vivox ID earlier, retrieve it here.
                m_lobbyUserVolumeUI.IsLocalPlayer = participant.IsSelf;
                m_lobbyUserVolumeUI.EnableVoice();
            }
        }
        private void BeforeParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
        {
            var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
            var participant = source[keyEventArg.Key];
            var username = participant.Account.DisplayName;

            bool isThisUser = username == m_id;
            if (isThisUser)
            {   m_lobbyUserVolumeUI.DisableVoice();
            }
        }
        private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
        {
            var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
            var participant = source[valueEventArg.Key];
            var username = participant.Account.DisplayName;
            string property = valueEventArg.PropertyName;

            if (username == m_id)
            {
                if (property == "UnavailableCaptureDevice")
                {
                    if (participant.UnavailableCaptureDevice)
                    {   m_lobbyUserVolumeUI.DisableVoice();
                        participant.SetIsMuteForAll(m_vivoxId, true, null); // Note: If you add more places where a player might be globally muted, a state machine might be required for accurate logic.
                    }
                    else
                    {   m_lobbyUserVolumeUI.EnableVoice();
                        participant.SetIsMuteForAll(m_vivoxId, false, null);
                    }
                }
                else if (property == "IsMutedForAll")
                {
                    if (participant.IsMutedForAll)
                        m_lobbyUserVolumeUI.DisableVoice();
                    else
                        m_lobbyUserVolumeUI.EnableVoice();
                }
            }
        }

        public void OnVolumeSlide(float volumeNormalized)
        {
            if (m_channelSession == null || m_vivoxId == null) // Verify initialization, since SetId and OnChannelJoined are called at different times for local vs. remote clients.
                return;

            int vol = (int)Mathf.Clamp(k_volumeMin + (k_volumeMax - k_volumeMin) * volumeNormalized, k_volumeMin, k_volumeMax); // Clamping as a precaution; if UserVolume somehow got above 1, listeners could be harmed.
            bool isSelf = m_channelSession.Participants[m_vivoxId].IsSelf;
            if (isSelf)
            {
                VivoxService.Instance.Client.AudioInputDevices.VolumeAdjustment = vol;
            }
            else
            {
                m_channelSession.Participants[m_vivoxId].LocalVolumeAdjustment = vol;
            }
        }

        public void OnMuteToggle(bool isMuted)
        {
            if (m_channelSession == null || m_vivoxId == null)
                return;

            bool isSelf = m_channelSession.Participants[m_vivoxId].IsSelf;
            if (isSelf)
            {
                VivoxService.Instance.Client.AudioInputDevices.Muted = isMuted;
            }
            else
            {
                m_channelSession.Participants[m_vivoxId].LocalMute = isMuted;
            }
        }
    }
}
