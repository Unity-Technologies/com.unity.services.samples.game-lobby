using System;
using System.Text;
using UnityEngine;

namespace Unity.Services.Authentication.Utilities
{
    /// <summary>
    /// LogLevel is used to control the logs that are written to Unity for debugging.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Disable all logs from Authentication SDK.
        /// </summary>
        Off = 0,

        /// <summary>
        /// Show errors in Authentication SDK.
        /// </summary>
        ErrorsOnly = 1,

        /// <summary>
        /// Show warnings and errors in Authentication SDK.
        /// </summary>
        WarningsAndErrors = 2,

        /// <summary>
        /// Show all logs in Authentication SDK.
        /// </summary>
        Verbose = 3
    }

    interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);

        void Info(string format, params object[] args);
        void Warning(string format, params object[] args);
        void Error(string format, params object[] args);
    }

    class Logger : ILogger
    {
        readonly string m_Prefix;

        delegate void LogMethod(object message);

        public LogLevel LogLevel { get; private set; }

        public Logger(string prefix, LogLevel logLevel = LogLevel.ErrorsOnly)
        {
            m_Prefix = prefix;
            LogLevel = logLevel;
        }

        public void SetLogLevel(LogLevel level)
        {
            LogLevel = level;
        }

        public void Info(string message)
        {
            if (LogLevel >= LogLevel.Verbose)
            {
                Log(Debug.Log, message);
            }
        }

        public void Info(string format, params object[] args)
        {
            if (LogLevel >= LogLevel.Verbose)
            {
                Log(Debug.Log, format, args);
            }
        }

        public void Warning(string message)
        {
            if (LogLevel >= LogLevel.WarningsAndErrors)
            {
                Log(Debug.LogWarning, message);
            }
        }

        public void Warning(string format, params object[] args)
        {
            if (LogLevel >= LogLevel.WarningsAndErrors)
            {
                Log(Debug.LogWarning, format, args);
            }
        }

        public void Error(string message)
        {
            if (LogLevel >= LogLevel.ErrorsOnly)
            {
                Log(Debug.LogError, message);
            }
        }

        public void Error(string format, params object[] args)
        {
            if (LogLevel >= LogLevel.ErrorsOnly)
            {
                Log(Debug.LogError, format, args);
            }
        }

        void Log(LogMethod log, string format, params object[] args)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(m_Prefix);
                sb.Append(" ");
                if (args?.Length == 0)
                {
                    // There is no args, so it's supposed to be a raw string to log.
                    // Don't do AppendFormat since it will throw FormatException if the format string contains
                    // placeholder character {}.
                    sb.Append(format);
                }
                else
                {
                    sb.AppendFormat(format, args);
                }

                log(sb.ToString());
            }
            catch (Exception e)
            {
                try
                {
                    // It's possible to get FormatException if the format string doesn't match args.
                    // Fallback to a non-formatted string

                    var sb = new StringBuilder();
                    sb.Append(m_Prefix);
                    sb.Append(" [");
                    sb.Append(e.Message);
                    sb.Append("] ");
                    sb.Append(format);
                    foreach (var arg in args)
                    {
                        sb.Append(" ");
                        sb.Append(arg);
                    }

                    log(sb.ToString());
                }
                catch
                {
                    // Ignore the exception if it fails again, best effort.
                    // It's possible that log() itself throws exception, then there isn't a good way to write a log.
                }
            }
        }
    }
}
