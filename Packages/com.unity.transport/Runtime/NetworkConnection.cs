using Unity.Collections;

namespace Unity.Networking.Transport
{
    namespace Error
    {
        /// <summary>
        /// DisconnectReason enumerates all disconnect reasons.
        /// </summary>
        public enum DisconnectReason : byte
        {
            /// <summary>Indicates a normal disconnection as a result of calling Disconnect on the connection.</summary>
            Default, // don't assign explicit values
            /// <summary>Indicates the connection timed out.</summary>
            Timeout,
            /// <summary>Indicates the connection failed to establish a connection after <see cref="NetworkConfigParameter.maxConnectAttempts"/>.</summary>
            MaxConnectionAttempts,
            /// <summary>Indicates the connection was closed remotely.</summary>
            ClosedByRemote,

            /// <summary>Used only for count. Keep last and don't assign explicit values</summary>
            Count
        }

        public enum StatusCode
        {
            Success                       =  0,
            NetworkIdMismatch             = -1,
            NetworkVersionMismatch        = -2,
            NetworkStateMismatch          = -3,
            NetworkPacketOverflow         = -4,
            NetworkSendQueueFull          = -5,
            NetworkHeaderInvalid          = -6,
            NetworkDriverParallelForErr   = -7,
            NetworkSendHandleInvalid      = -8,
            NetworkArgumentMismatch       = -9,
        }
    }

    /// <summary>
    /// The NetworkConnection is a struct that hold all information needed by the driver to link it with a virtual
    /// connection. The NetworkConnection is a public representation of a connection.
    /// </summary>
    public struct NetworkConnection
    {
        internal int m_NetworkId;
        internal int m_NetworkVersion;

        /// <summary>
        /// ConnectionState enumerates available connection states a connection can have.
        /// </summary>
        public enum State
        {
            /// <summary>Indicates the connection is disconnected</summary>
            Disconnected,
            /// <summary>Indicates the connection is trying to connect.</summary>
            Connecting,
            /// <summary>Indicates the connection is waiting for a connection response. </summary>
            AwaitingResponse,
            /// <summary>Indicates the connection is connected.. </summary>
            Connected
        }

        /// <summary>
        /// Disconnects a virtual connection and marks it for deletion. This connection will be removed on next the next frame.
        /// </summary>
        /// <param name="driver">The driver that owns the virtual connection.</param>
        public int Disconnect(NetworkDriver driver)
        {
            return driver.Disconnect(this);
        }

        /// <summary>
        /// Receive an event for this specific connection. Should be called until it returns <see cref="NetworkEvent.Type.Empty"/>, even if the socket is disconnected.
        /// </summary>
        /// <param name="driver">The driver that owns the virtual connection.</param>
        /// <param name="strm">A DataStreamReader, that will only be populated if a <see cref="NetworkEvent.Type.Data"/>
        /// event was received.
        /// </param>
        public NetworkEvent.Type PopEvent(NetworkDriver driver, out DataStreamReader stream)
        {
            return driver.PopEventForConnection(this, out stream);
        }

        public NetworkEvent.Type PopEvent(NetworkDriver driver, out DataStreamReader stream, out NetworkPipeline pipeline)
        {
            return driver.PopEventForConnection(this, out stream, out pipeline);
        }

        /// <summary>
        /// Close an active NetworkConnection, similar to <see cref="Disconnect{T}"/>.
        /// </summary>
        /// <param name="driver">The driver that owns the virtual connection.</param>
        public int Close(NetworkDriver driver)
        {
            if (m_NetworkId >= 0)
                return driver.Disconnect(this);
            return -1;
        }

        /// <summary>
        /// Check to see if a NetworkConnection is Created.
        /// </summary>
        /// <returns>`true` if the NetworkConnection has been correctly created by a call to
        /// <see cref="NetworkDriver.Accept"/> or <see cref="NetworkDriver.Connect"/></returns>
        public bool IsCreated
        {
            get { return m_NetworkVersion != 0; }
        }

        public State GetState(NetworkDriver driver)
        {
            return driver.GetConnectionState(this);
        }

        public static bool operator ==(NetworkConnection lhs, NetworkConnection rhs)
        {
            return lhs.m_NetworkId == rhs.m_NetworkId && lhs.m_NetworkVersion == rhs.m_NetworkVersion;
        }

        public static bool operator !=(NetworkConnection lhs, NetworkConnection rhs)
        {
            return lhs.m_NetworkId != rhs.m_NetworkId || lhs.m_NetworkVersion != rhs.m_NetworkVersion;
        }

        public override bool Equals(object o)
        {
            return this == (NetworkConnection)o;
        }
        public bool Equals(NetworkConnection o)
        {
            return this == o;
        }

        public override int GetHashCode()
        {
            return (m_NetworkId << 8) ^ m_NetworkVersion;
        }

        public int InternalId => m_NetworkId;
    }
}