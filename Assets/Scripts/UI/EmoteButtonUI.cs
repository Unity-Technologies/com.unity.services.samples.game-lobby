using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls a button which will set the local player's emote state when pressed. This demonstrates a player updating their data within the room.
    /// </summary>
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
