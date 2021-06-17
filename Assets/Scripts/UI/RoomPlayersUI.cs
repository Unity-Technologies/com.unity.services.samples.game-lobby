using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobbyRooms.UI
{
    /// <summary>
    /// Watches for changes in the Lobby's player List
    /// </summary>
    [RequireComponent(typeof(LobbyDataObserver))]
    public class RoomPlayersUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        List<PlayerCardUI> m_PlayerCardSlots = new List<PlayerCardUI>();
        List<string> m_CurrentUsers = new List<string>(); // Just for keeping track more easily of which users are already displayed.

        /// <summary>
        /// When the observed data updates, we need to detect changes to the list of players.
        /// </summary>
        public override void ObservedUpdated(LobbyData observed)
        {
          //  if (observed.PlayerCount == m_CurrentUsers.Count) // TODO: Not a 100% accurate shorthand.
            //    return;

            for (int id = m_CurrentUsers.Count - 1; id >= 0; id--) // We might remove users if they aren't in the new data, so iterate backwards.
            {
                string userId = m_CurrentUsers[id];
                if (!observed.LobbyUsers.ContainsKey(userId))
                {   foreach (var card in m_PlayerCardSlots)
                    {   if (card.UserId == userId)
                        {
                            card.OnUserLeft(userId);
                            OnPlayerLeft(userId);
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
                    var lobbyUser = lobbyUserKvp.Value;
                    lobbyUser.onDestroyed += OnPlayerLeft; // TODO: Where do we unsubscribe?
                    pcu.SetUser(lobbyUserKvp.Value);
                    break;
                }
            }
        }

        void OnPlayerLeft(LobbyUser left)
        {
            OnPlayerLeft(left.ID);
        }
        void OnPlayerLeft(string playerId) // TODO: We seem to waffle between player and user.
        {
            if (!m_CurrentUsers.Contains(playerId))
                return;
            m_CurrentUsers.Remove(playerId);
        }
    }
}
