using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// After all players ready up for the game, this will show the countdown that occurs.
    /// This countdown is purely visual, to give clients a moment if they need to un-ready before entering the game; 
    /// clients will actually wait for a message from the host confirming that they are in the game, instead of assuming the game is ready to go when the countdown ends.
    /// </summary>
    public class CountdownUI : LocalLobbyObserver
    {
        [SerializeField]
        TMP_Text m_CountDownText;

        protected override void UpdateObserver(LocalLobby obs)
        {
            base.UpdateObserver(obs);
            if (observed.CountDownTime <= 0)
                m_CountDownText.SetText("Waiting for all players...");
            else
                m_CountDownText.SetText($"Starting in: {observed.CountDownTime:0}");
        }
    }
}