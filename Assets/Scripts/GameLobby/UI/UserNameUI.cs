using LobbyRelaySample.ngo;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Displays the player's name.
    /// </summary>
    public class UserNameUI : UIPanelBase
    {
        [SerializeField]
        TMP_Text m_TextField;

        public override void Start()
        {
            base.Start();
            GameManager.Instance.LocalUser.DisplayName.onChanged += SetText;
        }

        void SetText(string text)
        {
            m_TextField.SetText(text);
        }
    }
}