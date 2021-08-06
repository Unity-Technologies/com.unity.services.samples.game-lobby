using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Keeps the lobby list updated automatically.
    /// </summary>
    public class LobbyListHeartbeat : MonoBehaviour
    {
        private const float k_refreshRate = 5;
        private float m_refreshTimer = 0;

        // This is called in-editor via events.
        public void SetActive(bool isActive)
        {
            if (isActive)
                Locator.Get.UpdateSlow.Subscribe(OnUpdate);
            else
                Locator.Get.UpdateSlow.Unsubscribe(OnUpdate);
            m_refreshTimer = 0;
        }

        private void OnUpdate(float dt)
        {
            m_refreshTimer += dt;
            if (m_refreshTimer > k_refreshRate)
            {
                Locator.Get.Messenger.OnReceiveMessage(MessageType.QueryLobbies, null);
                m_refreshTimer = 0;
            }
        }
    }
}
