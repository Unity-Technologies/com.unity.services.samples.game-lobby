using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class LobbyUserVolumeUI : MonoBehaviour
    {
        [SerializeField]
        private UIPanelBase m_volumeSliderContainer;
        [SerializeField]
        private UIPanelBase m_muteButtonContainer;
        [SerializeField]
        [Tooltip("This is shown for other players, to mute them.")]
        private GameObject m_muteIcon;
        [SerializeField]
        [Tooltip("This is shown for the local player, to make it clearer that they are muting themselves.")]
        private GameObject m_micMuteIcon;
        public bool IsLocalPlayer { private get; set; }

        public void EnableVoice()
        {
            if (IsLocalPlayer)
            {
                m_volumeSliderContainer.Hide(0);
                m_muteButtonContainer.Show();
                m_muteIcon.SetActive(false);
                m_micMuteIcon.SetActive(true);
            }
            else
            {
                m_volumeSliderContainer.Show();
                m_muteButtonContainer.Show();
                m_muteIcon.SetActive(true);
                m_micMuteIcon.SetActive(false);
            }
        }

        public void DisableVoice()
        {
            m_volumeSliderContainer.Hide(0.4f);
            m_muteButtonContainer.Hide(0.4f);
            m_muteIcon.SetActive(true);
            m_micMuteIcon.SetActive(false);
        }

        /* TODO : If we can hook in the volume from a user, we can plug it in here.
              /// <summary>
              /// Controls the visibility of the volume rings to show activity levels of the voice channel on this user.
              /// </summary>
              /// <param name="normalizedVolume"></param>
              public void OnSoundDetected(float normalizedVolume)
              {
                  m_voiceRings.alpha = normalizedVolume;
              }
              */
    }
}
