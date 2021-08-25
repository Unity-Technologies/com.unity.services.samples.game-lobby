using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class MuteUI : UIPanelBase
    {
        [SerializeField]
        CanvasGroup m_voiceRings;

        public void EnableVoice()
        {
            Show();
        }

        public void DisableVoice()
        {
            Hide(1);
        }
        
        /// <summary>
        /// Controls the visibility of the volume rings to show activity levels of the voice channel on this user.
        /// </summary>
        /// <param name="normalizedVolume"></param>
        public void OnSoundDetected(float normalizedVolume)
        {
            m_voiceRings.alpha = normalizedVolume;
        }
        
    }
}
