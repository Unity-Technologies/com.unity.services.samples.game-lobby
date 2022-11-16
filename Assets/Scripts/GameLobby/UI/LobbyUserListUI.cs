using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class LobbyUserListUI : UIPanelBase
    {
        [SerializeField]
        List<InLobbyUserUI> m_UserUIObjects = new List<InLobbyUserUI>();


        public override void Start()
        {
            base.Start();

            GameManager.Instance.LocalLobby.onUserJoined += OnUserJoined;
            GameManager.Instance.LocalLobby.onUserLeft += OnUserLeft;
        }

        void OnUserJoined(LocalPlayer localPlayer)
        {
            var lobbySlot = m_UserUIObjects[localPlayer.Index.Value];

            lobbySlot.SetUser(localPlayer);
        }

        void OnUserLeft(int i)
        {
            m_UserUIObjects[i].ResetUI();
        }


    }
}
