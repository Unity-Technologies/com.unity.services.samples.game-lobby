using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
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
