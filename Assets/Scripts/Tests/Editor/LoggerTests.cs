using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LobbyRelaySample.Tests
{
    /// <summary>
    /// Tests of the LogHandler overriding debug logging.
    /// </summary>
    public class LoggerTest
    {
        /// <summary>Reset the console between tests.</summary>
        [SetUp]
        public void ResetScene()
        {
            Debug.ClearDeveloperConsole();
        }

        /// <summary>
        /// Only display Log messages when set to Verbose.
        /// </summary>
        [Test]
        public void TestLog()
        {
            LogHandler.Get().mode = LogMode.Critical;
            Debug.Log("CritLog");
            LogAssert.NoUnexpectedReceived(); // Ensure that we haven't received any unexpected logs.

            LogHandler.Get().mode = LogMode.Warnings;
            Debug.Log("WarningLog");
            LogAssert.NoUnexpectedReceived();

            LogHandler.Get().mode = LogMode.Verbose;
            Debug.Log("VerbLog");

            LogAssert.Expect(LogType.Log, "VerbLog");
        }

        /// <summary>
        /// Only display Warning messages when set to Verbose or Warnings.
        /// </summary>
        [Test]
        public void TestWarning()
        {
            LogHandler.Get().mode = LogMode.Critical;
            Debug.LogWarning("CritWarning");
            LogAssert.NoUnexpectedReceived();

            LogHandler.Get().mode = LogMode.Warnings;
            Debug.LogWarning("WarningWarning");
            LogAssert.Expect(LogType.Warning, "WarningWarning");

            LogHandler.Get().mode = LogMode.Verbose;
            Debug.LogWarning("VerbWarning");
            LogAssert.Expect(LogType.Warning, "VerbWarning");
        }

        /// <summary>
        /// Always display Error messages.
        /// </summary>
        [Test]
        public void TestError()
        {
            LogHandler.Get().mode = LogMode.Critical;
            Debug.LogError("CritError");
            LogAssert.Expect(LogType.Error, "CritError");

            LogHandler.Get().mode = LogMode.Warnings;
            Debug.LogError("WarningError");
            LogAssert.Expect(LogType.Error, "WarningError");

            LogHandler.Get().mode = LogMode.Verbose;
            Debug.LogError("VerbError");
            LogAssert.Expect(LogType.Error, "VerbError");
        }

        /// <summary>
        /// Always display Assert messages.
        /// </summary>
        [Test]
        public void TestAssert()
        {
            LogHandler.Get().mode = LogMode.Critical;
            Debug.LogAssertion(true);
            LogAssert.Expect(LogType.Assert, "True");

            LogHandler.Get().mode = LogMode.Warnings;
            Debug.LogAssertion(true);
            LogAssert.Expect(LogType.Assert, "True");

            LogHandler.Get().mode = LogMode.Verbose;
            Debug.LogAssertion(true);
            LogAssert.Expect(LogType.Assert, "True");
        }

        /// <summary>
        /// Always display Exception messages.
        /// </summary>
        [Test]
        public void TestException()
        {
            LogHandler.Get().mode = LogMode.Critical;
            LogAssert.Expect(LogType.Exception, "Exception: CriticalException");
            Debug.LogException(new Exception("CriticalException"));

            LogHandler.Get().mode = LogMode.Warnings;
            LogAssert.Expect(LogType.Exception, "Exception: WarningException");
            Debug.LogException(new Exception("WarningException"));

            LogHandler.Get().mode = LogMode.Verbose;
            LogAssert.Expect(LogType.Exception, "Exception: VerboseException");
            Debug.LogException(new Exception("VerboseException"));
        }
    }
}
