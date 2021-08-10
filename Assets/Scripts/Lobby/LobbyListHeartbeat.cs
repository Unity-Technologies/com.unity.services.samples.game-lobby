using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keeps the lobby list updated automatically.
    /// </summary>
    public class LobbyListHeartbeat : MonoBehaviour
    {
        private const float k_refreshRate = 5;

        // This is called in-editor via events.
        public void SetActive(bool isActive)
        {
            if (isActive)
                Locator.Get.UpdateSlow.Subscribe(OnUpdate, k_refreshRate);
            else
                Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
        }

        private void OnUpdate(float dt)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryLobbies, null);
        }
    }
}
