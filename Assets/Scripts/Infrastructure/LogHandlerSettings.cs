using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LobbyRelaySample
{
    public class LogHandlerSettings : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Only logs of this level or higher will appear in the console.")]
        private LogMode m_editorLogVerbosity = LogMode.Critical;

        [SerializeField]
        private PopUpUI m_popUpPrefab;

        [SerializeField]
        private ErrorReaction m_errorReaction;

        void Awake()
        {
            LogHandler.Get().mode = m_editorLogVerbosity;
            LogHandler.Get().SetLogReactions(m_errorReaction);
        }

        public void SpawnErrorPopup(string errorMessage)
        {
            var popupInstance = Instantiate(m_popUpPrefab, transform);
            popupInstance.ShowPopup(errorMessage, Color.red);
        }
    }
}
