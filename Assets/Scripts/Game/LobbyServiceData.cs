using System.Collections.Generic;

namespace LobbyRelaySample
{
    public enum LobbyServiceState
    {
        Empty,
        Fetching,
        Error,
        Fetched
    }

    /// <summary>
    /// Holds data related to the Lobby service itself - The latest retrieved lobby list, the state of retrieval.
    /// </summary>
    [System.Serializable]
    public class LobbyServiceData : Observed<LobbyServiceData>
    {
        LobbyServiceState m_CurrentState = LobbyServiceState.Empty;

        public long lastErrorCode;
        public LobbyServiceState State
        {
            get { return m_CurrentState; }
            set
            {
                m_CurrentState = value;
                OnChanged(this);
            }
        }

        Dictionary<string, LocalLobby> m_currentLobbies = new Dictionary<string, LocalLobby>();

        /// <summary>
        /// Will only trigger if the dictionary is set wholesale. Changes in the size, or contents will not trigger OnChanged
        /// string is lobby ID, Key is the Lobby data representation of it
        /// </summary>
        public Dictionary<string, LocalLobby> CurrentLobbies
        {
            get { return m_currentLobbies; }
            set
            {
                m_currentLobbies = value;
                OnChanged(this);
            }
        }

        public override void CopyObserved(LobbyServiceData oldObserved)
        {
            m_currentLobbies = oldObserved.CurrentLobbies;
            OnChanged(this);
        }
    }
}
