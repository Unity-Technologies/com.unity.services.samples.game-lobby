using TMPro;
using UnityEngine;

namespace GamelobbySample.UI
{
    /// <summary>
    /// Displays the IP when connected to Relay.
    /// </summary>
    public class RelayAddressUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        TMP_Text m_IPAddressText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            m_IPAddressText.SetText(observed.RelayServer?.ToString());
        }
    }
}
