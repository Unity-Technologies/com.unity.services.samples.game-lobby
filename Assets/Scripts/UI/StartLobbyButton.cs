using UnityEngine;

namespace LobbyRelaySample
{
    public class StartLobbyButton : MonoBehaviour
    {
        public void ToJoinMenu()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.JoinMenu);
        }
    }
}
