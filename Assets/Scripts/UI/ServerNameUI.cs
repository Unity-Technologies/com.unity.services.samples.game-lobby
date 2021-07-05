using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class ServerNameUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        TMP_Text m_ServerNameText;

        public override void ObservedUpdated(LobbyData observed)
        {
            m_ServerNameText.SetText(observed.LobbyName);
        }
    }
}
