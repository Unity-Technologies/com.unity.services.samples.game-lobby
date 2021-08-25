using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Networking.Transport.Error;
using Unity.Networking.Transport.Utilities;

namespace Unity.Networking.Transport
{
    public unsafe struct QueuedSendMessage
    {
        public fixed byte Data[NetworkParameterConstants.MTU];
        public NetworkInterfaceEndPoint Dest;
        public int DataLength;
    }

    /// <summary>
    /// The NetworkDriver is an implementation of Virtual Connections over any transport.
    ///
    /// Basic usage:
    /// <code>
    /// var driver = new NetworkDriver.Create();
    /// </code>
    /// </summary>
    public struct NetworkDriver : IDisposable
    {
        /// <summary>
        /// Create a Concurrent Copy of the NetworkDriver.
        /// </summary>
        public Concurrent ToConcurrent()
        {
            return new Concurrent
            {
                m_NetworkSendInterface = m_NetworkSendInterface,
                m_NetworkProtocolInterface = m_NetworkProtocolInterface,
                m_EventQueue = m_EventQueue.ToConcurrent(),
                m_ConnectionList = m_ConnectionList,
                m_DataStream = m_DataStream,
                m_DisconnectReasons = m_DisconnectReasons,
                m_PipelineProcessor = m_PipelineProcessor.ToConcurrent(),
                m_DefaultHeaderFlags = m_DefaultHeaderFlags,
                m_ConcurrentParallelSendQueue = m_ParallelSendQueue.AsParallelWriter(),
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_ThreadIndex = 0,
                m_PendingBeginSend = m_PendingBeginSend
#endif
            };
        }

        private Concurrent ToConcurrentSendOnly()
        {
            return new Concurrent
            {
                m_NetworkSendInterface = m_NetworkSendInterface,
                m_NetworkProtocolInterface = m_NetworkProtocolInterface,
                m_EventQueue = default,
                m_ConnectionList = m_ConnectionList,
                m_DataStream = m_DataStream,
                m_DisconnectReasons = m_DisconnectReasons,
                m_PipelineProcessor = m_PipelineProcessor.ToConcurrent(),
                m_DefaultHeaderFlags = m_DefaultHeaderFlags,
                m_ConcurrentParallelSendQueue = m_ParallelSendQueue.AsParallelWriter(),
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_ThreadIndex = 0,
                m_PendingBeginSend = m_PendingBeginSend
#endif
            };
        }

        /// <summary>
        /// The Concurrent struct is used to create an Concurrent instance of the GenericNetworkDriver.
        /// </summary>
        public struct Concurrent
        {
            public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out DataStreamReader reader)
            {
                return PopEventForConnection(connectionId, out reader, out var _);
            }

            public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out DataStreamReader reader, out NetworkPipeline pipeline)
            {
                pipeline = default(NetworkPipeline);

                reader = default(DataStreamReader);
                if (connectionId.m_NetworkId < 0 || connectionId.m_NetworkId >= m_ConnectionList.Length ||
                    m_ConnectionList[connectionId.m_NetworkId].Version != connectionId.m_NetworkVersion)
                    return (int) NetworkEvent.Type.Empty;

                var type = m_EventQueue.PopEventForConnection(connectionId.m_NetworkId, out var offset, out var size, out var pipelineId);
                pipeline = new NetworkPipeline { Id = pipelineId };

                if (type == NetworkEvent.Type.Disconnect && offset < 0)
                    reader = new DataStreamReader(((NativeArray<byte>)m_DisconnectReasons).GetSubArray(math.abs(offset), 1));
                else if (size > 0)
                    reader = new DataStreamReader(((NativeArray<byte>)m_DataStream).GetSubArray(offset, size));

                return type;
            }

            public int MaxHeaderSize(NetworkPipeline pipe)
            {
                var headerSize = m_NetworkProtocolInterface.PaddingSize;
                if (pipe.Id > 0)
                {
                    // All headers plus one byte for pipeline id
                    headerSize += m_PipelineProcessor.SendHeaderCapacity(pipe) + 1;
                }

                return headerSize;
            }
            struct PendingSend
            {
                public NetworkPipeline Pipeline;
                public NetworkConnection Connection;
                public NetworkInterfaceSendHandle SendHandle;
                public int headerSize;
            }

            /// <summary>
            /// Acquires a DataStreamWriter for starting a asynchronous send.
            /// </summary>
            /// <param name="id">The NetworkConnection id to write through</param>
            /// <param name="writer">A DataStreamWriter to write to</param>
            /// <param name="requiredPayloadSize">If you require the payload to be of certain size</param>
            /// <value>Returns <see cref="StatusCode.Success"/> on a successful acquire. Otherwise returns an <see cref="StatusCode"/> indicating the error.</value>
            /// <remarks> Will throw a <exception cref="InvalidOperationException"></exception> if the connection is in a Connecting state.</remarks>
            public unsafe int BeginSend(NetworkConnection id, out DataStreamWriter writer, int requiredPayloadSize = 0)
            {
                return BeginSend(NetworkPipeline.Null, id, out writer, requiredPayloadSize);
            }

            /// <summary>
            /// Acquires a DataStreamWriter for starting a asynchronous send.
            /// </summary>
            /// <param name="pipe">The NetworkPipeline to write through</param>
            /// <param name="id">The NetworkConnection id to write through</param>
            /// <param name="writer">A DataStreamWriter to write to</param>
            /// <param name="requiredPayloadSize">If you require the payload to be of certain size</param>
            /// <value>Returns <see cref="StatusCode.Success"/> on a successful acquire. Otherwise returns an <see cref="StatusCode"/> indicating the error.</value>
            /// <remarks> Will throw a <exception cref="InvalidOperationException"></exception> if the connection is in a Connecting state.</remarks>
            public unsafe int BeginSend(NetworkPipeline pipe, NetworkConnection id,
                out DataStreamWriter writer, int requiredPayloadSize = 0)
            {
                writer = default;

                if (id.m_NetworkId < 0 || id.m_NetworkId >= m_ConnectionList.Length)
                    return (int)Error.StatusCode.NetworkIdMismatch;

                var connection = m_ConnectionList[id.m_NetworkId];
                if (connection.Version != id.m_NetworkVersion)
                    return (int)Error.StatusCode.NetworkVersionMismatch;

                if (connection.State == NetworkConnection.State.Connecting)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnityEngine.Debug.LogError("Cannot send data while connecting");
#endif
                    return (int)Error.StatusCode.NetworkStateMismatch;
                }

                var pipelineHeader = (pipe.Id > 0) ? m_PipelineProcessor.SendHeaderCapacity(pipe) + 1 : 0;
                var payloadCapacity = requiredPayloadSize > 0 ? requiredPayloadSize + pipelineHeader : 0;

                // This will set the payloadCapacity value to the actual capacity provided by the protocol
                var packetAllocationSize = m_NetworkProtocolInterface.ComputePacketAllocationSize.Ptr.Invoke(ref connection, ref payloadCapacity, out var payloadOffset);

                payloadCapacity -= pipelineHeader;

                if (packetAllocationSize > m_PipelineProcessor.PayloadCapacity(pipe) || payloadCapacity < requiredPayloadSize)
                    return (int) Error.StatusCode.NetworkPacketOverflow;

                var result = 0;
                if ((result = m_NetworkSendInterface.BeginSendMessage.Ptr.Invoke(out var sendHandle, m_NetworkSendInterface.UserData, packetAllocationSize)) != 0)
                {
                    sendHandle.data = (IntPtr)UnsafeUtility.Malloc(packetAllocationSize, 8, Allocator.Temp);
                    sendHandle.capacity = packetAllocationSize;
                    sendHandle.id = 0;
                    sendHandle.size = 0;
                    sendHandle.flags = SendHandleFlags.AllocatedByDriver;
                }

                if (sendHandle.capacity < packetAllocationSize)
                    return (int) Error.StatusCode.NetworkPacketOverflow;

                var slice = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>((byte*) sendHandle.data + payloadOffset + pipelineHeader, payloadCapacity, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var safety = AtomicSafetyHandle.GetTempMemoryHandle();
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref slice, safety);
#endif
                writer = new DataStreamWriter(slice);
                writer.m_SendHandleData = (IntPtr)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<PendingSend>(), UnsafeUtility.AlignOf<PendingSend>(), Allocator.Temp);
                *(PendingSend*)writer.m_SendHandleData = new PendingSend
                {
                    Pipeline = pipe,
                    Connection = id,
                    SendHandle = sendHandle,
                    headerSize = payloadOffset,
                };
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_PendingBeginSend[m_ThreadIndex * JobsUtility.CacheLineSize/4] = m_PendingBeginSend[m_ThreadIndex * JobsUtility.CacheLineSize/4] + 1;
#endif
                return (int)Error.StatusCode.Success;
            }

            /// <summary>
            /// Ends a asynchronous send.
            /// </summary>
            /// <param name="writer">If you require the payload to be of certain size.</param>
            /// <value>The length of the buffer sent if nothing went wrong.</value>
            /// <exception cref="InvalidOperationException">If endsend is called with a matching BeginSend call.</exception>
            /// <exception cref="InvalidOperationException">If the connection got closed between the call of being and end send.</exception>
            public unsafe int EndSend(DataStreamWriter writer)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Just here to trigger a safety check on the writer
                if (writer.Capacity == 0)
                    throw new InvalidOperationException("EndSend without matching BeginSend");
#endif
                PendingSend* pendingSendPtr = (PendingSend*)writer.m_SendHandleData;
                if (pendingSendPtr == null || pendingSendPtr->Connection == default)
                    throw new InvalidOperationException("EndSend without matching BeginSend");

                if (m_ConnectionList[pendingSendPtr->Connection.m_NetworkId].Version != pendingSendPtr->Connection.m_NetworkVersion)
                    throw new InvalidOperationException("Connection closed between begin and end send");

                PendingSend pendingSend = *(PendingSend*)writer.m_SendHandleData;
                pendingSendPtr->Connection = default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_PendingBeginSend[m_ThreadIndex * JobsUtility.CacheLineSize/4] = m_PendingBeginSend[m_ThreadIndex * JobsUtility.CacheLineSize/4] - 1;
#endif

                pendingSend.SendHandle.size = pendingSend.headerSize + writer.Length;
                int retval = 0;
                if (pendingSend.Pipeline.Id > 0)
                {
                    pendingSend.SendHandle.size += m_PipelineProcessor.SendHeaderCapacity(pendingSend.Pipeline) + 1;
                    var oldHeaderFlags = m_DefaultHeaderFlags;
                    m_DefaultHeaderFlags = UdpCHeader.HeaderFlags.HasPipeline;
                    retval = m_PipelineProcessor.Send(this, pendingSend.Pipeline, pendingSend.Connection, pendingSend.SendHandle, pendingSend.headerSize);
                    m_DefaultHeaderFlags = oldHeaderFlags;
                }
                else
                    // TODO: Is there a better way we could set the hasPipeline value correctly?
                    // this case is when the message is sent from the pipeline directly, "without a pipeline" so the hasPipeline flag is set in m_DefaultHeaderFlags
                    // allowing us to capture it here
                    retval = CompleteSend(pendingSend.Connection, pendingSend.SendHandle, (m_DefaultHeaderFlags & UdpCHeader.HeaderFlags.HasPipeline) != 0);
                if (retval <= 0)
                    return retval;
                return writer.Length;
            }

            /// <summary>
            /// Aborts a asynchronous send.
            /// </summary>
            /// <param name="writer">If you require the payload to be of certain size.</param>
            /// <value>The length of the buffer sent if nothing went wrong.</value>
            /// <exception cref="InvalidOperationException">If endsend is called with a matching BeginSend call.</exception>
            /// <exception cref="InvalidOperationException">If the connection got closed between the call of being and end send.</exception>
            public unsafe void AbortSend(DataStreamWriter writer)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // Just here to trigger a safety check on the writer
                if (writer.Capacity == 0)
                    throw new InvalidOperationException("EndSend without matching BeginSend");
#endif
                PendingSend* pendingSendPtr = (PendingSend*)writer.m_SendHandleData;
                if (pendingSendPtr == null || pendingSendPtr->Connection == default)
                    throw new InvalidOperationException("EndSend without matching BeginSend");
                PendingSend pendingSend = *(PendingSend*)writer.m_SendHandleData;
                pendingSendPtr->Connection = default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_PendingBeginSend[m_ThreadIndex * JobsUtility.CacheLineSize/4] = m_PendingBeginSend[m_ThreadIndex * JobsUtility.CacheLineSize/4] - 1;
#endif
                AbortSend(pendingSend.SendHandle);
            }

            internal unsafe int CompleteSend(NetworkConnection sendConnection, NetworkInterfaceSendHandle sendHandle, bool hasPipeline)
            {
                if (0 != (sendHandle.flags & SendHandleFlags.AllocatedByDriver))
                {
                    var ret = 0;
                    NetworkInterfaceSendHandle originalHandle = sendHandle;
                    if ((ret = m_NetworkSendInterface.BeginSendMessage.Ptr.Invoke(out sendHandle, m_NetworkSendInterface.UserData, originalHandle.size)) != 0)
                    {
                        return ret;
                    }
                    UnsafeUtility.MemCpy((void*) sendHandle.data, (void*) originalHandle.data, originalHandle.size);
                    sendHandle.size = originalHandle.size;
                }

                var connection = m_ConnectionList[sendConnection.m_NetworkId];
                var queueHandle = NetworkSendQueueHandle.ToTempHandle(m_ConcurrentParallelSendQueue);
                return m_NetworkProtocolInterface.ProcessSend.Ptr.Invoke(ref connection, hasPipeline, ref m_NetworkSendInterface, ref sendHandle, ref queueHandle, m_NetworkProtocolInterface.UserData);
            }
            internal void AbortSend(NetworkInterfaceSendHandle sendHandle)
            {
                if(0 == (sendHandle.flags & SendHandleFlags.AllocatedByDriver))
                {
                    m_NetworkSendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, m_NetworkSendInterface.UserData);
                }
            }
            public NetworkConnection.State GetConnectionState(NetworkConnection id)
            {
                if (id.m_NetworkId < 0 || id.m_NetworkId >= m_ConnectionList.Length)
                    return NetworkConnection.State.Disconnected;
                var connection = m_ConnectionList[id.m_NetworkId];
                if (connection.Version != id.m_NetworkVersion)
                    return NetworkConnection.State.Disconnected;
                return connection.State;
            }

            internal NetworkSendInterface m_NetworkSendInterface;
            internal NetworkProtocol m_NetworkProtocolInterface;
            internal NetworkEventQueue.Concurrent m_EventQueue;
            internal NativeArray<byte> m_DisconnectReasons;

            [ReadOnly] internal NativeList<Connection> m_ConnectionList;
            [ReadOnly] internal NativeList<byte> m_DataStream;
            internal NetworkPipelineProcessor.Concurrent m_PipelineProcessor;
            internal UdpCHeader.HeaderFlags m_DefaultHeaderFlags;
            internal NativeQueue<QueuedSendMessage>.ParallelWriter m_ConcurrentParallelSendQueue;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [NativeSetThreadIndex] internal int m_ThreadIndex;
            [NativeDisableParallelForRestriction] internal NativeArray<int> m_PendingBeginSend;
#endif
        }

        internal struct Connection
        {
            public NetworkInterfaceEndPoint Address;
            public long LastAttempt;
            public int Id;
            public int Version;
            public int Attempts;
            public NetworkConnection.State State;
            public ushort ReceiveToken;
            public ushort SendToken;
            public byte DidReceiveData;

            public static bool operator ==(Connection lhs, Connection rhs)
            {
                return lhs.Id == rhs.Id && lhs.Version == rhs.Version && lhs.Address == rhs.Address;
            }

            public static bool operator !=(Connection lhs, Connection rhs)
            {
                return lhs.Id != rhs.Id || lhs.Version != rhs.Version || lhs.Address != rhs.Address;
            }

            public override bool Equals(object compare)
            {
                return this == (Connection) compare;
            }

            public static Connection Null => new Connection() {Id = 0, Version = 0};

            public override int GetHashCode()
            {
                return Id;
            }

            public bool Equals(Connection connection)
            {
                return connection.Id == Id && connection.Version == Version && connection.Address == Address;
            }
        }

        // internal variables :::::::::::::::::::::::::::::::::::::::::::::::::
        static List<INetworkInterface> s_NetworkInterfaces = new List<INetworkInterface>();
        static List<INetworkProtocol> s_NetworkProtocols = new List<INetworkProtocol>();

        int m_NetworkInterfaceIndex;
        NetworkSendInterface m_NetworkSendInterface;

        int m_NetworkProtocolIndex;
        NetworkProtocol m_NetworkProtocolInterface;

        NativeQueue<QueuedSendMessage> m_ParallelSendQueue;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        NativeArray<int> m_PendingBeginSend;
#endif

        NetworkEventQueue m_EventQueue;
        private NativeArray<byte> m_DisconnectReasons;

        NativeQueue<int> m_FreeList;
        NativeQueue<int> m_NetworkAcceptQueue;
        NativeList<Connection> m_ConnectionList;
        NativeArray<int> m_InternalState;
        NativeQueue<int> m_PendingFree;
        NativeArray<ushort> m_SessionIdCounter;
        NativeArray<int> m_ErrorCodes;
        enum ErrorCodeType
        {
            ReceiveError = 0,
            SendError = 1,
            NumErrorCodes
        }

#pragma warning disable 649
        struct Parameters
        {
            public NetworkDataStreamParameter dataStream;
            public NetworkConfigParameter config;

            public Parameters(params INetworkParameter[] param)
            {
                config = new NetworkConfigParameter {
                    maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                    connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                    disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                    maxFrameTimeMS = 0
                };
                dataStream = default(NetworkDataStreamParameter);

                for (int i = 0; i < param.Length; ++i)
                {
                    if (param[i] is NetworkConfigParameter)
                        config = (NetworkConfigParameter)param[i];
                    else if (param[i] is NetworkDataStreamParameter)
                        dataStream = (NetworkDataStreamParameter)param[i];
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            public static void ValidateParameters(Parameters param)
            {
                if (param.dataStream.size < 0)
                    throw new ArgumentException($"Value for NetworkDataStreamParameter.size must be larger then zero.");
            }
        }
#pragma warning restore 649

        private Parameters m_NetworkParams;
        private NativeList<byte> m_DataStream;
        private NativeArray<int> m_DataStreamSize;
        private NetworkPipelineProcessor m_PipelineProcessor;
        private UdpCHeader.HeaderFlags m_DefaultHeaderFlags;

        private long m_updateTime;
        private long m_updateTimeAdjustment;

        // properties :::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private const int InternalStateListening = 0;
        private const int InternalStateBound = 1;

        public bool Listening
        {
            get { return (m_InternalState[InternalStateListening] != 0); }
            internal set { m_InternalState[InternalStateListening] = value ? 1 : 0; }
        }

        // 0 = Unbound
        // 1 = Binding
        // 2 = Bound
        public bool Bound => m_InternalState[InternalStateBound] == 2;

        /// <summary>
        /// Helper function for creating a NetworkDriver.
        /// </summary>
        /// <param name="param">
        /// An optional array of INetworkParameter. There are currently only two <see cref="INetworkParameter"/>,
        /// the <see cref="NetworkDataStreamParameter"/> and the <see cref="NetworkConfigParameter"/>.
        /// </param>
        /// <exception cref="InvalidOperationException"></exception>
        public static NetworkDriver Create(params INetworkParameter[] param)
        {
#if UNITY_WEBGL
            return new NetworkDriver(new IPCNetworkInterface(), param);
#else
            return new NetworkDriver(new BaselibNetworkInterface(), param);
#endif
        }

        private static int InsertInAvailableIndex<T>(List<T> list, T element)
        {
            var index = -1;
            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] == null)
                {
                    index = i;
                    list[i] = element;
                    break;
                }
            }
            if (index < 0)
            {
                index = list.Count;
                list.Add(element);
            }
            return index;
        }
        
        private static INetworkProtocol GetProtocolForParameters(INetworkParameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                if (parameter is Unity.Networking.Transport.Relay.RelayNetworkParameter)
                    return new Unity.Networking.Transport.Relay.RelayNetworkProtocol();
            }
            return new UnityTransportProtocol();
        }

        public NetworkDriver(INetworkInterface netIf, params INetworkParameter[] param) : this(netIf, GetProtocolForParameters(param), param)
        {
        }

        /// <summary>
        /// Constructor for NetworkDriver.
        /// </summary>
        /// <param name="param">
        /// An array of INetworkParameter. There are currently only two <see cref="INetworkParameter"/>,
        /// the <see cref="NetworkDataStreamParameter"/> and the <see cref="NetworkConfigParameter"/>.
        /// </param>
        /// <exception cref="ArgumentException">Thrown if the value for NetworkDataStreamParameter.size is smaller then zero.</exception>
        internal NetworkDriver(INetworkInterface netIf, INetworkProtocol netProtocol, params INetworkParameter[] param)
        {
            m_NetworkParams = new Parameters(param);
            Parameters.ValidateParameters(m_NetworkParams);
            NetworkPipelineParams.ValidateParameters(param);

            netProtocol.Initialize(param);
            m_NetworkProtocolIndex = InsertInAvailableIndex(s_NetworkProtocols, netProtocol);
            m_NetworkProtocolInterface = netProtocol.CreateProtocolInterface();

            m_NetworkInterfaceIndex = InsertInAvailableIndex(s_NetworkInterfaces, netIf);

            var result = netIf.Initialize(param);
            if (0 != result)
            {
                throw new InvalidOperationException($"Failed to initialize the NetworkInterface. Error Code: {result}.");
            }

            m_NetworkSendInterface = netIf.CreateSendInterface();

            m_PipelineProcessor = new NetworkPipelineProcessor(param);
            m_ParallelSendQueue = new NativeQueue<QueuedSendMessage>(Allocator.Persistent);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_PendingBeginSend = new NativeArray<int>(JobsUtility.MaxJobThreadCount * JobsUtility.CacheLineSize/4, Allocator.Persistent);
#endif
            var time = Stopwatch.GetTimestamp() / (Stopwatch.Frequency/1000);
            m_updateTime = m_NetworkParams.config.fixedFrameTimeMS > 0 ? 1 : time;
            m_updateTimeAdjustment = 0;
            int initialStreamSize = m_NetworkParams.dataStream.size;
            if (initialStreamSize == 0)
                initialStreamSize = NetworkParameterConstants.DriverDataStreamSize;

            m_DataStream = new NativeList<byte>(initialStreamSize, Allocator.Persistent);
            m_DataStream.ResizeUninitialized(initialStreamSize);
            m_DataStreamSize = new NativeArray<int>(1, Allocator.Persistent);

            m_DefaultHeaderFlags = 0;

            m_NetworkAcceptQueue = new NativeQueue<int>(Allocator.Persistent);

            m_ConnectionList = new NativeList<Connection>(1, Allocator.Persistent);

            m_FreeList = new NativeQueue<int>(Allocator.Persistent);
            m_EventQueue = new NetworkEventQueue(NetworkParameterConstants.InitialEventQueueSize);

            const int reasons = (int)DisconnectReason.Count;
            m_DisconnectReasons = new NativeArray<byte>(reasons, Allocator.Persistent);
            for (var idx = 0; idx < reasons; ++idx)
                m_DisconnectReasons[idx] = (byte)idx;

            m_InternalState = new NativeArray<int>(2, Allocator.Persistent);
            m_PendingFree = new NativeQueue<int>(Allocator.Persistent);

            m_ReceiveCount = new NativeArray<int>(1, Allocator.Persistent);
            m_SessionIdCounter = new NativeArray<ushort>(1, Allocator.Persistent) {[0] = RandomHelpers.GetRandomUShort()};
            m_ErrorCodes = new NativeArray<int>((int)ErrorCodeType.NumErrorCodes, Allocator.Persistent);
            ReceiveCount = 0;
            Listening = false;
        }

        // interface implementation :::::::::::::::::::::::::::::::::::::::::::
        public void Dispose()
        {
            s_NetworkProtocols[m_NetworkProtocolIndex].Dispose();
            s_NetworkProtocols[m_NetworkProtocolIndex] = null;

            s_NetworkInterfaces[m_NetworkInterfaceIndex].Dispose();
            s_NetworkInterfaces[m_NetworkInterfaceIndex] = null;

            m_NetworkProtocolIndex = -1;
            m_NetworkInterfaceIndex = -1;

            m_DataStream.Dispose();
            m_DataStreamSize.Dispose();
            m_PipelineProcessor.Dispose();

            m_EventQueue.Dispose();
            m_DisconnectReasons.Dispose();

            m_NetworkAcceptQueue.Dispose();
            m_ConnectionList.Dispose();
            m_FreeList.Dispose();
            m_InternalState.Dispose();
            m_PendingFree.Dispose();
            m_ReceiveCount.Dispose();
            m_SessionIdCounter.Dispose();
            m_ErrorCodes.Dispose();
            m_ParallelSendQueue.Dispose();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_PendingBeginSend.Dispose();
#endif
        }

        public bool IsCreated => m_InternalState.IsCreated;

        [BurstCompile]
        struct UpdateJob : IJob
        {
            public NetworkDriver driver;

            public void Execute()
            {
                driver.InternalUpdate();
            }
        }

        struct ClearEventQueue : IJob
        {
            public NativeList<byte> dataStream;
            public NativeArray<int> dataStreamSize;
            public NetworkEventQueue eventQueue;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            public NativeArray<int> pendingSend;
            [ReadOnly] public NativeList<Connection> connectionList;
            [ReadOnly] public NativeArray<int> internalState;
#endif
            public void Execute()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                for (int i = 0; i < connectionList.Length; ++i)
                {
                    int conCount = eventQueue.GetCountForConnection(i);
                    if (conCount != 0 && connectionList[i].State != NetworkConnection.State.Disconnected)
                    {
                        UnityEngine.Debug.LogError($"Resetting event queue with pending events (Count={conCount}, ConnectionID={i}) Listening: {internalState[InternalStateListening]}");
                    }
                }
                bool didPrint = false;
                for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
                {
                    if (pendingSend[i * JobsUtility.CacheLineSize / 4] > 0)
                    {
                        pendingSend[i * JobsUtility.CacheLineSize / 4] = 0;
                        if (!didPrint)
                        {
                            UnityEngine.Debug.LogError(
                                "Missing EndSend, calling BeginSend without calling EndSend will result in a memory leak");
                            didPrint = true;
                        }
                    }
                }
#endif
                eventQueue.Clear();
                dataStream.ResizeUninitialized(dataStream.Length);
                dataStreamSize[0] = 0;
            }
        }

        public long LastUpdateTime => m_updateTime;

        public JobHandle ScheduleUpdate(JobHandle dep = default(JobHandle))
        {
            long timeNow = m_NetworkParams.config.fixedFrameTimeMS > 0 ? m_updateTime + m_NetworkParams.config.fixedFrameTimeMS :
                Stopwatch.GetTimestamp() / (Stopwatch.Frequency/1000) - m_updateTimeAdjustment;
            if (m_NetworkParams.config.maxFrameTimeMS > 0 && (timeNow - m_updateTime) > m_NetworkParams.config.maxFrameTimeMS)
            {
                m_updateTimeAdjustment += (timeNow - m_updateTime) - m_NetworkParams.config.maxFrameTimeMS;
                timeNow = m_updateTime + m_NetworkParams.config.maxFrameTimeMS;
            }
            m_updateTime = timeNow;
            var job = new UpdateJob {driver = this};
            JobHandle handle;
            var clearJob = new ClearEventQueue
            {
                dataStream = m_DataStream,
                dataStreamSize = m_DataStreamSize,
                eventQueue = m_EventQueue,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                pendingSend = m_PendingBeginSend,
                connectionList = m_ConnectionList,
                internalState = m_InternalState
#endif
            };
            handle = clearJob.Schedule(dep);
            handle = job.Schedule(handle);
            handle = s_NetworkInterfaces[m_NetworkInterfaceIndex].ScheduleReceive(new NetworkPacketReceiver{m_Driver = this}, handle);
            handle = s_NetworkInterfaces[m_NetworkInterfaceIndex].ScheduleSend(m_ParallelSendQueue, handle);
            return handle;
        }
        public JobHandle ScheduleFlushSend(JobHandle dep)
        {
            return s_NetworkInterfaces[m_NetworkInterfaceIndex].ScheduleSend(m_ParallelSendQueue, dep);
        }
        void InternalUpdate()
        {
            m_PipelineProcessor.Timestamp = m_updateTime;
            while (m_PendingFree.TryDequeue(out var free))
            {
                int ver = m_ConnectionList[free].Version + 1;
                if (ver == 0)
                    ver = 1;
                m_ConnectionList[free] = new Connection {Id = free, Version = ver};
                m_FreeList.Enqueue(free);
            }

            CheckTimeouts();

            if (m_NetworkProtocolInterface.NeedsUpdate)
            {
                var queueHandle = NetworkSendQueueHandle.ToTempHandle(m_ParallelSendQueue.AsParallelWriter());
                m_NetworkProtocolInterface.Update.Ptr.Invoke(m_updateTime, ref m_NetworkSendInterface, ref queueHandle, m_NetworkProtocolInterface.UserData);
            }

            m_PipelineProcessor.UpdateReceive(this, out var updateCount);

            // TODO: Find a good way to establish a good limit (connections*pipelines/2?)
            if (updateCount > (m_ConnectionList.Length - m_FreeList.Count) * 64)
            {
                UnityEngine.Debug.LogWarning(
                    FixedString.Format("A lot of pipeline updates have been queued, possibly too many being scheduled in pipeline logic, queue count: {0}", updateCount));
            }

            m_DefaultHeaderFlags = UdpCHeader.HeaderFlags.HasPipeline;
            m_PipelineProcessor.UpdateSend(ToConcurrentSendOnly(), out updateCount);
            if (updateCount > (m_ConnectionList.Length - m_FreeList.Count) * 64)
            {
                UnityEngine.Debug.LogWarning(
                    FixedString.Format("A lot of pipeline updates have been queued, possibly too many being scheduled in pipeline logic, queue count: {0}", updateCount));
            }

            m_DefaultHeaderFlags = 0;
        }

        /// <summary>
        /// Create a new pipeline.
        /// </summary>
        /// <param name="stages">
        /// An array of stages the pipeline should contain.
        /// </param>
        /// <exception cref="InvalidOperationException">If the driver is not created properly</exception>
        /// <exception cref="InvalidOperationException">A connection has already been established</exception>
        public NetworkPipeline CreatePipeline(params Type[] stages)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");
            if (m_ConnectionList.Length > 0)
                throw new InvalidOperationException(
                    "Pipelines cannot be created after establishing connections");
#endif
            return m_PipelineProcessor.CreatePipeline(stages);
        }

        /// <summary>
        /// Bind the driver to a endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint to bind to.</param>
        /// <value>Returns 0 on success. And a negative value if a error occured.</value>
        /// <exception cref="InvalidOperationException">If the driver is not created properly</exception>
        /// <exception cref="InvalidOperationException">If bind is called more then once on the driver</exception>
        /// <exception cref="InvalidOperationException">If bind is called after a connection has already been established</exception>
        public int Bind(NetworkEndPoint endpoint)
        {
            var ifEndPoint = new NetworkInterfaceEndPoint();
            if (s_NetworkInterfaces[m_NetworkInterfaceIndex].CreateInterfaceEndPoint(endpoint, out ifEndPoint) != 0)
            {
                return -1;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");
            // question: should this really be an error?
            if (m_InternalState[InternalStateBound] != 0)
                throw new InvalidOperationException(
                    "Bind can only be called once per NetworkDriver");
            if (m_ConnectionList.Length > 0)
                throw new InvalidOperationException(
                    "Bind cannot be called after establishing connections");
#endif
            var protocolBind = s_NetworkProtocols[m_NetworkProtocolIndex].Bind(s_NetworkInterfaces[m_NetworkInterfaceIndex], ref ifEndPoint);

            if (protocolBind < 0)
                return protocolBind;

            m_InternalState[InternalStateBound] = protocolBind;
            
            return 0;
        }

        /// <summary>
        /// Set the driver to Listen for incomming connections
        /// </summary>
        /// <value>Returns 0 on success.</value>
        /// <exception cref="InvalidOperationException">If the driver is not created properly</exception>
        /// <exception cref="InvalidOperationException">If listen is called more then once on the driver</exception>
        /// <exception cref="InvalidOperationException">If bind has not been called before calling Listen.</exception>
        public int Listen()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");

            if (Listening)
                throw new InvalidOperationException(
                    "Listen can only be called once per NetworkDriver");
            if (!Bound)
                throw new InvalidOperationException(
                    "Listen can only be called after a successful call to Bind");
#endif
            if (!Bound)
                return -1;
            var ret = s_NetworkInterfaces[m_NetworkInterfaceIndex].Listen();
            if (ret == 0)
                Listening = true;
            return ret;
        }

        /// <summary>
        /// Checks to see if there are any new connections to Accept.
        /// </summary>
        /// <value>If accept fails it returnes a default NetworkConnection.</value>
        public NetworkConnection Accept()
        {
            if (!Listening)
                return default(NetworkConnection);

            if (!m_NetworkAcceptQueue.TryDequeue(out var id))
                return default(NetworkConnection);
            return new NetworkConnection {m_NetworkId = id, m_NetworkVersion = m_ConnectionList[id].Version};
        }

        /// <summary>
        /// Connects the driver to a endpoint
        /// </summary>
        /// <value>If connect fails it returns a default NetworkConnection.</value>
        /// <exception cref="InvalidOperationException">If the driver is not created properly</exception>
        public NetworkConnection Connect(NetworkEndPoint endpoint)
        {
            var address = new NetworkInterfaceEndPoint();
            if (s_NetworkProtocols[m_NetworkProtocolIndex].Connect(s_NetworkInterfaces[m_NetworkInterfaceIndex], endpoint, out address) != 0)
            {
                return default;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_InternalState.IsCreated)
                throw new InvalidOperationException(
                    "Driver must be constructed with a populated or empty INetworkParameter params list");
#endif
            if (!m_FreeList.TryDequeue(out var id))
            {
                id = m_ConnectionList.Length;
                m_ConnectionList.Add(new Connection{Id = id, Version = 1});
            }

            int ver = m_ConnectionList[id].Version;
            var c = new Connection
            {
                Id = id,
                Version = ver,
                State = NetworkConnection.State.Connecting,
                Address = address,
                Attempts = 1,
                LastAttempt = m_updateTime,
                SendToken = 0,
                ReceiveToken = m_SessionIdCounter[0]
            };
            m_SessionIdCounter[0] = (ushort)(m_SessionIdCounter[0] + 1);

            m_ConnectionList[id] = c;
            var netcon = new NetworkConnection {m_NetworkId = id, m_NetworkVersion = ver};
            SendConnectionRequest(c);
            m_PipelineProcessor.initializeConnection(netcon);

            return netcon;
        }

        void SendConnectionRequest(Connection c)
        {
            var queueHandle = NetworkSendQueueHandle.ToTempHandle(m_ParallelSendQueue.AsParallelWriter());
            m_NetworkProtocolInterface.ProcessSendConnectionRequest.Ptr.Invoke(ref c, ref m_NetworkSendInterface, ref queueHandle, m_NetworkProtocolInterface.UserData);
        }

        /// <summary>
        /// Disconnects a NetworkConnection
        /// </summary>
        /// <param name="id">The NetworkConnection we want to Disconnect.</param>
        /// <value>Return 0 on success.</value>
        public int Disconnect(NetworkConnection id)
        {
            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null)
                return 0;

            if (connection.State == NetworkConnection.State.Connected)
            {
                var queueHandle = NetworkSendQueueHandle.ToTempHandle(m_ParallelSendQueue.AsParallelWriter());
                m_NetworkProtocolInterface.ProcessSendDisconnect.Ptr.Invoke(ref connection, ref m_NetworkSendInterface, ref queueHandle, m_NetworkProtocolInterface.UserData);
            }
            RemoveConnection(connection);

            return 0;
        }

        /// <summary>
        /// Returns the PipelineBuffers for a specific pipeline and stage.
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="stageId"></param>
        /// <param name="connection"></param>
        /// <param name="readProcessingBuffer"></param>
        /// <param name="writeProcessingBuffer"></param>
        /// <param name="sharedProcessingBuffer"></param>
        /// <exception cref="InvalidOperationException">If the the connection is invalid.</exception>
        public void GetPipelineBuffers(NetworkPipeline pipeline, NetworkPipelineStageId stageId, NetworkConnection connection, out NativeArray<byte> readProcessingBuffer, out NativeArray<byte> writeProcessingBuffer, out NativeArray<byte> sharedBuffer)
        {
            if (connection.m_NetworkId < 0 || connection.m_NetworkId >= m_ConnectionList.Length ||
                 m_ConnectionList[connection.m_NetworkId].Version != connection.m_NetworkVersion)
                throw new InvalidOperationException("Invalid connection");
            m_PipelineProcessor.GetPipelineBuffers(pipeline, stageId, connection, out readProcessingBuffer, out writeProcessingBuffer, out sharedBuffer);
        }

        public NetworkConnection.State GetConnectionState(NetworkConnection con)
        {
            Connection connection;
            if ((connection = GetConnection(con)) == Connection.Null)
                return NetworkConnection.State.Disconnected;
            return connection.State;
        }

        public NetworkEndPoint RemoteEndPoint(NetworkConnection id)
        {
            if (id == default(NetworkConnection))
                return default(NetworkEndPoint);

            Connection connection;
            if ((connection = GetConnection(id)) == Connection.Null)
                return default(NetworkEndPoint);
            return s_NetworkProtocols[m_NetworkProtocolIndex].GetRemoteEndPoint(s_NetworkInterfaces[m_NetworkInterfaceIndex], connection.Address);
        }

        public NetworkEndPoint LocalEndPoint()
        {
            var ep = s_NetworkInterfaces[m_NetworkInterfaceIndex].LocalEndPoint;
            return s_NetworkInterfaces[m_NetworkInterfaceIndex].GetGenericEndPoint(ep);
        }

        public int MaxHeaderSize(NetworkPipeline pipe)
        {
            return ToConcurrentSendOnly().MaxHeaderSize(pipe);
        }

        public int BeginSend(NetworkPipeline pipe, NetworkConnection id, out DataStreamWriter writer, int requiredPayloadSize = 0)
        {
            return ToConcurrentSendOnly().BeginSend(pipe, id, out writer, requiredPayloadSize);
        }
        public int BeginSend(NetworkConnection id, out DataStreamWriter writer, int requiredPayloadSize = 0)
        {
            return ToConcurrentSendOnly().BeginSend(NetworkPipeline.Null, id, out writer, requiredPayloadSize);
        }
        public int EndSend(DataStreamWriter writer)
        {
            return ToConcurrentSendOnly().EndSend(writer);
        }
        public void AbortSend(DataStreamWriter writer)
        {
            ToConcurrentSendOnly().AbortSend(writer);
        }

        /// <summary>
        /// Pops an event
        /// </summary>
        /// <param name="con"></param>
        /// <param name="reader"></param>
        /// <value>Returns the type of event received, if the value is a <see cref="NetworkEvent.Type.Disconnect"/> event
        /// then the DataStreamReader will contain the disconnect reason.<value/>
        public NetworkEvent.Type PopEvent(out NetworkConnection con, out DataStreamReader reader)
        {
            return PopEvent(out con, out reader, out var _);
        }

        public NetworkEvent.Type PopEvent(out NetworkConnection con, out DataStreamReader reader, out NetworkPipeline pipeline)
        {
            reader = default(DataStreamReader);
            var type = m_EventQueue.PopEvent(out var id, out var offset, out var size, out var pipelineId);
            pipeline = new NetworkPipeline { Id = pipelineId };

            if (type == NetworkEvent.Type.Disconnect && offset < 0)
                reader = new DataStreamReader(((NativeArray<byte>)m_DisconnectReasons).GetSubArray(math.abs(offset), 1));
            else if (size > 0)
                reader = new DataStreamReader(((NativeArray<byte>)m_DataStream).GetSubArray(offset, size));
            con = id < 0
                ? default(NetworkConnection)
                : new NetworkConnection {m_NetworkId = id, m_NetworkVersion = m_ConnectionList[id].Version};

            return type;
        }

        /// <summary>
        /// Pops an event for a specific connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="reader"></param>
        /// <value>Returns the type of event received, if the value is a <see cref="NetworkEvent.Type.Disconnect"/> event
        /// then the DataStreamReader will contain the disconnect reason.<value/>
        public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out DataStreamReader reader)
        {
            return PopEventForConnection(connectionId, out reader, out var _);
        }

        public NetworkEvent.Type PopEventForConnection(NetworkConnection connectionId, out DataStreamReader reader, out NetworkPipeline pipeline)
        {
            reader = default(DataStreamReader);
            pipeline = default(NetworkPipeline);

            if (connectionId.m_NetworkId < 0 || connectionId.m_NetworkId >= m_ConnectionList.Length ||
                m_ConnectionList[connectionId.m_NetworkId].Version != connectionId.m_NetworkVersion)
                return (int) NetworkEvent.Type.Empty;
            var type = m_EventQueue.PopEventForConnection(connectionId.m_NetworkId, out var offset, out var size, out var pipelineId);
            pipeline = new NetworkPipeline { Id = pipelineId };

            if (type == NetworkEvent.Type.Disconnect && offset < 0)
                reader = new DataStreamReader(((NativeArray<byte>)m_DisconnectReasons).GetSubArray(math.abs(offset), 1));
            else if (size > 0)
                reader = new DataStreamReader(((NativeArray<byte>)m_DataStream).GetSubArray(offset, size));

            return type;
        }

        /// <summary>
        /// Returns the size of the eventqueue for a specific connection
        /// </summary>
        /// <param name="connectionId"></param>
        /// <value>If the connection is valid it returns the size of the event queue otherwise it returns 0.</value>
        public int GetEventQueueSizeForConnection(NetworkConnection connectionId)
        {
            if (connectionId.m_NetworkId < 0 || connectionId.m_NetworkId >= m_ConnectionList.Length ||
                m_ConnectionList[connectionId.m_NetworkId].Version != connectionId.m_NetworkVersion)
                return 0;
            return m_EventQueue.GetCountForConnection(connectionId.m_NetworkId);
        }

        // internal helper functions ::::::::::::::::::::::::::::::::::::::::::
        void AddConnection(int id)
        {
            m_EventQueue.PushEvent(new NetworkEvent {connectionId = id, type = NetworkEvent.Type.Connect});
        }

        void AddDisconnection(int id, Error.DisconnectReason reason = DisconnectReason.Default)
        {
            m_EventQueue.PushEvent(new NetworkEvent { connectionId = id, type = NetworkEvent.Type.Disconnect, status = (int)reason });
        }

        Connection GetConnection(NetworkConnection id)
        {
            if (id.m_NetworkId < 0 || id.m_NetworkId >= m_ConnectionList.Length)
                return Connection.Null;

            var con = m_ConnectionList[id.m_NetworkId];
            if (con.Version != id.m_NetworkVersion)
                return Connection.Null;
            return con;
        }

        Connection GetConnection(NetworkInterfaceEndPoint address, ushort sessionId)
        {
            for (int i = 0; i < m_ConnectionList.Length; i++)
            {
                if (address == m_ConnectionList[i].Address && m_ConnectionList[i].ReceiveToken == sessionId )
                    return m_ConnectionList[i];
            }

            return Connection.Null;
        }

        Connection GetNewConnection(NetworkInterfaceEndPoint address, ushort sessionId)
        {
            for (int i = 0; i < m_ConnectionList.Length; i++)
            {
                if (address == m_ConnectionList[i].Address && m_ConnectionList[i].SendToken == sessionId )
                    return m_ConnectionList[i];
            }

            return Connection.Null;
        }

        void SetConnection(Connection connection)
        {
            m_ConnectionList[connection.Id] = connection;
        }

        bool RemoveConnection(Connection connection)
        {
            if (connection.State != NetworkConnection.State.Disconnected && connection == m_ConnectionList[connection.Id])
            {
                connection.State = NetworkConnection.State.Disconnected;
                m_ConnectionList[connection.Id] = connection;
                m_PendingFree.Enqueue(connection.Id);

                return true;
            }

            return false;
        }

        bool UpdateConnection(Connection connection)
        {
            if (connection == m_ConnectionList[connection.Id])
            {
                SetConnection(connection);
                return true;
            }

            return false;
        }

        void CheckTimeouts()
        {
            for (int i = 0; i < m_ConnectionList.Length; ++i)
            {
                var connection = m_ConnectionList[i];
                if (connection == Connection.Null)
                    continue;

                long now = m_updateTime;

                var netcon = new NetworkConnection {m_NetworkId = connection.Id, m_NetworkVersion = connection.Version};
                if ((connection.State == NetworkConnection.State.Connecting ||
                     connection.State == NetworkConnection.State.AwaitingResponse) &&
                    now - connection.LastAttempt > m_NetworkParams.config.connectTimeoutMS)
                {
                    if (connection.Attempts >= m_NetworkParams.config.maxConnectAttempts)
                    {
                        RemoveConnection(connection);
                        AddDisconnection(connection.Id, DisconnectReason.MaxConnectionAttempts);
                        continue;
                    }

                    connection.Attempts = ++connection.Attempts;
                    connection.LastAttempt = now;
                    SetConnection(connection);

                    if (connection.State == NetworkConnection.State.Connecting)
                        SendConnectionRequest(connection);
                    else
                    {
                        var queueHandle = NetworkSendQueueHandle.ToTempHandle(m_ParallelSendQueue.AsParallelWriter());
                        m_NetworkProtocolInterface.ProcessSendConnectionAccept.Ptr.Invoke(ref connection, ref m_NetworkSendInterface, ref queueHandle, m_NetworkProtocolInterface.UserData);
                    }
                }

                if (connection.State == NetworkConnection.State.Connected &&
                    now - connection.LastAttempt > m_NetworkParams.config.disconnectTimeoutMS)
                {
                    Disconnect(netcon);
                    AddDisconnection(connection.Id, DisconnectReason.Timeout);
                }
            }
        }

        public int ReceiveErrorCode
        {
            get { return m_ErrorCodes[(int)ErrorCodeType.ReceiveError]; }
            internal set
            {
                if (value != 0)
                {
                    UnityEngine.Debug.LogError(FixedString.Format("Error on receive, errorCode = {0}", value));
                }
                m_ErrorCodes[(int)ErrorCodeType.ReceiveError] = value;
            }
        }

        // Interface for receiving packages from a NetworkInterface
        internal NativeList<byte> GetDataStream()
        {
            return m_DataStream;
        }
        internal int GetDataStreamSize()
        {
            return m_DataStreamSize[0];
        }

        private NativeArray<int> m_ReceiveCount;
        internal int ReceiveCount {
            get { return m_ReceiveCount[0]; }
            set { m_ReceiveCount[0] = value; }
        }

        internal bool DynamicDataStreamSize()
        {
            return m_NetworkParams.dataStream.size == 0;
        }

        internal bool IsAddressUsed(NetworkInterfaceEndPoint address)
        {
            for (int i = 0; i < m_ConnectionList.Length; i++)
            {
                if (address == m_ConnectionList[i].Address)
                    return true;
            }
            return false;
        }

        internal unsafe int AppendPacket(NetworkInterfaceEndPoint endpoint, int dataLen)
        {
            var count = 0;
            var command = default(ProcessPacketCommand);

            byte* dataStream = (byte*)m_DataStream.GetUnsafePtr() + m_DataStreamSize[0];
            var queueHandle = NetworkSendQueueHandle.ToTempHandle(m_ParallelSendQueue.AsParallelWriter());
            m_NetworkProtocolInterface.ProcessReceive.Ptr.Invoke((IntPtr)dataStream, ref endpoint, dataLen, ref m_NetworkSendInterface, ref queueHandle, m_NetworkProtocolInterface.UserData, ref command);

            switch (command.Type)
            {
                case ProcessPacketCommandType.AddressUpdate:
                {
                    for (int i = 0; i < m_ConnectionList.Length; i++)
                    {
                        if (command.AsAddressUpdate.Address == m_ConnectionList[i].Address && command.AsAddressUpdate.SessionToken == m_ConnectionList[i].ReceiveToken)
                            m_ConnectionList.ElementAt(i).Address = command.AsAddressUpdate.NewAddress;
                    }
                } break;

                case ProcessPacketCommandType.BindAccept:
                {
                    m_InternalState[InternalStateBound] = 2;
                } break;

                case ProcessPacketCommandType.ConnectionAccept:
                {
                    Connection c = GetConnection(command.AsConnectionAccept.Address, command.AsConnectionAccept.SessionId);

                    if (c != Connection.Null)
                    {
                        c.DidReceiveData = 1;

                        if (c.State == NetworkConnection.State.Connected)
                        {
                            //DebugLog("Dropping connect request for an already connected endpoint [" + address + "]");
                            return 0;
                        }

                        if (c.State == NetworkConnection.State.Connecting)
                        {
                            c.SendToken = command.AsConnectionAccept.ConnectionToken;

                            c.State = NetworkConnection.State.Connected;
                            UpdateConnection(c);
                            AddConnection(c.Id);
                            count++;
                        }
                    }
                } break;

                case ProcessPacketCommandType.ConnectionReject:
                    break;

                case ProcessPacketCommandType.ConnectionRequest:
                {
                    if (!Listening)
                        return 0;

                    Connection c = GetNewConnection(command.AsConnectionRequest.Address, command.AsConnectionRequest.SessionId);
                    if (c == Connection.Null || c.State == NetworkConnection.State.Disconnected)
                    {
                        var sessionId = m_SessionIdCounter[0];
                        m_SessionIdCounter[0] = (ushort) (m_SessionIdCounter[0] + 1);
                        if (!m_FreeList.TryDequeue(out var id))
                        {
                            id = m_ConnectionList.Length;
                            m_ConnectionList.Add(new Connection{Id = id, Version = 1});
                        }

                        int ver = m_ConnectionList[id].Version;
                        c = new Connection
                        {
                            Id = id,
                            Version = ver,
                            ReceiveToken = sessionId,
                            SendToken = command.AsConnectionRequest.SessionId,
                            State = NetworkConnection.State.Connected,
                            Address = command.AsConnectionRequest.Address,
                            Attempts = 1,
                            LastAttempt = m_updateTime
                        };
                        SetConnection(c);
                        m_PipelineProcessor.initializeConnection(new NetworkConnection{m_NetworkId = id, m_NetworkVersion = c.Version});
                        m_NetworkAcceptQueue.Enqueue(id);
                        count++;
                    }
                    else
                    {
                        c.Attempts++;
                        c.LastAttempt = m_updateTime;
                        SetConnection(c);
                    }

                    m_NetworkProtocolInterface.ProcessSendConnectionAccept.Ptr.Invoke(ref c, ref m_NetworkSendInterface, ref queueHandle, m_NetworkProtocolInterface.UserData);
                    
                } break;

                case ProcessPacketCommandType.Disconnect:
                {
                    Connection c = GetConnection(command.AsDisconnect.Address, command.AsDisconnect.SessionId);
                    if (c != Connection.Null)
                    {
                        if (RemoveConnection(c))
                            AddDisconnection(c.Id, DisconnectReason.ClosedByRemote);

                        count++;
                    }
                } break;

                case ProcessPacketCommandType.DataWithImplicitConnectionAccept:
                {
                    Connection c = GetConnection(command.AsDataWithImplicitConnectionAccept.Address, command.AsDataWithImplicitConnectionAccept.SessionId);
                    if (c == Connection.Null)
                        return 0;

                    c.DidReceiveData = 1;
                    c.LastAttempt = m_updateTime;
                    UpdateConnection(c);

                    if (c.State == NetworkConnection.State.Connecting)
                    {
                        c.SendToken = command.AsDataWithImplicitConnectionAccept.ConnectionToken;

                        c.State = NetworkConnection.State.Connected;
                        UpdateConnection(c);
                        UnityEngine.Assertions.Assert.IsTrue(!Listening);
                        AddConnection(c.Id);
                        count++;
                    }

                    if (c.State == NetworkConnection.State.Connected)
                    {
                        var sliceOffset = m_DataStreamSize[0] + command.AsDataWithImplicitConnectionAccept.Offset;
                        m_DataStreamSize[0] = sliceOffset + command.AsDataWithImplicitConnectionAccept.Length;

                        if (command.AsDataWithImplicitConnectionAccept.HasPipeline)
                        {
                            var netCon = new NetworkConnection {m_NetworkId = c.Id, m_NetworkVersion = c.Version};
                            m_PipelineProcessor.Receive(this, netCon, ((NativeArray<byte>)m_DataStream).GetSubArray(sliceOffset, command.AsDataWithImplicitConnectionAccept.Length));

                            // TODO: is this return 0 intended? shouldn't this be return count + 1
                            return 0;
                        }

                        m_EventQueue.PushEvent(new NetworkEvent
                        {
                            connectionId = c.Id,
                            type = NetworkEvent.Type.Data,
                            offset = sliceOffset,
                            size = command.AsDataWithImplicitConnectionAccept.Length
                        });
                        count++;
                    }
                } break;

                case ProcessPacketCommandType.Data:
                {
                    Connection c = GetConnection(command.AsData.Address, command.AsData.SessionId);
                    if (c == Connection.Null)
                        return 0;

                    c.DidReceiveData = 1;
                    c.LastAttempt = m_updateTime;
                    UpdateConnection(c);
                    
                    if (c.State == NetworkConnection.State.Connected)
                    {
                        var sliceOffset = m_DataStreamSize[0] + command.AsData.Offset;
                        m_DataStreamSize[0] = sliceOffset + command.AsData.Length;

                        if (command.AsData.HasPipeline)
                        {
                            var netCon = new NetworkConnection {m_NetworkId = c.Id, m_NetworkVersion = c.Version};
                            m_PipelineProcessor.Receive(this, netCon, ((NativeArray<byte>)m_DataStream).GetSubArray(sliceOffset, command.AsData.Length));

                            // TODO: is this return 0 intended? shouldn't this be return count + 1
                            return 0;
                        }

                        m_EventQueue.PushEvent(new NetworkEvent
                        {
                            connectionId = c.Id,
                            type = NetworkEvent.Type.Data,
                            offset = sliceOffset,
                            size = command.AsData.Length
                        });
                        count++;
                    }
                } break;

                case ProcessPacketCommandType.Drop:
                    break;
            }

            return count;
        }

        // Interface for receiving data from a pipeline
        internal unsafe void PushDataEvent(NetworkConnection con, int pipelineId, byte* dataPtr, int dataLength)
        {
            byte* streamBasePtr = (byte*)m_DataStream.GetUnsafePtr();
            int sliceOffset = 0;
            if (dataPtr >= streamBasePtr && dataPtr + dataLength <= streamBasePtr + m_DataStreamSize[0])
            {
                // Pointer is a subset of our receive buffer, no need to copy
                sliceOffset = (int)(dataPtr - streamBasePtr);
            }
            else
            {
                if (DynamicDataStreamSize())
                {
                    while (m_DataStreamSize[0] + dataLength >= m_DataStream.Length)
                        m_DataStream.ResizeUninitialized(m_DataStream.Length * 2);
                }
                else if (m_DataStreamSize[0] + dataLength >= m_DataStream.Length)
                    return; // FIXME: how do we signal this error?

                sliceOffset = m_DataStreamSize[0];
                streamBasePtr = (byte*)m_DataStream.GetUnsafePtr();
                UnsafeUtility.MemCpy(streamBasePtr + sliceOffset, dataPtr, dataLength);
                m_DataStreamSize[0] = sliceOffset + dataLength;
            }

            m_EventQueue.PushEvent(new NetworkEvent
            {
                pipelineId = (short)pipelineId,
                connectionId = con.m_NetworkId,
                type = NetworkEvent.Type.Data,
                offset = sliceOffset,
                size = dataLength
            });
        }
    }
}
