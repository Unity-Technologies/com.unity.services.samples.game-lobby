using System;
using System.Collections;
using LobbyRelaySample.Relay;
using NUnit.Framework;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{
    /// <summary>
    /// Accesses the Authentication and Relay services in order to ensure we can connect to Relay and retrieve a join code.
    /// RelayUtp* wraps the Relay API, so go through that in practice. This simply ensures the connection to the Lobby service is functional.
    ///
    /// If the tests pass, you can assume you are connecting to the Relay service itself properly.
    /// </summary>
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
        }

        /// <summary>
        /// Create a Relay allocation, request a join code, and then join. Note that this is purely to ensure the service is functioning;
        /// in practice, the RelayUtpSetup does more work to bind to the allocation and has slightly different logic for hosts vs. clients.
        /// </summary>
        [UnityTest]
        public IEnumerator DoBaseRoundTrip()
        {
            if (!m_didSigninComplete)
                yield return new WaitForSeconds(3);
            if (!m_didSigninComplete)
                Assert.Fail("Did not sign in.");
            yield return new WaitForSeconds(1); // To prevent a possible 429 after a previous test.

            // Allocation
            float timeout = 5;
            Allocation allocation = null;
            RelayAPIInterface.AllocateAsync(4, (a) => { allocation = a; });
            while (allocation == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }

            Assert.Greater(timeout, 0, "Timeout Check (Allocate)");
            Guid allocationId = allocation.AllocationId;
            var allocationIP = allocation.RelayServer.IpV4;
            var allocationPort = allocation.RelayServer.Port;
            Assert.NotNull(allocationId);
            Assert.NotNull(allocationIP);
            Assert.NotNull(allocationPort);

            // Join code retrieval
            timeout = 5;
            string joinCode = null;
            RelayAPIInterface.GetJoinCodeAsync(allocationId, (j) => { joinCode = j; });
            while (joinCode == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout Check (JoinCode)");
            Assert.False(string.IsNullOrEmpty(joinCode));

            // Joining with the join code
            timeout = 5;
            JoinAllocation joinResponse = null;
            RelayAPIInterface.JoinAsync(joinCode, (j) => { joinResponse = j; });
            while (joinResponse == null && timeout > 0)
            {   yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            Assert.Greater(timeout, 0, "Timeout Check (Join)");
            var codeIp = joinResponse.RelayServer.IpV4;
            var codePort = joinResponse.RelayServer.Port;
            Assert.AreEqual(codeIp, allocationIP);
            Assert.AreEqual(codePort, allocationPort);
        }
    }
}
