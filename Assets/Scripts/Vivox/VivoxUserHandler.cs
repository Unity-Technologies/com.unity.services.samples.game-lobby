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
            m_vivoxId = account.ToString(); //"sip:." + account.Issuer + "." + m_id + ".@" + account.Domain; 
            
            
            // TODO: This doesn't end up matching the Participants keys, since there's something else appended between id and domain.


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
                // TODO: Verify non-null things?
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
