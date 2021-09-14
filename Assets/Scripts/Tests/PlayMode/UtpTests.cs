using System.Collections;
using System.Collections.Generic;
using LobbyRelaySample.relay;
using NUnit.Framework;
using Unity.Networking.Transport;
using Unity.Services.Relay.Models;
using UnityEngine.TestTools;

namespace Test
{
    public class UtpTests
    {
        private class RelayUtpTest : RelayUtpSetupHost
        {
            public NetworkEndPoint GetEndpointButPublic(List<RelayServerEndpoint> endpoints, string ip, int port, out bool isSecure)
            {
                return GetEndpointForAllocation(endpoints, ip, port, out isSecure);
            }
        }

        [UnityTest]
        public IEnumerator DTLSCheck()
        {
            yield return null; // TEMP

            #if ENABLE_MANAGED_UNITYTLS
                Assert.Fail("TODO: Implement.");
            #else
                Assert.Ignore("DTLS is not available in this version of Unity.");
            #endif
        }
    }
}
