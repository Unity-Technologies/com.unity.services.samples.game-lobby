using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class CreateMenuUI : UIPanelBase
    {
        [SerializeField]
        LobbyData m_ServerRequestData = new LobbyData { LobbyName = "New Lobby", MaxPlayerCount = 4 };

        public void SetServerName(string serverName)
        {
            m_ServerRequestData.LobbyName = serverName;
        }

        public void SetServerPrivate(bool priv)
        {
            m_ServerRequestData.Private = priv;
        }

        public void OnCreatePressed()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.CreateRoomRequest, m_ServerRequestData);
        }
    }
}
