using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
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