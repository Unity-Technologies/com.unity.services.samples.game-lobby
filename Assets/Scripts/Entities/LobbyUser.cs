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
        Lobby = 1,      // Connected to lobby, not ready yet
        Ready = 4,      // User clicked ready (Note that 2 is missing; some flags have been removed over time, but we want any serialized values to be unaffected.)
        Connecting = 8, // User sent join request through Relay
        Connected = 16, // User connected through Relay
        Menu = 32,      // User is in a menu, external to the lobby
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
        private UserMembers m_lastChanged;
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
