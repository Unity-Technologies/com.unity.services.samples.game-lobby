using NUnit.Framework;

namespace Unity.Networking.Transport.Tests
{
    [TestFixture]
    public class NetworkAddressTests
    {
        [Test]
        public unsafe void NetworkAddress_CastingTests()
        {
            var endpoint = new NetworkEndPoint();
            endpoint.Port = 1;
            Assert.True(1 == endpoint.Port);

            Assert.True(256 == endpoint.RawPort);
        }
        
        [Test]
        public unsafe void NetworkAddress_ParseAddress_CompareToBaselibParse()
        {
            // 19 ==  SizeOf<Binding.Baselib_NetworkAddress>
            //Assert.True(19 == UnsafeUtility.SizeOf<Binding.Baselib_NetworkAddress>());
            
            string[] addresses = {
                "127.0.0.1",
                "192.168.1.134",
                "53BF:009C:0000:0000:120A:09D5:000D:CD29",
                "2001:0db8::0370:7334",
                "2001:db8::123.123.123.123",
                "1200:0000:AB00:1234:0000:2552:7777:1313",
                "21DA:D3:0:2F3B:2AA:FF:FE28:9C5A",
                "FE80:0000:0000:0000:0202:B3FF:FE1E:8329",
                "53BF:009C:0000:0000:120A:09D5:000D:CD29",
                "0.0.0.0",
                "9.255.255.255",
                "11.0.0.0",
                "126.255.255.255",
                "129.0.0.0",
                "169.253.255.255",
                "169.255.0.0",
                "172.15.255.255",
                "172.32.0.0",
                "191.0.1.255",
                "192.88.98.255",
                "192.88.100.0",
                "192.167.255.255",
                "192.169.0.0",
                "198.17.255.255",
                "223.255.255.255",
                "[2001:db8:0:1]:80",
                "http://[2001:db8:0:1]:80",
                "1200:0000:AB00:1234:O000:2552:7777:1313"
            };

            NetworkFamily[] families =
            {
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv4,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6,
                NetworkFamily.Ipv6
            };
        }
    }
}
