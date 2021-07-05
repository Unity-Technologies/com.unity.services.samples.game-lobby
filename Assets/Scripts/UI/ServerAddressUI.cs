using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class ServerAddressUI : ObserverPanel<LobbyData>
    {
        [SerializeField]
        TMP_Text m_IPAddressText;

        public override void ObservedUpdated(LobbyData observed)
        {
            m_IPAddressText.SetText(observed.RelayServer?.ToString());
        }
    }
}
