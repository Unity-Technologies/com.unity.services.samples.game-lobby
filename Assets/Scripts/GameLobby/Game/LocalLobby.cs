using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobbyRelaySample
{
    [Flags] // Some UI elements will want to specify multiple states in which to be active, so this is Flags.
    public enum LobbyState
    {
        Lobby = 1,
        CountDown = 2,
        InGame = 4
    }

    public enum LobbyColor
    {
        None = 0,
        Orange = 1,
        Green = 2,
        Blue = 3
    }

    /// <summary>
    /// A local wrapper around a lobby's remote data, with additional functionality for providing that data to UI elements and tracking local player objects.
    /// (The way that the Lobby service handles its data doesn't necessarily match our needs, so we need to map from that to this LocalLobby for use in the sample code.)
    /// </summary>
    [System.Serializable]
    public class LocalLobby : Observed<LocalLobby>
    {
        Dictionary<string, LobbyUser> m_LobbyUsers = new Dictionary<string, LobbyUser>();
        public Dictionary<string, LobbyUser> LobbyUsers => m_LobbyUsers;
        public bool changedByLobbySynch;
        #region LocalLobbyData

        public struct LobbyData
        {
            public string LobbyID { get; set; }
            public string LobbyCode { get; set; }
            public string RelayCode { get; set; }
            public string RelayNGOCode { get; set; }
            public string LobbyName { get; set; }
            public bool Private { get; set; }
            public bool Locked { get; set; }
            public int AvailableSlots { get; set; }
            public int MaxPlayerCount { get; set; }
            public LobbyState State { get; set; }
            public LobbyColor Color { get; set; }
            public long State_LastEdit { get; set; }
            public long Color_LastEdit { get; set; }
            public long RelayNGOCode_LastEdit { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayCode = existing.RelayCode;
                RelayNGOCode = existing.RelayNGOCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
                State = existing.State;
                Color = existing.Color;
                State_LastEdit = existing.State_LastEdit;
                Color_LastEdit = existing.Color_LastEdit;
                RelayNGOCode_LastEdit = existing.RelayNGOCode_LastEdit;
                AvailableSlots = existing.AvailableSlots;
                Locked = existing.Locked;
            }

            public LobbyData(string lobbyCode)
            {
                LobbyID = null;
                LobbyCode = lobbyCode;
                RelayCode = null;
                RelayNGOCode = null;
                LobbyName = null;
                Private = false;
                MaxPlayerCount = -1;
                State = LobbyState.Lobby;
                Color = LobbyColor.None;
                State_LastEdit = 0;
                Color_LastEdit = 0;
                RelayNGOCode_LastEdit = 0;
                AvailableSlots = 4;
                Locked = false;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder("Lobby : ");
                sb.AppendLine(LobbyName);
                sb.Append("ID: ");
                sb.AppendLine(LobbyID);
                sb.Append("Code: ");
                sb.AppendLine(LobbyCode);
                sb.Append("Private: ");
                sb.AppendLine(Private.ToString());
                sb.Append("Locked: ");
                sb.AppendLine(Locked.ToString());
                sb.Append("Max Players: ");
                sb.AppendLine(MaxPlayerCount.ToString());
                sb.Append("AvailableSlots: ");
                sb.AppendLine(AvailableSlots.ToString());
                sb.Append("LobbyState: ");
                sb.AppendLine(State.ToString());
                sb.Append("Lobby State Last Edit: ");
                sb.AppendLine(new DateTime(State_LastEdit).ToString());
                sb.Append("LobbyColor: ");
                sb.AppendLine(Color.ToString());
                sb.Append("Color Last Edit: ");
                sb.AppendLine(new DateTime(Color_LastEdit).ToString());
                sb.Append("RelayCode: ");
                sb.AppendLine(RelayCode);
                sb.Append("RelayNGO: ");
                sb.AppendLine(RelayNGOCode);
                sb.Append("Relay NGO last Edit: ");
                sb.AppendLine(new DateTime(RelayNGOCode_LastEdit).ToString());
                return sb.ToString();
            }
        }

        public LobbyData Data => m_Data;
        LobbyData m_Data;

        ServerAddress m_RelayServer;

        /// <summary>Used only for visual output of the Relay connection info. The obfuscated Relay server IP is obtained during allocation in the RelayUtpSetup.</summary>
        public ServerAddress RelayServer
        {
            get => m_RelayServer;
            set
            {
                m_RelayServer = value;
                OnChanged(this);
            }
        }

        #endregion

        public void AddPlayer(LobbyUser user)
        {
            if (m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogError($"Cant add player {user.DisplayName}({user.ID}) to lobby: {LobbyID} twice");
                return;
            }

            AddUser(user);
            OnChanged(this);
        }

        void AddUser(LobbyUser user)
        {
            m_LobbyUsers.Add(user.ID, user);
            user.onChanged += OnChangedUser;
        }

        public void RemovePlayer(LobbyUser user)
        {
            DoRemoveUser(user);
            OnChanged(this);
        }

        private void DoRemoveUser(LobbyUser user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
            user.onChanged -= OnChangedUser;
        }

        private void OnChangedUser(LobbyUser user)
        {
            OnChanged(this);
        }

        public string LobbyID
        {
            get => m_Data.LobbyID;
            set
            {
                m_Data.LobbyID = value;
                OnChanged(this);
            }
        }

        public string LobbyCode
        {
            get => m_Data.LobbyCode;
            set
            {
                m_Data.LobbyCode = value;
                OnChanged(this);
            }
        }

        public string RelayCode
        {
            get => m_Data.RelayCode;
            set
            {
                m_Data.RelayCode = value;
                OnChanged(this);
            }
        }

        public string RelayNGOCode
        {
            get => m_Data.RelayNGOCode;
            set
            {
                m_Data.RelayNGOCode = value;
                m_Data.RelayNGOCode_LastEdit = DateTime.Now.Ticks;
                OnChanged(this);
            }
        }

        public string LobbyName
        {
            get => m_Data.LobbyName;
            set
            {
                m_Data.LobbyName = value;
                OnChanged(this);
            }
        }

        public LobbyState State
        {
            get => m_Data.State;
            set
            {
                m_Data.State = value;
                m_Data.State_LastEdit = DateTime.Now.Ticks;
                OnChanged(this);
            }
        }

        public bool Private
        {
            get => m_Data.Private;
            set
            {
                m_Data.Private = value;
                OnChanged(this);
            }
        }

        public int PlayerCount => m_LobbyUsers.Count;

        public int MaxPlayerCount
        {
            get => m_Data.MaxPlayerCount;
            set
            {
                m_Data.MaxPlayerCount = value;
                OnChanged(this);
            }
        }

        public LobbyColor Color
        {
            get => m_Data.Color;
            set
            {
                if (m_Data.Color != value)
                {
                    m_Data.Color = value;
                    m_Data.Color_LastEdit = DateTime.Now.Ticks;
                    OnChanged(this);
                }
            }
        }

        public void CopyObserved(LobbyData lobbyData, Dictionary<string, LobbyUser> lobbyUsers)
        {
            // It's possible for the host to edit the lobby in between the time they last pushed lobby data and the time their pull for new lobby data completes.
            // If that happens, the edit will be lost, so instead we maintain the time of last edit to detect that case.
            var pendingState = lobbyData.State;
            var pendingColor = lobbyData.Color;
            var pendingNgoCode = lobbyData.RelayNGOCode;
            if (m_Data.State_LastEdit > lobbyData.State_LastEdit)
                pendingState = m_Data.State;
            if (m_Data.Color_LastEdit > lobbyData.Color_LastEdit)
                pendingColor = m_Data.Color;
            if (m_Data.RelayNGOCode_LastEdit > lobbyData.RelayNGOCode_LastEdit)
                pendingNgoCode = m_Data.RelayNGOCode;
            m_Data = lobbyData;
            m_Data.State = pendingState;
            m_Data.Color = pendingColor;
            m_Data.RelayNGOCode = pendingNgoCode;

            if (lobbyUsers == null)
                m_LobbyUsers = new Dictionary<string, LobbyUser>();
            else
            {
                List<LobbyUser> toRemove = new List<LobbyUser>();
                foreach (var oldUser in m_LobbyUsers)
                {
                    if (lobbyUsers.ContainsKey(oldUser.Key))
                        oldUser.Value.CopyObserved(lobbyUsers[oldUser.Key]);
                    else
                        toRemove.Add(oldUser.Value);
                }

                foreach (var remove in toRemove)
                {
                    DoRemoveUser(remove);
                }

                foreach (var currUser in lobbyUsers)
                {
                    if (!m_LobbyUsers.ContainsKey(currUser.Key))
                        AddUser(currUser.Value);
                }
            }

            OnChanged(this);
        }

        // This ends up being called from the lobby list when we get data about a lobby without having joined it yet.
        public override void CopyObserved(LocalLobby oldObserved)
        {
            CopyObserved(oldObserved.Data, oldObserved.m_LobbyUsers);
        }
    }
}
