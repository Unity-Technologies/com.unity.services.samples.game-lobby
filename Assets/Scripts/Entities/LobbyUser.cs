using System;

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
        Connecting = 1,   // User has joined a lobby but has not yet connected to Relay.
        Lobby = 2,        // User is in a lobby and connected to Relay.
        Ready = 4,        // User has selected the ready button, to ready for the "game" to start.
        InGame = 8,       // User is part of a "game" that has started.
        Menu = 16         // User is not in a lobby, in one of the main menus.
    }

    /// <summary>
    /// Data for a local player instance. This will update data and is observed to know when to push local player changes to the entire lobby.
    /// </summary>
    [Serializable]
    public class LobbyUser : Observed<LobbyUser>
    {
        public LobbyUser(bool isHost = false, string displayName = null, string id = null, EmoteType emote = EmoteType.None, string userStatus = null)
        {
            m_isHost = isHost;
            m_displayName = displayName;
            m_id = id;
            m_emote = emote;
            UserStatus status;
            if (!string.IsNullOrEmpty(userStatus) && Enum.TryParse(userStatus, out status))
                m_userStatus = status;
        }

        /// <summary>
        /// Used for limiting costly OnChanged actions to just the members which actually changed.
        /// </summary>
        [Flags]
        public enum UserMembers { IsHost = 1, DisplayName = 2, Emote = 4, ID = 8, UserStatus = 16 }
        private UserMembers m_lastChanged;// TODO: Is the following necessary to prompt an initial update, or do I need to adjust RelayUtpClient.DoUserUpdate to force all messages on the first go? (Or maybe just have some separate call to send full state as one message to start with? Although it should only be name...) = (UserMembers)(-1); // All values are set as changed to begin with, for initial updates.
        public UserMembers LastChanged => m_lastChanged;

        bool m_isHost;
        public bool IsHost
        {
            get { return m_isHost; }
            set
            {
                if (m_isHost != value)
                {
                    m_isHost = value;
                    m_lastChanged = UserMembers.IsHost;
                    OnChanged(this);
                }
            }
        }

        string m_displayName = "";
        public string DisplayName
        {
            get => m_displayName;
            set
            {
                if (m_displayName != value)
                {
                    m_displayName = value;
                    m_lastChanged = UserMembers.DisplayName;
                    OnChanged(this);
                }
            }
        }

        EmoteType m_emote = EmoteType.None;
        public EmoteType Emote
        {
            get => m_emote;
            set
            {
                if (m_emote != value)
                {
                    m_emote = value;
                    m_lastChanged = UserMembers.Emote;
                    OnChanged(this);
                }
            }
        }

        string m_id = "";
        public string ID
        {
            get => m_id;
            set
            {
                if (m_id != value)
                {
                    m_id = value;
                    m_lastChanged = UserMembers.ID;
                    OnChanged(this);
                }
            }
        }

        UserStatus m_userStatus = UserStatus.Menu;
        public UserStatus UserStatus
        {
            get => m_userStatus;
            set
            {
                m_userStatus = value;
                m_lastChanged = UserMembers.UserStatus;
                OnChanged(this);
            }
        }

        public override void CopyObserved(LobbyUser oldObserved)
        {
            int lastChanged = // Set flags just for the members that will be changed.
                (m_displayName == oldObserved.m_displayName ? 0 : (int)UserMembers.DisplayName) |
                (m_emote == oldObserved.m_emote ?             0 : (int)UserMembers.Emote) |
                (m_id == oldObserved.m_id ?                   0 : (int)UserMembers.ID) |
                (m_isHost == oldObserved.m_isHost ?           0 : (int)UserMembers.IsHost) |
                (m_userStatus == oldObserved.m_userStatus ?   0 : (int)UserMembers.UserStatus);

            m_displayName = oldObserved.m_displayName;
            m_emote = oldObserved.m_emote;
            m_id = oldObserved.m_id;
            m_isHost = oldObserved.m_isHost;
            m_userStatus = oldObserved.m_userStatus;
            m_lastChanged = (UserMembers)lastChanged;

            if (lastChanged != 0) // Ensure something actually changed.
                OnChanged(this);
        }
    }
}
