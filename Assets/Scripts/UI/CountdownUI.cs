using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class CountdownUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        TMP_Text m_CountDownText;

        public override void ObservedUpdated(LobbyData observed)
        {
            if (observed.CountDownTime <= 0)
                return;
            m_CountDownText.SetText($"Starting in: {observed.CountDownTime}");
        }
    }
}