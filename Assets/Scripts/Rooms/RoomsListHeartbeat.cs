using UnityEngine;
using Utilities;

namespace LobbyRooms
{
    /// <summary>
    /// Keeps the rooms list updated automatically.
    /// TODO: This should only be active while the join menu is visible. Also remove the OnVisibilityChange event when a better solution is introduced.
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
