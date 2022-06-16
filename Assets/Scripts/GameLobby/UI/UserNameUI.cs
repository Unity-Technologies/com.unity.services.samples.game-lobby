using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Displays the player's name.
    /// </summary>
    public class UserNameUI : ObserverPanel<LocalPlayer>
    {
        [SerializeField]
        TMP_Text m_TextField;

        public override void ObservedUpdated(LocalPlayer observed)
        {
            m_TextField.SetText(observed.DisplayName);
        }
    }
}
