using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace LobbyRelaySample
{
    public enum LogMode
    {
        Critical, // Errors only
        Warnings, // Errors and Warnings
        Verbose // Everything
    }

    /// <summary>
    /// Overrides the default Unity logging with our own, so that verbose logs (both from the services and from any of our Debug.Log* calls) don't clutter the Console.
    /// </summary>
    public class LogHandler : ILogHandler
    {
        public LogMode mode = LogMode.Critical;

        static LogHandler s_instance;
        ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler; // Store the default logger that prints to console.
        ErrorReaction m_reaction;

        public static LogHandler Get()
        {
            if (s_instance != null) return s_instance;
            s_instance = new LogHandler();
            Debug.unityLogger.logHandler = s_instance;
            return s_instance;
        }

        public void SetLogReactions(ErrorReaction reactions)
        {
            m_reaction = reactions;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (logType == LogType.Exception) // Exceptions are captured by LogException and should always be logged.
                return;

            if (logType == LogType.Error || logType == LogType.Assert)
            {
                m_DefaultLogHandler.LogFormat(logType, context, format, args);
                return;
            }

            if (mode == LogMode.Critical)
                return;

            if (logType == LogType.Warning)
            {
                m_DefaultLogHandler.LogFormat(logType, context, format, args);
                return;
            }

            if (mode != LogMode.Verbose)
                return;

            m_DefaultLogHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, Object context)
        {
            LogReaction(exception);
            m_DefaultLogHandler.LogException(exception, context);
        }

        private void LogReaction(Exception exception)
        {
            m_reaction?.Filter(exception);
        }
    }

    /// <summary>
    /// The idea here is to 
    /// </summary>
    [Serializable]
    public class ErrorReaction
    {
        public UnityEvent<string> m_logMessageCallback;

        public void Filter(Exception exception)
        {
            string message = "";
            var rawExceptionMessage = "";

            //We want to Ensure the most relevant error message is on top
            if (exception.InnerException != null)
                rawExceptionMessage = exception.InnerException.ToString();
            else
                rawExceptionMessage = exception.ToString();

            var firstLineIndex = rawExceptionMessage.IndexOf("\n");
            var firstRelayString = rawExceptionMessage.Substring(0, firstLineIndex);
            message = firstRelayString;

            if (string.IsNullOrEmpty(message))
                return;
            m_logMessageCallback?.Invoke(message);
        }
    }
}
