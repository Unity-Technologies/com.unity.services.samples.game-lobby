using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class LobbyUserListUI : UIPanelBase
    {
        [SerializeField]
        List<InLobbyUserUI> m_UserUIObjects = new List<InLobbyUserUI>();
        List<string>
            m_CurrentUsers =
                new List<string>(); // Just for keeping track more easily of which users are already displayed.

        public override void Start()
        {
            base.Start();

            GameManager.Instance.LocalLobby.onUserListChanged += OnUsersChanged;
        }

        void OnUsersChanged(Dictionary<string, LocalPlayer> newUserDict)
        {
            for (int id = m_CurrentUsers.Count - 1;
                id >= 0;
                id--) // We might remove users if they aren't in the new data, so iterate backwards.
            {
                string userId = m_CurrentUsers[id];
                if (!newUserDict.ContainsKey(userId))
                {
                    foreach (var ui in m_UserUIObjects)
                    {
                        if (ui.UserId == userId)
                        {
                            ui.OnUserLeft();
                            OnUserLeft(userId);
                        }
                    }
                }
            }

            foreach (var lobbyUserKvp in newUserDict) // If there are new players, we need to hook them into the UI.
            {
                if (m_CurrentUsers.Contains(lobbyUserKvp.Key))
                    continue;
                m_CurrentUsers.Add(lobbyUserKvp.Key);

                foreach (var pcu in m_UserUIObjects)
                {
                    if (pcu.IsAssigned)
                        continue;
                    pcu.SetUser(lobbyUserKvp.Value);
                    break;
                }
            }
        }

        void OnUserLeft(string userID)
        {
            if (!m_CurrentUsers.Contains(userID))
                return;
            m_CurrentUsers.Remove(userID);
        }
    }
}
