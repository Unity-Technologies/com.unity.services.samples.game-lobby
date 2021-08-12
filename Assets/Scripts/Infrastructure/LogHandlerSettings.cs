using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace LobbyRelaySample
{
    public class LogHandlerSettings : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Only logs of this level or higher will appear in the console.")]
        private LogMode m_Mode = LogMode.Critical;

        void Awake()
        {
            LogHandler.Get().mode = m_Mode;
        }


    }

    [System.Serializable]
    public class LogFilter
    {
        [SerializeField]
        private LogType m_ifThisType;
        public LogType IfThisType => m_ifThisType;

        public UnityEvent<string> m_logMessageCallback;

        public void Filter(LogType logType, string logString)
        {
            if (logType != IfThisType)
                return;
            m_logMessageCallback?.Invoke(logString);
        }
    }
}