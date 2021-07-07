using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Displays the player's name.
    /// </summary>
    public class PlayerNameUI : ObserverPanel<LobbyUser>
    {
        [SerializeField]
        TMP_Text m_TextField;

        public override void ObservedUpdated(LobbyUser observed)
        {
            m_TextField.SetText(observed.DisplayName);
        }
    }
}
