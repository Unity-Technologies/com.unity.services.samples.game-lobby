using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    public class HostReadyCheckUI : MonoBehaviour
    {
        public void InitReadyCheck(bool init)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.HostInitReadyCheck, init);
        }
    }
}
