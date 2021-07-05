using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;

namespace LobbyRooms
{
    public class StartLobbyButton : MonoBehaviour
    {
        public void ToJoinMenu()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.JoinMenu);
        }
    }
}
