using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    public class EndGameButtonUI : MonoBehaviour
    {
        public void EndServer()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ToLobby, null);
        }
    }
}
