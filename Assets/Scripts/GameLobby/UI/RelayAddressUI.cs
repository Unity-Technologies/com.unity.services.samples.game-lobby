using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Displays the IP when connected to Relay.
    /// </summary>
    public class RelayAddressUI : UIPanelBase
    {
        [SerializeField]
        TMP_Text m_IPAddressText;

        public override void Start()
        {
            base.Start();
            GameManager.Instance.LocalLobby.RelayServer.onChanged += GotRelayAddress;
        }

        void GotRelayAddress(ServerAddress address)
        {
            m_IPAddressText.SetText(address.ToString());
        }
    }
}