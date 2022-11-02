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
    public class LocalLobby
    {

        public bool CanSetChanged = true;
        public Action<Dictionary<string, LocalPlayer>> onUserListChanged;

        Dictionary<string, LocalPlayer> m_LocalPlayers = new Dictionary<string, LocalPlayer>();
        public Dictionary<string, LocalPlayer> LocalPlayers => m_LocalPlayers;

        #region LocalLobbyData

        ServerAddress m_RelayServer;

        public LocalLobby()
        {
            LastUpdated.Value = DateTime.Now.ToFileTimeUtc();
            onUserListChanged = (dict) => { SetValueChanged(); };
            LobbyID.onChanged = (s) => { SetValueChanged(); };
            LobbyCode.onChanged = (s) => { SetValueChanged(); };
            RelayCode.onChanged = (s) => { SetValueChanged(); };
            RelayNGOCode.onChanged = (s) => { SetValueChanged(); };
            RelayServer.onChanged = (s) => { SetValueChanged(); };
            LobbyName.onChanged = (s) => { SetValueChanged(); };
            LocalLobbyState.onChanged = (s) => { SetValueChanged(); };
            Private.onChanged = (s) => { SetValueChanged(); };
            AvailableSlots.onChanged = (s) => { SetValueChanged(); };
            MaxPlayerCount.onChanged = (s) => { SetValueChanged(); };
            LocalLobbyColor.onChanged = (s) => { SetValueChanged(); };
            LastUpdated.onChanged = (s) => { SetValueChanged(); };
        }

        /// <summary>Used only for visual output of the Relay connection info. The obfuscated Relay server IP is obtained during allocation in the RelayUtpSetup.</summary>

        #endregion
        public CallbackValue<string> LobbyID = new CallbackValue<string>();

        public CallbackValue<string> LobbyCode = new CallbackValue<string>();

        public CallbackValue<string> RelayCode = new CallbackValue<string>();

        public CallbackValue<string> RelayNGOCode = new CallbackValue<string>();

        public CallbackValue<ServerAddress> RelayServer = new CallbackValue<ServerAddress>();

        public CallbackValue<string> LobbyName = new CallbackValue<string>();

        public CallbackValue<LobbyState> LocalLobbyState = new CallbackValue<LobbyState>();

        public CallbackValue<bool> Locked = new CallbackValue<bool>();
        public CallbackValue<bool> Private = new CallbackValue<bool>();

        public CallbackValue<int> AvailableSlots = new CallbackValue<int>();

        public CallbackValue<int> MaxPlayerCount = new CallbackValue<int>();

        public CallbackValue<LobbyColor> LocalLobbyColor = new CallbackValue<LobbyColor>();

        public CallbackValue<long> LastUpdated = new CallbackValue<long>();

        public int PlayerCount => m_LocalPlayers.Count;

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
        }

        /// <summary>
        /// A locking mechanism for registering when something has looked at the Lobby to see if anything has changed
        /// </summary>
        /// <returns></returns>
        public bool IsLobbyChanged()
        {
            bool isChanged = m_ValuesChanged;
            m_ValuesChanged = false;
            return isChanged;
        }

        void SetValueChanged()
        {
            if (CanSetChanged)
                m_ValuesChanged = true;
        }
        bool m_ValuesChanged;

        public void AddPlayer(LocalPlayer user)
        {
            if (m_LocalPlayers.ContainsKey(user.ID.Value))
            {
                Debug.LogError($"Cant add player {user.DisplayName}({user.ID}) to lobby: {LobbyID} twice");
                return;
            }

            Debug.Log($"Adding User: {user.DisplayName} - {user.ID}");
            m_LocalPlayers.Add(user.ID.Value, user);
            user.Emote.onChanged += EmoteChangedCallback;
            user.DisplayName.onChanged += StringChangedCallback;
            user.IsHost.onChanged += BoolChangedCallback;
            user.UserStatus.onChanged += EmoteChangedCallback;

            onUserListChanged?.Invoke(m_LocalPlayers);
        }

        public void RemovePlayer(LocalPlayer user)
        {
            if (!m_LocalPlayers.ContainsKey(user.ID.Value))
            {
                Debug.LogWarning($"Player {user.DisplayName}({user.ID}) does not exist in lobby: {LobbyID}");
                return;
            }
            m_LocalPlayers.Remove(user.ID.Value);
            user.Emote.onChanged -= EmoteChangedCallback;
            user.DisplayName.onChanged -= StringChangedCallback;
            user.IsHost.onChanged -= BoolChangedCallback;
            user.UserStatus.onChanged -= EmoteChangedCallback;
            onUserListChanged?.Invoke(m_LocalPlayers);
        }


        void EmoteChangedCallback(EmoteType emote)
        {
            IsLobbyChanged();
        }

        void StringChangedCallback(string text)
        {
            IsLobbyChanged();
        }

        void BoolChangedCallback(bool changed)
        {
            IsLobbyChanged();
        }

        void EmoteChangedCallback(UserStatus status)
        {
            IsLobbyChanged();
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
            sb.Append("RelayNGO: ");
            sb.AppendLine(RelayNGOCode.Value);

            return sb.ToString();
        }
    }
}
