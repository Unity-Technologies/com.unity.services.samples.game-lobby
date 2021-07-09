using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Networking.Transport
{
    internal interface INetworkProtocol : IDisposable
    {
        /// <summary>
        /// This is call when initializing the NetworkDriver. If the protocol requires custom paramters, they can be passed
        /// to the NetworkDriver initialization.
        /// </summary>
        void Initialize(INetworkParameter[] parameters);

        /// <summary>
        /// Returns a burst compatible NetworkProtocol struct containing the function pointers and custom UserData for the protocol.
        /// </summary>
        NetworkProtocol CreateProtocolInterface();

        /// <summary>
        /// This method should bind the NetworkInterface to the local endpoint and perform any
        /// custom binding behaviour for the protocol.
        /// </summary>
        int Bind(INetworkInterface networkInterface, ref NetworkInterfaceEndPoint localEndPoint);

        /// <summary>
        /// Create a new connection address for the endPoint using the passed NetworkInterface.
        /// Some protocols - as relay - could decide to use virtual addressed that not necessarily
        /// maps 1 - 1 to a endpoint.
        /// </summary>
        int Connect(INetworkInterface networkInterface, NetworkEndPoint endPoint, out NetworkInterfaceEndPoint address);

        NetworkEndPoint GetRemoteEndPoint(INetworkInterface networkInterface, NetworkInterfaceEndPoint address);
    }

    /// <summary>
    /// This is a Burst compatible struct that contains all the function pointers that the NetworkDriver
    /// uses for processing messages with a particular protocol.
    /// </summary>
    internal struct NetworkProtocol
    {
        /// <summary>
        /// Computes the size required for allocating a packet for the connection with this protocol. The dataCapacity received
        /// can be modified to reflect the resulting payload capacity of the packet, if it gets reduced the NetworkDriver will
        /// return a NetworkPacketOverflow error. The payloadOffset return value is the possition where the payload data needs
        /// to be stored in the packet.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ComputePacketAllocationSizeDelegate(ref NetworkDriver.Connection connection, ref int dataCapacity, out int payloadOffset);

        /// <summary>
        /// Process a receiving packet and returns a ProcessPacketCommand that will indicate to the NetworkDriver what actions
        /// need to be performed and what to do with the message.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessReceiveDelegate(IntPtr stream, ref NetworkInterfaceEndPoint address, int size, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData, ref ProcessPacketCommand command);

        /// <summary>
        /// Process a sending packet. When this method is called, the packet is ready to be sent through the sendInterface.
        /// Here the protocol could perform some final steps as, for instance, filling some header fields.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ProcessSendDelegate(ref NetworkDriver.Connection connection, bool hasPipeline, ref NetworkSendInterface sendInterface, ref NetworkInterfaceSendHandle sendHandle, ref NetworkSendQueueHandle queueHandle, IntPtr userData);

        /// <summary>
        /// This method should send a protocol specific connect confirmation message from a server to a client using the connection.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessSendConnectionAcceptDelegate(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData);

        /// <summary>
        /// This method should send a protocol specific connect request message from a client to a server.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessSendConnectionRequestDelegate(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData);

        /// <summary>
        /// This method should send a protocol specific disconnect request message from a client to a server.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ProcessSendDisconnectDelegate(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData);

        /// <summary>
        /// This method is called every NetworkDriver tick and can be used for performing protocol update operations.
        /// One common case is sending protocol specific packets for keeping the connections alive or retrying failed ones.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void UpdateDelegate(long updateTime, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData);

        public TransportFunctionPointer<ComputePacketAllocationSizeDelegate> ComputePacketAllocationSize;
        public TransportFunctionPointer<ProcessReceiveDelegate> ProcessReceive;
        public TransportFunctionPointer<ProcessSendDelegate> ProcessSend;
        public TransportFunctionPointer<ProcessSendConnectionAcceptDelegate> ProcessSendConnectionAccept;
        public TransportFunctionPointer<ProcessSendConnectionRequestDelegate> ProcessSendConnectionRequest;
        public TransportFunctionPointer<ProcessSendDisconnectDelegate> ProcessSendDisconnect;
        public TransportFunctionPointer<UpdateDelegate> Update;

        /// <summary>
        /// Raw pointer that is going to be passed to the function pointer and that can contain protocol specific data.
        /// </summary>
        [NativeDisableUnsafePtrRestriction] public IntPtr UserData;

        /// <summary>
        /// The maximun size of the header of a data packet for this protocol.
        /// </summary>
        public int MaxHeaderSize;

        /// <summary>
        /// The maximun size of the footer of a data packet for this protocol.
        /// </summary>
        public int MaxFooterSize;

        /// <summary>
        /// The maximun amount of bytes that are not payload data for a packet for this protocol.
        /// </summary>
        public int PaddingSize => MaxHeaderSize + MaxFooterSize;

        /// <summary>
        /// If true - UpdateDelegate will be called
        /// </summary>
        public bool NeedsUpdate;

        public NetworkProtocol(
            TransportFunctionPointer<ComputePacketAllocationSizeDelegate> computePacketAllocationSize,
            TransportFunctionPointer<ProcessReceiveDelegate> processReceive,
            TransportFunctionPointer<ProcessSendDelegate> processSend,
            TransportFunctionPointer<ProcessSendConnectionAcceptDelegate> processSendConnectionAccept,
            TransportFunctionPointer<ProcessSendConnectionRequestDelegate> processSendConnectionRequest,
            TransportFunctionPointer<ProcessSendDisconnectDelegate> processSendDisconnect,
            TransportFunctionPointer<UpdateDelegate> update,
            bool needsUpdate,
            IntPtr userData,
            int maxHeaderSize,
            int maxFooterSize
        ) {
            ComputePacketAllocationSize = computePacketAllocationSize;
            ProcessReceive = processReceive;
            ProcessSend = processSend;
            ProcessSendConnectionAccept = processSendConnectionAccept;
            ProcessSendConnectionRequest = processSendConnectionRequest;
            ProcessSendDisconnect = processSendDisconnect;
            Update = update;
            NeedsUpdate = needsUpdate;
            UserData = userData;
            MaxHeaderSize = maxHeaderSize;
            MaxFooterSize = maxFooterSize;
        }
    }

    /// <summary>
    /// The type of commands that the NetworkDriver can process from a received packet after it is proccessed
    /// by the protocol.
    /// </summary>
    public enum ProcessPacketCommandType : byte
    {
        /// <summary>
        /// Do not perform any extra action.
        /// </summary>
        Drop = 0, // keep Drop = 0 to make it the default.

        /// <summary>
        /// Find and update the address for a connection.
        /// </summary>
        AddressUpdate,

        /// <summary>
        /// Complete the binding proccess.
        /// </summary>
        BindAccept,

        /// <summary>
        /// The connection has been accepted by the server and can be completed.
        /// </summary>
        ConnectionAccept,

        /// <summary>
        /// The connection has been rejected by the server.
        /// </summary>
        ConnectionReject,

        /// <summary>
        /// A connection request comming from a client has been received by the server.
        /// </summary>
        ConnectionRequest,

        /// <summary>
        /// A Data message has been received for a well stablished connection.
        /// </summary>
        Data,

        /// <summary>
        /// The connection is requesting to disconnect.
        /// </summary>
        Disconnect,

        /// <summary>
        /// A simultanious Data + Connection accept command.
        /// </summary>
        DataWithImplicitConnectionAccept,
    }

    /// <summary>
    /// Contains the command type and command data required by the NetworkDriver to process a packet.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ProcessPacketCommand
    {
        /// <summary>
        /// The type of the command to proccess
        /// </summary>
        [FieldOffset(0)] public ProcessPacketCommandType Type;

        // The following fields behaves like a C++ union. All command types data should start with the Address field.
        [FieldOffset(1)] public NetworkInterfaceEndPoint ConnectionAddress;
        [FieldOffset(1)] public ProcessPacketCommandAddressUpdate AsAddressUpdate;
        [FieldOffset(1)] public ProcessPacketCommandConnectionAccept AsConnectionAccept;
        [FieldOffset(1)] public ProcessPacketCommandConnectionRequest AsConnectionRequest;
        [FieldOffset(1)] public ProcessPacketCommandData AsData;
        [FieldOffset(1)] public ProcessPacketCommandDataWithImplicitConnectionAccept AsDataWithImplicitConnectionAccept;
        [FieldOffset(1)] public ProcessPacketCommandDisconnect AsDisconnect;
    }

    internal struct ProcessPacketCommandAddressUpdate
    {
        public NetworkInterfaceEndPoint Address;
        public NetworkInterfaceEndPoint NewAddress;
        public ushort SessionToken;
    }

    internal struct ProcessPacketCommandConnectionRequest
    {
        public NetworkInterfaceEndPoint Address;
        public ushort SessionId;
    }

    internal struct ProcessPacketCommandConnectionAccept
    {
        public NetworkInterfaceEndPoint Address;
        public ushort SessionId;
        public ushort ConnectionToken;
    }

    internal struct ProcessPacketCommandDisconnect
    {
        public NetworkInterfaceEndPoint Address;
        public ushort SessionId;
    }

    internal struct ProcessPacketCommandData
    {
        public NetworkInterfaceEndPoint Address;
        public ushort SessionId;
        public int Offset;
        public int Length;
        public byte HasPipelineByte;

        public bool HasPipeline => HasPipelineByte != 0;
    }

    internal struct ProcessPacketCommandDataWithImplicitConnectionAccept
    {
        public NetworkInterfaceEndPoint Address;
        public ushort SessionId;
        public int Offset;
        public int Length;
        public byte HasPipelineByte;
        public ushort ConnectionToken;

        public bool HasPipeline => HasPipelineByte != 0;
    }
}