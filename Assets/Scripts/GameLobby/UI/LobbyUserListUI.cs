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

            GameManager.Instance.LocalLobby.onUserListChanged += OnUsersChanged;
        }

        void OnUsersChanged(Dictionary<int, LocalPlayer> newUserDict)
        {
            for (int id = m_UserUIObjects.Count - 1;
                id >= 0;
                id--) // We might remove users if they aren't in the new data, so iterate backwards.
            {
                string userId = m_UserUIObjects[id];
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

            // If there are new players, we need to hook them into the UI.
            foreach (var lobbyUserKvp in newUserDict)
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

        void OnUserLeft(int userID)
        {
            m_UserUIObjects.RemoveAt(userID);
        }
    }
}
