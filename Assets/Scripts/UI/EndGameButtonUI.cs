using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// After connecting to Relay, the host can use this to end the game, returning to the regular lobby state.
    /// </summary>
    public class EndGameButtonUI : MonoBehaviour
    {
        public void EndGame()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ToLobby, null);
        }
    }
}
