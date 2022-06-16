using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace LobbyRelaySample
{
    /// <summary>
    /// Current state of the user in the lobby.
    /// This is a Flags enum to allow for the Inspector to select multiples for various UI features.
    /// </summary>
    [Flags]
    public enum UserStatus
    {
        None = 0,
        Connecting = 1, // User has joined a lobby but has not yet connected to Relay.
        Lobby = 2, // User is in a lobby and connected to Relay.
        Ready = 4, // User has selected the ready button, to ready for the "game" to start.
        InGame = 8, // User is part of a "game" that has started.
        Menu = 16 // User is not in a lobby, in one of the main menus.
    }

    /// <summary>
    /// Data for a local player instance. This will update data and is observed to know when to push local player changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LocalPlayer : Observed<LocalPlayer>
    {
        public LocalPlayer(bool isHost = false, string displayName = null, string id = null,
            EmoteType emote = EmoteType.None, UserStatus userStatus = UserStatus.Menu, bool isApproved = false)
        {
            m_data = new UserData(isHost, displayName, id, emote, userStatus, isApproved);
        }

        const string key_Displayname = nameof(DisplayName);
        const string key_Userstatus = nameof(UserStatus);
        const string key_Emote = nameof(Emote);

        #region Local UserData

        public Player CloudPlayer { get; private set; }

        public struct UserData
        {
            public bool IsHost { get; set; }
            public string DisplayName { get; set; }
            public string ID { get; set; }
            public EmoteType Emote { get; set; }
            public UserStatus UserStatus { get; set; }
            public bool IsApproved { get; set; }

            public UserData(bool isHost, string displayName, string id, EmoteType emote, UserStatus userStatus,
                bool isApproved)
            {
                IsHost = isHost;
                DisplayName = displayName;
                ID = id;
                Emote = emote;
                UserStatus = userStatus;
                IsApproved = isApproved;
            }
        }

        UserData m_data;

        public LocalPlayer()
        {
            CloudPlayer = new Player();
            DeesplayName.onChanged += SynchDisplayName;
        }

        public void ResetState()
        {
            m_data = new UserData(false, m_data.DisplayName, m_data.ID, EmoteType.None, UserStatus.Menu,
                false); // ID and DisplayName should persist since this might be the local user.
        }

        #endregion

        /// <summary>
        /// Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [Flags]
        public enum UserMembers
        {
            IsHost = 1,
            DisplayName = 2,
            Emote = 4,
            ID = 8,
            UserStatus = 16,
        }

        public bool IsHost
        {
            get { return m_data.IsHost; }
            set
            {
                if (m_data.IsHost == value)
                    return;
                m_data.IsHost = value;
                OnChanged(this);
            }
        }

        public CallbackValue<string> DeesplayName = new CallbackValue<string>();

        void SynchDisplayName(string name)
        {
            PlayerDataObject playerDataObject;
            if (CloudPlayer.Data.TryGetValue(key_Displayname, out playerDataObject))
            {
                playerDataObject.Value = name;
            }
            else
            {
                CloudPlayer.Data.Add(key_Displayname,
                    new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name));
            }
        }
        //TODO Finish this, for now i'm going to the LocalLobby
        public string DisplayName
        {
            get => CloudPlayer.Data[key_Displayname].Value;
            set
            {

                if (m_data.DisplayName == value)
                    return;
                m_data.DisplayName = value;
                OnChanged(this);
            }
        }

        public EmoteType Emote
        {
            get => m_data.Emote;
            set
            {
                if (m_data.Emote == value)
                    return;
                m_data.Emote = value;
                OnChanged(this);
            }
        }

        public string ID
        {
            get => m_data.ID;
            set
            {
                if (m_data.ID == value)
                    return;
                m_data.ID = value;
                OnChanged(this);
            }
        }

        UserStatus m_userStatus = UserStatus.Menu;

        public UserStatus UserStatus
        {
            get => m_userStatus;
            set
            {
                if (m_userStatus == value)
                    return;
                m_userStatus = value;
                OnChanged(this);
            }
        }

        public override void CopyObserved(LocalPlayer observed)
        {
            m_data = observed.m_data;
            ;

            OnChanged(this);
        }
    }
}