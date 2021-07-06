using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Watches for changes in the Lobby's player List
    /// </summary>
    [RequireComponent(typeof(LocalLobbyObserver))]
    public class LobbyUsersUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        List<LobbyUserCardUI> m_PlayerCardSlots = new List<LobbyUserCardUI>();
        List<string> m_CurrentUsers = new List<string>(); // Just for keeping track more easily of which users are already displayed.

        /// <summary>
        /// When the observed data updates, we need to detect changes to the list of players.
        /// </summary>
        public override void ObservedUpdated(LocalLobby observed)
        {
            for (int id = m_CurrentUsers.Count - 1; id >= 0; id--) // We might remove users if they aren't in the new data, so iterate backwards.
            {
                string userId = m_CurrentUsers[id];
                if (!observed.LobbyUsers.ContainsKey(userId))
                {
                    foreach (var card in m_PlayerCardSlots)
                    {
                        if (card.UserId == userId)
                        {
                            card.OnUserLeft();
                            OnUserLeft(userId);
                        }
                    }
                }
            }

            foreach (var lobbyUserKvp in observed.LobbyUsers) // If there are new players, we need to hook them into the UI.
            {
                if (m_CurrentUsers.Contains(lobbyUserKvp.Key))
                    continue;
                m_CurrentUsers.Add(lobbyUserKvp.Key);

                foreach (var pcu in m_PlayerCardSlots)
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
