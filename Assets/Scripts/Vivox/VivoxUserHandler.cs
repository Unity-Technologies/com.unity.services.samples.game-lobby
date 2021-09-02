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
        private UI.MuteUI m_MuteUI;
        [SerializeField]
        private UI.LobbyUserVolumeUI m_lobbyUserVolumeUI;

        private IChannelSession m_channelSession;
        private string m_id;
        private string m_vivoxId;

        private const int k_volumeMin = -50, k_volumeMax = 20; // From the Vivox docs, the valid range is [-50, 50] but anything above 25 risks being painfully loud.

        public void SetId(string id)
        {
            m_id = id;
            Account account = new Account(id);
            // Vivox appends additional info to the ID we provide, in order to associate it with a specific channel. We'll construct m_vivoxId to match the ID used by Vivox.
            // FUTURE: This isn't yet available. When using Auth, the Vivox ID will match this format:
            // m_vivoxId = $"sip:.{account.Issuer}.{m_id}.{environmentId}.@{account.Domain}";
            // However, the environment ID from Auth is not exposed anywhere, and Vivox doesn't provide a way to retrieve the ID, either.
            // Instead, when needed, we'll search for the Vivox ID containing this user's Auth ID, which is a GUID so collisions are extremely unlikely.
            // In the future, remove FindVivoxId and pass the environment ID here instead.
            m_vivoxId = null;
        }
        private void FindVivoxId()
        {
            if (m_vivoxId != null || m_channelSession == null)
                return;
            foreach (var participant in m_channelSession.Participants)
            {
                if (!participant.Key.Contains(m_id))
                    continue;
                m_vivoxId = participant.Key;
                return;
            }
        }

        public void OnChannelJoined(IChannelSession channelSession)
        {
            m_channelSession = channelSession;
        }

        public void OnChannelLeft()
        {
            m_channelSession = null;
        }

        public void OnVolumeSlide(float volumeNormalized)
        {
            FindVivoxId();
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
            FindVivoxId();
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
