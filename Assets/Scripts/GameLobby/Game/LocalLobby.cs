using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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
    public class LocalLobby
    {
        public Action<LocalPlayer> onUserJoined;

        public Action<int> onUserLeft;

        public Action<int> onUserReadyChange;

        public CallbackValue<string> LobbyID = new CallbackValue<string>();

        public CallbackValue<string> LobbyCode = new CallbackValue<string>();

        public CallbackValue<string> RelayCode = new CallbackValue<string>();

        public CallbackValue<ServerAddress> RelayServer = new CallbackValue<ServerAddress>();

        public CallbackValue<string> LobbyName = new CallbackValue<string>();

        public CallbackValue<string> HostID = new CallbackValue<string>();

        public CallbackValue<LobbyState> LocalLobbyState = new CallbackValue<LobbyState>();

        public CallbackValue<bool> Locked = new CallbackValue<bool>();

        public CallbackValue<bool> Private = new CallbackValue<bool>();

        public CallbackValue<int> AvailableSlots = new CallbackValue<int>();

        public CallbackValue<int> MaxPlayerCount = new CallbackValue<int>();

        public CallbackValue<LobbyColor> LocalLobbyColor = new CallbackValue<LobbyColor>();

        public CallbackValue<long> LastUpdated = new CallbackValue<long>();

        public int PlayerCount => m_LocalPlayers.Count;
        ServerAddress m_RelayServer;

        public List<LocalPlayer> LocalPlayers => m_LocalPlayers;
        List<LocalPlayer> m_LocalPlayers = new List<LocalPlayer>();

        public void ResetLobby()
        {
            m_LocalPlayers.Clear();

            LobbyName.Value = "";
            LobbyID.Value = "";
            LobbyCode.Value = "";
            Locked.Value = false;
            Private.Value = false;
            LocalLobbyColor.Value = LobbyRelaySample.LobbyColor.None;
            AvailableSlots.Value = 4;
            MaxPlayerCount.Value = 4;
            onUserJoined = null;
            onUserLeft = null;
        }

        public LocalLobby()
        {
            LastUpdated.Value = DateTime.Now.ToFileTimeUtc();
            HostID.onChanged += OnHostChanged;
        }

        ~LocalLobby()
        {
            HostID.onChanged -= OnHostChanged;
        }

        public LocalPlayer GetLocalPlayer(int index)
        {
            return PlayerCount > index ? m_LocalPlayers[index] : null;
        }

        private void OnHostChanged(string newHostId)
        {
            foreach(var player in m_LocalPlayers)
            {
                player.IsHost.Value = player.ID.Value == newHostId;
            }
        }
        
        public void AddPlayer(int index, LocalPlayer user)
        {
            m_LocalPlayers.Insert(index, user);
            user.UserStatus.onChanged += OnUserChangedStatus;
            onUserJoined?.Invoke(user);
            Debug.Log($"Added User: {user.DisplayName.Value} - {user.ID.Value} to slot {index + 1}/{PlayerCount}");
        }

        public void RemovePlayer(int playerIndex)
        {
            m_LocalPlayers[playerIndex].UserStatus.onChanged -= OnUserChangedStatus;
            m_LocalPlayers.RemoveAt(playerIndex);
            onUserLeft?.Invoke(playerIndex);
        }

        void OnUserChangedStatus(PlayerStatus status)
        {
            int readyCount = 0;
            foreach (var player in m_LocalPlayers)
            {
                if (player.UserStatus.Value == PlayerStatus.Ready)
                    readyCount++;
            }

            onUserReadyChange?.Invoke(readyCount);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("Lobby : ");
            sb.AppendLine(LobbyName.Value);
            sb.Append("ID: ");
            sb.AppendLine(LobbyID.Value);
            sb.Append("Code: ");
            sb.AppendLine(LobbyCode.Value);
            sb.Append("Locked: ");
            sb.AppendLine(Locked.Value.ToString());
            sb.Append("Private: ");
            sb.AppendLine(Private.Value.ToString());
            sb.Append("AvailableSlots: ");
            sb.AppendLine(AvailableSlots.Value.ToString());
            sb.Append("Max Players: ");
            sb.AppendLine(MaxPlayerCount.Value.ToString());
            sb.Append("LocalLobbyState: ");
            sb.AppendLine(LocalLobbyState.Value.ToString());
            sb.Append("Lobby LocalLobbyState Last Edit: ");
            sb.AppendLine(new DateTime(LastUpdated.Value).ToString());
            sb.Append("LocalLobbyColor: ");
            sb.AppendLine(LocalLobbyColor.Value.ToString());
            sb.Append("RelayCode: ");
            sb.AppendLine(RelayCode.Value);

            return sb.ToString();
        }
    }
}