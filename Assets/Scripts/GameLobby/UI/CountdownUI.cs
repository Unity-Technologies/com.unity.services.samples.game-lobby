using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// After all players ready up for the game, this will show the countdown that occurs.
    /// This countdown is purely visual, to give clients a moment if they need to un-ready before entering the game;
    /// clients will actually wait for a message from the host confirming that they are in the game, instead of assuming the game is ready to go when the countdown ends.
    /// </summary>
    public class CountdownUI : UIPanelBase
    {
        [SerializeField]
        TMP_Text m_CountDownText;

        public void OnTimeChanged(float time)
        {
            if (time <= 0)
                m_CountDownText.SetText("Waiting for all players...");
            else
                m_CountDownText.SetText($"Starting in: {time:0}"); // Note that the ":0" formatting rounds, not truncates.
        }
    }
}