using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class ServerAddressUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        TMP_Text m_IPAddressText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            m_IPAddressText.SetText(observed.RelayServer?.ToString());
        }
    }
}
