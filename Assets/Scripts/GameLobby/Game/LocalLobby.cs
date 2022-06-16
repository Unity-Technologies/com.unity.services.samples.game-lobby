using System;
using System.Collections.Generic;
using System.Text;
using Unity.Services.Lobbies.Models;
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
        //Should be set to true when pushing lobby to the cloud, and set to false when done pulling.
        //This is because we could get more than just our changes when we receive the latest lobby from our calls.
        public bool changedByLobbySynch;
        public Action<LocalLobby> onLobbyChanged { get; set; }
        public Action<Dictionary<string, LocalPlayer>> onUserListChanged;

        Dictionary<string, LocalPlayer> m_LobbyUsers = new Dictionary<string, LocalPlayer>();
        public Dictionary<string, LocalPlayer> LobbyUsers => m_LobbyUsers;

        public Lobby CloudLobby => m_CloudLobby;
        Lobby m_CloudLobby;
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
            public LobbyState LobbyState { get; set; }
            public LobbyColor LobbyColor { get; set; }
            public long LastEdit { get; set; }

            public LobbyData(LobbyData existing)
            {
                LobbyID = existing.LobbyID;
                LobbyCode = existing.LobbyCode;
                RelayCode = existing.RelayCode;
                RelayNGOCode = existing.RelayNGOCode;
                LobbyName = existing.LobbyName;
                Private = existing.Private;
                MaxPlayerCount = existing.MaxPlayerCount;
                LobbyState = existing.LobbyState;
                LobbyColor = existing.LobbyColor;
                LastEdit = existing.LastEdit;

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
                LobbyState = LobbyState.Lobby;
                LobbyColor = LobbyColor.None;
                LastEdit = 0;

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
                sb.AppendLine(LobbyState.ToString());
                sb.Append("Lobby LobbyState Last Edit: ");
                sb.AppendLine(new DateTime(LastEdit).ToString());
                sb.Append("LobbyColor: ");
                sb.AppendLine(LobbyColor.ToString());
                sb.Append("RelayCode: ");
                sb.AppendLine(RelayCode);
                sb.Append("RelayNGO: ");
                sb.AppendLine(RelayNGOCode);

                return sb.ToString();
            }
        }

        public LobbyData Data => m_Data;
        LobbyData m_Data;

        ServerAddress m_RelayServer;

        public LocalLobby()
        {
            m_CloudLobby = new Lobby();
            onChanged += (lobby) => { m_Data.LastEdit = DateTime.Now.ToFileTimeUtc(); };
        }

        /// <summary>Used only for visual output of the Relay connection info. The obfuscated Relay server IP is obtained during allocation in the RelayUtpSetup.</summary>

        #endregion

        public void AddPlayer(LocalPlayer user)
        {
            if (m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogError($"Cant add player {user.DisplayName}({user.ID}) to lobby: {LobbyID} twice");
                return;
            }

            AddUser(user);
            onUserListChanged?.Invoke(m_LobbyUsers);
        }

        void AddUser(LocalPlayer user)
        {
            Debug.Log($"Adding User: {user.DisplayName} - {user.ID}");
            m_CloudLobby.Players.Add(new Player());
            m_LobbyUsers.Add(user.ID, user);
        }

        public void RemovePlayer(LocalPlayer user)
        {
            DoRemoveUser(user);
            onUserListChanged?.Invoke(m_LobbyUsers);
        }

        private void DoRemoveUser(LocalPlayer user)
        {
            if (!m_LobbyUsers.ContainsKey(user.ID))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }

            m_LobbyUsers.Remove(user.ID);
        }

        public CallbackValue<string> LobbyID = new CallbackValue<string>();

        public CallbackValue<string> LobbyCode = new CallbackValue<string>();

        public CallbackValue<string> RelayCode = new CallbackValue<string>();

        public CallbackValue<string> RelayNGOCode = new CallbackValue<string>();

        public CallbackValue<ServerAddress> RelayServer = new CallbackValue<ServerAddress>();

        public CallbackValue<string> LobbyName = new CallbackValue<string>();

        public LobbyState LobbyState
        {
            get => m_Data.LobbyState;
            set
            {
                m_Data.LobbyState = value;
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

        public LobbyColor LobbyColor
        {
            get => m_Data.LobbyColor;
            set
            {
                if (m_Data.LobbyColor != value)
                {
                    m_Data.LobbyColor = value;
                    OnChanged(this);
                }
            }
        }

        public void OnChanged()
        {
            onLobbyChanged?.Invoke(this);
        }

        public void CopyObserved(LobbyData lobbyData, Dictionary<string, LocalPlayer> lobbyUsers)
        {
            // It's possible for the host to edit the lobby in between the time they last pushed lobby data and the time their pull for new lobby data completes.
            // If that happens, the edit will be lost, so instead we maintain the time of last edit to detect that case.
            m_Data = lobbyData;
            LobbyState = lobbyData.LobbyState;
            ;
            LobbyColor = lobbyData.LobbyColor;
            m_Data.RelayNGOCode = lobbyData.RelayNGOCode;

            if (lobbyUsers == null)
                m_LobbyUsers = new Dictionary<string, LocalPlayer>();
            else
            {
                List<LocalPlayer> toRemove = new List<LocalPlayer>();
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