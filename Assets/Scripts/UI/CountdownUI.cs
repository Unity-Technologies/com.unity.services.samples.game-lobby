using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// After all players ready up for the game, this will show the countdown that occurs.
    /// </summary>
    public class CountdownUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        TMP_Text m_CountDownText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            if (observed.CountDownTime <= 0)
                return;
            m_CountDownText.SetText($"Starting in: {observed.CountDownTime}");
        }
    }
}