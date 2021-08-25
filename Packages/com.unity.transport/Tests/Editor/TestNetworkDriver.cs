namespace Unity.Networking.Transport.Tests
{
    public static class TestNetworkDriver
   {
        public static NetworkDriver Create(params INetworkParameter[] param)
        {
            return new NetworkDriver(new IPCNetworkInterface(), param);
        }
    }
}