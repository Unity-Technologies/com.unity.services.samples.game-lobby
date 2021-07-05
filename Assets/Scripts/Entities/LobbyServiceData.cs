using System.Collections.Generic;

namespace LobbyRooms
{
    public enum LobbyServiceState
    {
        Empty,
        Fetching,
        Error,
        Fetched
    }

    /// <summary>
    /// Holds the latest service data, such as the list of rooms
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

        Dictionary<string, LobbyData> m_currentLobbies = new Dictionary<string, LobbyData>();

        /// <summary>
        /// Will only trigger if the dictionary is set wholesale. Changes in the size, or contents will not trigger OnChanged
        /// string is lobby ID, Key is the Lobby data representation of it
        /// </summary>
        public Dictionary<string, LobbyData> CurrentLobbies //TODO Copy or Re-implement vivox observableCollections?
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
