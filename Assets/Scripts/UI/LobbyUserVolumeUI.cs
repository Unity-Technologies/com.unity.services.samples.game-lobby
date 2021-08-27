using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class LobbyUserVolumeUI : UIPanelBase
    {
        public void EnableVoice()
        {
            Show();
        }

        public void DisableVoice()
        {
            Hide();
        }
    }


}
