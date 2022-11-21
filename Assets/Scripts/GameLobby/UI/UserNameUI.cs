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

        public override async void Start()
        {
            base.Start();
            var localUser = await GameManager.Instance.AwaitLocalUserInitialization();
            localUser.DisplayName.onChanged += SetText;

        }

        void SetText(string text)
        {
            m_TextField.SetText(text);
        }
    }
}
