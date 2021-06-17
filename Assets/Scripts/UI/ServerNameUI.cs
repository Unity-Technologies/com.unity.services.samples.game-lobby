using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbyRooms.UI
{
    public class ServerNameUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        TMP_Text m_ServerNameText;

        public override void ObservedUpdated(LobbyData observed)
        {
            m_ServerNameText.SetText(observed.LobbyName);
        }
    }
}
