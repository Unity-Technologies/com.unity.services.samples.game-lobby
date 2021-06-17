using System.Collections;
using System.Collections.Generic;
using LobbyRooms.UI;
using TMPro;
using UnityEngine;
using Utilities;

namespace LobbyRooms
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
