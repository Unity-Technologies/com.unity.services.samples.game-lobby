using System;
using System.Collections;
using LobbyRelaySample.relay;
using NUnit.Framework;
using Unity.Networking.Transport;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{
    public class UtpTests
    {
        private class RelayUtpTest : RelayUtpSetupHost
        {
            public Action<NetworkEndPoint, bool> OnGetEndpoint { private get; set; }

            public void JoinRelayPublic()
            {
                JoinRelay();
            }

            protected override void JoinRelay()
            {
                RelayAPIInterface.AllocateAsync(1, OnAllocation);

                void OnAllocation(Allocation allocation)
                {
                    bool isSecure = false;
                    NetworkEndPoint endpoint = GetEndpointForAllocation(allocation.ServerEndpoints, allocation.RelayServer.IpV4, allocation.RelayServer.Port, out isSecure);
                    OnGetEndpoint?.Invoke(endpoint, isSecure);
                    // The allocation will be cleaned up automatically, since we won't be pinging it regularly.
                }
            }
        }

        private LobbyRelaySample.Auth.SubIdentity_Authentication m_auth;
        private bool m_didSigninComplete = false;
        GameObject m_dummy;

        [OneTimeSetUp]
        public void Setup()
        {
            m_dummy = new GameObject();
            m_auth = new LobbyRelaySample.Auth.SubIdentity_Authentication(() => { m_didSigninComplete = true; });
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            m_auth?.Dispose();
            GameObject.Destroy(m_dummy);
        }

        [UnityTest]
        public IEnumerator DTLSCheck()
        {
            #if ENABLE_MANAGED_UNITYTLS

                if (!m_didSigninComplete)
                    yield return new WaitForSeconds(3);
                if (!m_didSigninComplete)
                    Assert.Fail("Did not sign in.");
                yield return new WaitForSeconds(1); // To prevent a possible 429 after a previous test.

                RelayUtpTest relaySetup = m_dummy.AddComponent<RelayUtpTest>();
                relaySetup.OnGetEndpoint = OnGetEndpoint;
                bool? isSecure = null;
                NetworkEndPoint endpoint = default;

                relaySetup.JoinRelayPublic();
                float timeout = 5;
                while (!isSecure.HasValue && timeout > 0)
                {
                    timeout -= 0.25f;
                    yield return new WaitForSeconds(0.25f);
                }
                Component.Destroy(relaySetup);
                Assert.IsTrue(timeout > 0, "Timeout check.");

                Assert.IsTrue(isSecure, "Should have a secure server endpoint.");
                Assert.IsTrue(endpoint.IsValid, "Endpoint should be valid.");

                void OnGetEndpoint(NetworkEndPoint resultEndpoint, bool resultIsSecure)
                {
                    endpoint = resultEndpoint;
                    isSecure = resultIsSecure;
                }

            #else

                Assert.Ignore("DTLS encryption for Relay is not currently available for this version of Unity.");
                yield break;

            #endif
        }
    }
}
