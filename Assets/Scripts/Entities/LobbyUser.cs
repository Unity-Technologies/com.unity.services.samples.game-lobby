using System;
using System.ComponentModel;
using UnityEngine;
using LobbyRooms;
using UnityEngine.Serialization;

namespace LobbyRooms
{
    /// <summary>
    /// Current state of the user in the lobby.
    /// Set as a flag to allow for the unity inspector to select multiples for various UI features.
    /// </summary>
    [Flags]
    public enum UserStatus
    {
        Lobby = 1, // Connected to lobby, not ready yet
        Ready = 4, // User clicked ready
        Connecting = 8, // User sent join request through relay
        Connected = 16, // User connected through relay
        Menu = 32, // User is in a menu, external to the lobby
    }

    /// <summary>
    /// Lobby Room Data for a player
    /// </summary>
    [Serializable]
    public class LobbyUser : Observed<LobbyUser>
    {
        public LobbyUser(bool isHost = false, string displayName = null, string id = null, string emote = null, string userStatus = null)
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

        string m_Emote = "";

        public string Emote
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
