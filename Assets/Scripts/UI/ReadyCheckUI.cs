using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Button callbacks for the "Ready"/"Not Ready" buttons used to indicate the local player is ready/not ready.
    /// </summary>
    public class ReadyCheckUI : MonoBehaviour
    {
        public void OnReadyButton()
        {
            ChangeState(UserStatus.Ready);
        }
        public void OnCancelButton()
        {
            ChangeState(UserStatus.Lobby);
        }
        private void ChangeState(UserStatus status)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.ChangeLobbyUserState, status);
        }
    }
}
