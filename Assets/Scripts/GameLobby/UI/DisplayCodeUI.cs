using System;
using TMPro;
using UnityEngine;

namespace LobbyRelaySample.UI
{
    /// <summary>
    /// Watches a lobby or relay code for updates, displaying the current code to lobby members.
    /// </summary>
    public class DisplayCodeUI : UIPanelBase
    {
        public enum CodeType { Lobby = 0, Relay = 1 }

        [SerializeField]
        TMP_InputField m_outputText;
        [SerializeField]
        CodeType m_codeType;

        void LobbyCodeChanged(string newCode)
        {
            if (!string.IsNullOrEmpty(newCode))
            {
                m_outputText.text = newCode;
                Show();
            }
            else
            {
                Hide();
            }
        }

        public override void Start()
        {
            base.Start();
            if(m_codeType==CodeType.Lobby)
                Manager.LocalLobby.LobbyCode.onChanged += LobbyCodeChanged;
            if(m_codeType==CodeType.Relay)
                Manager.LocalLobby.RelayCode.onChanged += LobbyCodeChanged;
        }
    }
}
