using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbyRooms.UI
{
    public class ServerAddressUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        TMP_Text m_IPAddressText;

        public override void ObservedUpdated(LobbyData observed)
        {
            m_IPAddressText.SetText(observed.RelayServer?.ToString());
        }
    }
}
