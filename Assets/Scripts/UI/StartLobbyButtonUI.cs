using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Main menu start button.
    /// </summary>
    public class StartLobbyButtonUI : MonoBehaviour
    {
        public void ToJoinMenu()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeGameState, GameState.JoinMenu);
        }
    }
}
