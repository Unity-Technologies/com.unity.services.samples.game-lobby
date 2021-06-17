using System.Collections.Generic;

namespace LobbyRooms
{
    /// <summary>
    /// Holds the latest service data, such as the list of rooms
    /// </summary>
    [System.Serializable]
    public class LobbyServiceData : Observed<LobbyServiceData>
    {
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
