using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Controls a button which will set the local player's emote state when pressed. This demonstrates a player updating their data within the room.
    /// </summary>
    public class EmoteButtonUI : UIPanelBase
    {
        [SerializeField]
        EmoteType m_emoteType;

        public void SetPlayerEmote()
        {
            Manager.SetLocalUserEmote(m_emoteType);
        }
    }
}
