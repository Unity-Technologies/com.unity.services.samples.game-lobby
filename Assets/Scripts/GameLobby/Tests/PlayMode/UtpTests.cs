using System;
using System.Collections;
using System.Threading.Tasks;
using LobbyRelaySample;
using LobbyRelaySample.relay;
using NUnit.Framework;
using Test.Tools;
using Unity.Networking.Transport;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.TestTools;

namespace Test
{
    public class UtpTests
    {
        class RelayUtpTest : RelayUtpSetupHost
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

        GameObject m_dummy;
        //Only used when testing DTLS
        #pragma warning disable CS0414 // This is the "assigned but its value is never used" warning, which will otherwise appear when DTLS is unavailable.
        private bool m_didSigninComplete = false;
        #pragma warning restore CS0414

        [OneTimeSetUp]
        public void Setup()
        {
            m_dummy = new GameObject();
#pragma warning disable 4014
            TestAuthSetup();
#pragma warning restore 4014
        }

        async Task TestAuthSetup()
        {
            await Auth.Authenticate("test");
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            GameObject.Destroy(m_dummy);
        }

        [UnityTest]
        public IEnumerator DTLSCheck()
        {
            #if ENABLE_MANAGED_UNITYTLS

                yield return AsyncTestHelper.Await(async () => await Auth.Authenticating());


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
