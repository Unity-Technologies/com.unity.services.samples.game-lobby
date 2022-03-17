using UnityEngine;
using UnityEngine.UI;

namespace LobbyRelaySample.UI
{
    public class LobbyUserVolumeUI : MonoBehaviour
    {
        [SerializeField]
        private UIPanelBase m_volumeSliderContainer;
        [SerializeField]
        private UIPanelBase m_muteToggleContainer;
        [SerializeField]
        [Tooltip("This is shown for other players, to mute them.")]
        private GameObject m_muteIcon;
        [SerializeField]
        [Tooltip("This is shown for the local player, to make it clearer that they are muting themselves.")]
        private GameObject m_micMuteIcon;
        public bool IsLocalPlayer { private get; set; }

        [SerializeField]
        private Slider m_volumeSlider;
        [SerializeField]
        private Toggle m_muteToggle;

        /// <param name="shouldResetUi">
        /// When the user is being added, we want the UI to reset to the default values.
        /// (We don't do this if the user is already in the lobby so that the previous values are retained. E.g. If they're too loud and volume was lowered, keep it lowered on reenable.)
        /// </param>
        public void EnableVoice(bool shouldResetUi)
        {
            if (shouldResetUi)
            {   m_volumeSlider.SetValueWithoutNotify(vivox.VivoxUserHandler.NormalizedVolumeDefault);
                m_muteToggle.SetIsOnWithoutNotify(false);
            }

            if (IsLocalPlayer)
            {
                m_volumeSliderContainer.Hide(0);
                m_muteToggleContainer.Show();
                m_muteIcon.SetActive(false);
                m_micMuteIcon.SetActive(true);
            }
            else
            {
                m_volumeSliderContainer.Show();
                m_muteToggleContainer.Show();
                m_muteIcon.SetActive(true);
                m_micMuteIcon.SetActive(false);
            }
        }

        /// <param name="shouldResetUi">
        /// When the user leaves the lobby (but not if they just lose voice access for some reason, e.g. device disconnect), reset state to the default values.
        /// (We can't just do this during Enable since it could cause Vivox to have a state conflict during participant add.)
        /// </param>
        public void DisableVoice(bool shouldResetUi)
        {
            if (shouldResetUi)
            {   m_volumeSlider.value = vivox.VivoxUserHandler.NormalizedVolumeDefault;
                m_muteToggle.isOn = false;
            }

            m_volumeSliderContainer.Hide(0.4f);
            m_muteToggleContainer.Hide(0.4f);
            m_muteIcon.SetActive(true);
            m_micMuteIcon.SetActive(false);
        }
    }
}
