using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LobbyRelaySample.Tests
{
    public class LoggerTest
    {
        [SetUp]
        public void ResetScene()
        {
            Debug.ClearDeveloperConsole(); // Reset Console between tests
        }

        [Test]
        public void TestLog() // Should not show when not Verbose
        {
            LogHandler.Get().mode = LogMode.Critical;
            Debug.Log("CritLog");
            LogAssert.NoUnexpectedReceived(); //Checks to see if there is anything here, there should not be

            LogHandler.Get().mode = LogMode.Warnings;
            Debug.Log("WarningLog");
            LogAssert.NoUnexpectedReceived();

            LogHandler.Get().mode = LogMode.Verbose;
            Debug.Log("VerbLog");

            LogAssert.Expect(LogType.Log, "VerbLog");
        }

        [Test]
        public void TestWarning() // Should not show when Critical
        {
            LogHandler.Get().mode = LogMode.Critical;
            Debug.LogWarning("CritWarning");
            LogAssert.NoUnexpectedReceived(); //Checks to see if there is anything here, there should not be

            LogHandler.Get().mode = LogMode.Warnings;
            Debug.LogWarning("WarningWarning");
            LogAssert.Expect(LogType.Warning, "WarningWarning");

            LogHandler.Get().mode = LogMode.Verbose;
            Debug.LogWarning("VerbWarning");
            LogAssert.Expect(LogType.Warning, "VerbWarning");
        }

        [Test]
        public void TestError() // Should show regardless.
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

        [Test]
        public void TestAssert() //Should Show regardless
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

        [Test]
        public void TestException() //Should Show regardless
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
