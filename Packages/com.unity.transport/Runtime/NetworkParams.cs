namespace Unity.Networking.Transport
{
    /// <summary>
    /// The interface for NetworkParameters
    /// </summary>
    public interface INetworkParameter
    {
    }

    /// <summary>
    /// Default NetworkParameter Constants.
    /// </summary>
    public struct NetworkParameterConstants
    {
        /// <summary>The default size of the event queue.</summary>
        public const int InitialEventQueueSize = 100;
        public const int InvalidConnectionId = -1;

        /// <summary>
        /// The default size of the DataStreamWriter. This value can be overridden using the <see cref="NetworkConfigParameter"/>.
        /// </summary>
        public const int DriverDataStreamSize = 64 * 1024;
        /// <summary>The default connection timeout value. This value can be overridden using the <see cref="NetworkConfigParameter"/></summary>
        public const int ConnectTimeoutMS = 1000;
        /// <summary>The default max connection attempts value. This value can be overridden using the <see cref="NetworkConfigParameter"/></summary>
        public const int MaxConnectAttempts = 60;
        /// <summary>The default disconnect timeout attempts value. This value can be overridden using the <see cref="NetworkConfigParameter"/></summary>
        public const int DisconnectTimeoutMS = 30 * 1000;

        public const int MTU = 1400;
    }

    /// <summary>
    /// The NetworkDataStreamParameter is used to set a fixed data stream size.
    /// </summary>
    /// <remarks>The <see cref="DataStreamWriter"/> will grow on demand if the size is set to zero. </remarks>
    public struct NetworkDataStreamParameter : INetworkParameter
    {
        /// <summary>Size of the default <see cref="DataStreamWriter"/></summary>
        public int size;
    }

    /// <summary>
    /// The NetworkConfigParameter is used to set specific parameters that the driver uses.
    /// </summary>
    public struct NetworkConfigParameter : INetworkParameter
    {
        /// <summary>A timeout in milliseconds indicating how long we will wait until we send a new connection attempt.</summary>
        public int connectTimeoutMS;
        /// <summary>The maximum amount of connection attempts we will try before disconnecting.</summary>
        public int maxConnectAttempts;
        /// <summary>A timeout in milliseconds indicating how long we will wait for a socket event, before we disconnect the socket.</summary>
        /// <remarks>The connection needs to receive data from the connected endpoint within this timeout.</remarks>
        public int disconnectTimeoutMS;
        /// <summary>The maximum amount of time a single frame can advance timeout values.</summary>
        /// <remarks>The main use for this parameter is to not get disconnects at frame spikes when both endpoints lives in the same process.</remarks>
        public int maxFrameTimeMS;
        /// <summary>A fixed amount of time to use for an interval between ScheduleUpdate. This is used instead of a clock.</summary>
        /// <remarks>The main use for this parameter is tests where determinism is more important than correctness.</remarks>
        public int fixedFrameTimeMS;
    }
}