using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    public class UserReadyCheckUI : MonoBehaviour
    {
        public void Ready(bool isReady)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.LocalUserReadyCheckResponse, isReady);
        }
    }
}
