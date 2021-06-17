using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LobbyRooms.UI
{
    /// <summary>
    /// Read Only input field (for copy/paste reasons) Watches for the changes in the lobby's Room Code
    /// </summary>
    public class RoomCodeUI : ObserverPanel<LobbyData>
    {
        public TMP_InputField roomCodeText;

        public override void ObservedUpdated(LobbyData observed)
        {
            if (!string.IsNullOrEmpty(observed.RoomCode))
            {
                roomCodeText.text = observed.RoomCode;
                Show();
            }
            else
            {
                Hide();
            }
           
        }
    }
}
