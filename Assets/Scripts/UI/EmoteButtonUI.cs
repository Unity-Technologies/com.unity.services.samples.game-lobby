using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

namespace LobbyRooms.UI
{
    public class EmoteButtonUI : MonoBehaviour
    {
        [SerializeField]
        TMP_Text m_EmoteTextElement;

        public void SetPlayerEmote()
        {
            Locator.Get.Messenger.OnReceiveMessage(MessageType.UserSetEmote, m_EmoteTextElement.text);
        }
    }
}
