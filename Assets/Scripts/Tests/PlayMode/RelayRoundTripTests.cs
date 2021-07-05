using System;
using System.Collections;
using LobbyRelaySample.Relay;
using NUnit.Framework;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{
    public class RelayRoundTripTests
    {
        private LobbyRelaySample.Auth.SubIdentity_Authentication m_auth;
        private bool m_didSigninComplete = false;

        [OneTimeSetUp]
        public void Setup()
        {
            m_auth = new LobbyRelaySample.Auth.SubIdentity_Authentication(() => { m_didSigninComplete = true; });
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            m_auth?.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        [UnityTest]
        public IEnumerator DoBaseRoundTrip()
        {
            LogAssert.ignoreFailingMessages = true;
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");

            //Allocation
            float timeout = 5;
            Response<AllocateResponseBody> allocationResponse = null;

            RelayInterface.AllocateAsync(4, (a) => { allocationResponse = a; });
            while (allocationResponse == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (Allocate)");
            Assert.IsTrue(allocationResponse.Status >= 200 && allocationResponse.Status < 300, "AllocationResponse should return a success code.");
            Guid allocationId = allocationResponse.Result.Data.Allocation.AllocationId;
            var allocationIP = allocationResponse.Result.Data.Allocation.RelayServer.IpV4;
            var allocationPort = allocationResponse.Result.Data.Allocation.RelayServer.Port;
            Assert.NotNull(allocationId);
            Assert.NotNull(allocationIP);
            Assert.NotNull(allocationPort);

            //Join Code Fetch
            timeout = 5;
            Response<JoinCodeResponseBody> joinCodeResponse = null;

            RelayInterface.GetJoinCodeAsync(allocationId, (j) => { joinCodeResponse = j; });
            while (joinCodeResponse == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (JoinCode)");
            Assert.IsTrue(allocationResponse.Status >= 200 && allocationResponse.Status < 300, "JoinCodeResponse should return a success code.");
            string joinCode = joinCodeResponse.Result.Data.JoinCode;
            Assert.False(string.IsNullOrEmpty(joinCode));

            //Join Via Join Code
            timeout = 5;
            Response<JoinResponseBody> joinResponse = null;

            RelayInterface.JoinAsync(joinCode, (j) => { joinResponse = j; });
            while (joinResponse == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (Join)");
            Assert.IsTrue(allocationResponse.Status >= 200 && allocationResponse.Status < 300, "JoinResponse should return a success code.");
            var codeIp = joinResponse.Result.Data.Allocation.RelayServer.IpV4;
            var codePort = joinResponse.Result.Data.Allocation.RelayServer.Port;
            Assert.AreEqual(codeIp, allocationIP);
            Assert.AreEqual(codePort, allocationPort);
        }

        [UnityTest]
        public IEnumerator DoShortcutRoundtrip()
        {
            LogAssert.ignoreFailingMessages = true;
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");
            yield return new WaitForSeconds(1); // To prevent a possible 429 after a previous test.

            //Allocation
            float timeout = 5;
            Allocation allocation = null;

            RelayInterface.AllocateAsync(4, (a) => { allocation = a; });
            while (allocation == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (Allocate)");
            Guid allocationId = allocation.AllocationId;
            var allocationIP = allocation.RelayServer.IpV4;
            var allocationPort = allocation.RelayServer.Port;
            Assert.NotNull(allocationId);
            Assert.NotNull(allocationIP);
            Assert.NotNull(allocationPort);

            //Join Code Fetch
            timeout = 5;
            string joinCode = null;

            RelayInterface.GetJoinCodeAsync(allocationId, (j) => { joinCode = j; });
            while (joinCode == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (JoinCode)");
            Assert.False(string.IsNullOrEmpty(joinCode));

            //Join Via Join Code
            timeout = 5;
            Response<JoinResponseBody> joinResponse = null;

            RelayInterface.JoinAsync(joinCode, (j) => { joinResponse = j; });
            while (joinResponse == null && timeout > 0)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (Join)");
            var codeIp = joinResponse.Result.Data.Allocation.RelayServer.IpV4;
            var codePort = joinResponse.Result.Data.Allocation.RelayServer.Port;
            Assert.AreEqual(codeIp, allocationIP);
            Assert.AreEqual(codePort, allocationPort);
        }
    }
}
