using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    public class ServerNameUI : ObserverPanel<LocalLobby>
    {
        [SerializeField]
        TMP_Text m_ServerNameText;

        public override void ObservedUpdated(LocalLobby observed)
        {
            m_ServerNameText.SetText(observed.LobbyName);
        }
    }
}
