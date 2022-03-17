using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// For navigating the main menu.
    /// </summary>
    public class BackButtonUI : MonoBehaviour
    {
        public void ToJoinMenu()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.JoinMenu);
        }

        public void ToMenu()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeMenuState, GameState.Menu);
        }
    }
}
