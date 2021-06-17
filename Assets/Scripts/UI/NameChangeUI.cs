using UnityEngine;
using Utilities;

namespace LobbyRooms.UI
{
    /// <summary>
    /// Sends a message that should change the displayName Data only.
    /// </summary>
    public class NameChangeUI : UIPanelBase
    {
        public void OnEndNameEdit(string name)
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.RenameRequest, name);
        }
    }
}
