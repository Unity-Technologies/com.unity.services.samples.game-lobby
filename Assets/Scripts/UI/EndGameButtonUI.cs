using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class EndGameButtonUI : MonoBehaviour
    {
        public void EndServer()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ToLobby, null);
        }
    }
}
