using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobbyRooms
{
    public struct LobbyInfo
    {
        public string RoomID { get; set; }
        public string RoomCode { get; set; }
        public string RelayCode { get; set; }
        public ServerAddress RelayServer { get; set; }
        public string LobbyName { get; set; }
        public bool Private { get; set; }
        public int MaxPlayerCount { get; set; }

        public LobbyInfo(LobbyInfo existing)
        {
            RoomID = existing.RoomID;
            RoomCode = existing.RoomCode;
            RelayCode = existing.RelayCode;
            RelayServer = existing.RelayServer;
            LobbyName = existing.LobbyName;
            Private = existing.Private;
            MaxPlayerCount = existing.MaxPlayerCount;
        }

        public LobbyInfo(string roomCode)
        {
            RoomID = null;
            RoomCode = roomCode;
            RelayCode = null;
            RelayServer = null;
            LobbyName = null;
            Private = false;
            MaxPlayerCount = -1;
        }
    }

    /// <summary>
    /// The local lobby data that the game can observe
    /// </summary>
    [System.Serializable]
    public class LobbyData : Observed<LobbyData>
    {
        Dictionary<string, LobbyUser> m_LobbyUsers = new Dictionary<string, LobbyUser>();
        public Dictionary<string, LobbyUser> LobbyUsers => m_LobbyUsers;

        private LobbyInfo m_data;

        public LobbyInfo Data
        {
            get { return new LobbyInfo(m_data); }
        }

        public void AddPlayer(LobbyUser user)
        {
            if (m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogError($"Cant add player {user.DisplayName}({user.ID}) to room: {RoomID} twice");
                return;
            }

            DoAddPlayer(user);
            OnChanged(this);
        }
        private void DoAddPlayer(LobbyUser user)
        {
            m_LobbyUsers.Add(user.ID, user);
            user.onChanged += OnChangedUser;
        }

        public void RemovePlayer(LobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in room: {RoomID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
            user.onChanged -= OnChangedUser;
            OnChanged(this);
        }

        private void OnChangedUser(LobbyUser user)
        {
            OnChanged(this);
        }

        public string RoomID
        {
            get => m_data.RoomID;
            set
            {
                m_data.RoomID = value;
                OnChanged(this);
            }
        }

        public string RoomCode
        {
            get => m_data.RoomCode;
            set
            {
                m_data.RoomCode = value;
                OnChanged(this);
            }
        }

        public string RelayCode
        {
            get => m_data.RelayCode;
            set
            {
                m_data.RelayCode = value;
                OnChanged(this);
            }
        }

        public ServerAddress RelayServer
        {
            get => m_data.RelayServer;
            set
            {
                m_data.RelayServer = value;
                OnChanged(this);
            }
        }

        public string LobbyName
        {
            get => m_data.LobbyName;
            set
            {
                m_data.LobbyName = value;
                OnChanged(this);
            }
        }

        public bool Private
        {
            get => m_data.Private;
            set
            {
                m_data.Private = value;
                OnChanged(this);
            }
        }

        public int PlayerCount => m_LobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => m_data.MaxPlayerCount;
            set
            {
                m_data.MaxPlayerCount = value;
                OnChanged(this);
            }
        }

        /// <summary>
        /// Checks if we have n players that have the Status.
        /// -1 Count means you need all Lobbyusers
        /// </summary>
        /// <returns>True if enough players are of the input status.</returns>
        public bool PlayersOfState(UserStatus status, int playersCount = -1)
        {
            var statePlayers = m_LobbyUsers.Values.Count(user => user.UserStatus == status);

            if (playersCount < 0)
                return statePlayers == m_LobbyUsers.Count;
            return statePlayers == playersCount;
        }

        public void CopyObserved(LobbyInfo info, Dictionary<string, LobbyUser> oldUsers)
        {
            m_data = info;
            if (oldUsers == null)
                m_LobbyUsers = new Dictionary<string, LobbyUser>();
            else
            {
                List<string> toRemove = new List<string>();
                foreach (var m_user in m_LobbyUsers)
                {
                    if (oldUsers.ContainsKey(m_user.Key))
                        m_user.Value.CopyObserved(oldUsers[m_user.Key]);
                    else
                        toRemove.Add(m_user.Key);
                }
                foreach (string remove in toRemove)
                    m_LobbyUsers.Remove(remove);
                foreach (var oldUser in oldUsers)
                {
                    if (!m_LobbyUsers.ContainsKey(oldUser.Key))
                        DoAddPlayer(oldUser.Value);
                }
            }
            OnChanged(this);
        }

        public override void CopyObserved(LobbyData oldObserved)
        {
            CopyObserved(oldObserved.Data, oldObserved.m_LobbyUsers);
        }

        public void SetAllPlayersToState(UserStatus status)
        {
            foreach (var user in LobbyUsers.Values)
            {
                user.UserStatus = status;
            }
        }

        ~LobbyData()
        {
            OnDestroy(this);
        }
    }
}
