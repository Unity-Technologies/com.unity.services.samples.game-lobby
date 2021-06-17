using System;
using LobbyRooms;
using LobbyRooms.UI;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobbyRooms
{
    public class PlayerNameUI : ObserverPanel<LobbyUser>
    {
        [SerializeField]
        TMP_Text m_TextField;

        public override void ObservedUpdated(LobbyUser observed)
        {
            m_TextField.SetText(observed.DisplayName);
        }
    }
}
