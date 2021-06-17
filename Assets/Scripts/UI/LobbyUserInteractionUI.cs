using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    public class LobbyUserInteractionUI : UIPanelBase
    {
     
        public void HostInitReadyCheck()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.HostInitReadyCheck, null);
        }

       
    }
}
