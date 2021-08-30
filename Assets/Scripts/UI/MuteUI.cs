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
            Hide(0.4f);
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
