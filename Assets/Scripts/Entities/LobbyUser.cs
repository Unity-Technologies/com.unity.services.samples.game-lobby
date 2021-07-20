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
            m_DisplayName = displayName;
            m_ID = id;  
            m_Emote = emote;
            UserStatus status;
            if (!string.IsNullOrEmpty(userStatus) && Enum.TryParse(userStatus, out status))
                m_UserStatus = status;
        }

        bool m_isHost;

        public bool IsHost
        {
            get { return m_isHost; }
            set
            {
                if (m_isHost != value)
                {
                    m_isHost = value;
                    OnChanged(this);
                }
            }
        }

        string m_DisplayName = "";

        public string DisplayName
        {
            get => m_DisplayName;
            set
            {
                if (m_DisplayName != value)
                {
                    m_DisplayName = value;
                    OnChanged(this);
                }
            }
        }

        EmoteType m_Emote = EmoteType.None;

        public EmoteType Emote
        {
            get => m_Emote;
            set
            {
                if (m_Emote != value)
                {
                    m_Emote = value;
                    OnChanged(this);
                }
            }
        }

        string m_ID = "";

        public string ID
        {
            get => m_ID;
            set
            {
                if (m_ID != value)
                {
                    m_ID = value;
                    OnChanged(this);
                }
            }
        }

        UserStatus m_UserStatus = UserStatus.Menu;

        public UserStatus UserStatus
        {
            get => m_UserStatus;
            set
            {
                m_UserStatus = value;
                OnChanged(this);
            }
        }

        public override void CopyObserved(LobbyUser oldObserved)
        {
            m_DisplayName = oldObserved.m_DisplayName;
            m_Emote = oldObserved.m_Emote;
            m_ID = oldObserved.m_ID;
            m_isHost = oldObserved.m_isHost;
            m_UserStatus = oldObserved.m_UserStatus;
            OnChanged(this);
        }
    }
}
