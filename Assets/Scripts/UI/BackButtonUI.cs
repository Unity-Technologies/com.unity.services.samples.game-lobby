using System.Collections;
using System.Collections.Generic;
using LobbyRooms;
using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    public class BackButtonUI : MonoBehaviour
    {
        public void ToMenu()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.Menu);
        }
    }
}
