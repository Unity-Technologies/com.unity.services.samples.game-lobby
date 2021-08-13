using System;
using System.Collections.Generic;
using UnityEngine;

namespace LobbyRelaySample
{
    public class LogHandlerSettings : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Only logs of this level or higher will appear in the console.")]
        private LogMode m_editorLogVerbosity = LogMode.Critical;

        [SerializeField]
        PopUpUI popUpPrefab;

        [SerializeField]
        List<ErrorReaction> m_errorReactions = new List<ErrorReaction>();

        void Awake()
        {
            LogHandler.Get().mode = m_editorLogVerbosity;
            LogHandler.Get().SetLogReactions(m_errorReactions);
        }

        public void SpawnErrorPopup(string errorMessage)
        {
            var popupInstance = Instantiate(popUpPrefab, transform);
            popupInstance.ShowPopup(errorMessage, Color.red);
        }
    }
}
