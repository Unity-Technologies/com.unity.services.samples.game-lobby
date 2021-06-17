using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbyRooms.UI
{
    /// <summary>
    /// Read Only input field (for copy/paste reasons) Watches for the changes in the lobby's Relay Code
    /// </summary>
    public class RelayCodeUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        TMP_InputField relayCodeText;

        public override void ObservedUpdated(LobbyData observed)
        {
            if (!string.IsNullOrEmpty(observed.RelayCode))
            {
                relayCodeText.text = observed.RelayCode;
                Show();
            }
            else
            {
                Hide();
            }
        }
    }
}
