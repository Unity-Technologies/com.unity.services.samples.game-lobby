using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keeps the rooms list updated automatically.
    /// </summary>
    public class RoomsListHeartbeat : MonoBehaviour
    {
        public void SetActive(bool isActive)
        {
            if (isActive)
                Locator.Get.UpdateSlow.Subscribe(OnUpdate);
            else
                Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
        }

        private void OnUpdate(float dt)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryRooms, null);
        }
    }
}
