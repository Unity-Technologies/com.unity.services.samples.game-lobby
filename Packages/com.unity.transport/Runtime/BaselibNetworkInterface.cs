#if !UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Baselib.LowLevel;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport.Utilities.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using ErrorState = Unity.Baselib.LowLevel.Binding.Baselib_ErrorState;
using ErrorCode = Unity.Baselib.LowLevel.Binding.Baselib_ErrorCode;

namespace Unity.Networking.Transport
{
    using NetworkRequest = Binding.Baselib_RegisteredNetwork_Request;
    using NetworkEndpoint = Binding.Baselib_RegisteredNetwork_Endpoint;
    using NetworkSocket = Binding.Baselib_RegisteredNetwork_Socket_UDP;

    public struct BaselibNetworkParameter : INetworkParameter
    {
        public int receiveQueueCapacity;
        public int sendQueueCapacity;
        public uint maximumPayloadSize;
    }

    [BurstCompile]
    public struct BaselibNetworkInterface : INetworkInterface
    {
        public static BaselibNetworkParameter DefaultParameters = new BaselibNetworkParameter
        {
            receiveQueueCapacity = k_defaultRxQueueSize,
            sendQueueCapacity = k_defaultTxQueueSize,
            maximumPayloadSize = NetworkParameterConstants.MTU
        };

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private class SocketList
        {
            public struct SocketId
            {
                public NetworkSocket socket;
            }
            public List<SocketId> OpenSockets = new List<SocketId>();

            ~SocketList()
            {
                foreach (var socket in OpenSockets)
                {
                    Binding.Baselib_RegisteredNetwork_Socket_UDP_Close(socket.socket);
                }
            }
        }
        private static SocketList AllSockets = new SocketList();
#endif
        struct Payloads : IDisposable
        {
            public UnsafeAtomicFreeList m_Handles;
            public UnsafeBaselibNetworkArray m_PayloadArray;
            public UnsafeBaselibNetworkArray m_EndpointArray;
            private uint m_PayloadSize;

            public int InUse => m_Handles.InUse;
            public int Capacity => m_Handles.Capacity;

            public Payloads(int capacity, uint maxPayloadSize)
            {
                m_PayloadSize = maxPayloadSize;
                m_Handles = new UnsafeAtomicFreeList(capacity, Allocator.Persistent);
                m_PayloadArray = new UnsafeBaselibNetworkArray(capacity * (int)maxPayloadSize);
                m_EndpointArray = new UnsafeBaselibNetworkArray(capacity * (int)Binding.Baselib_RegisteredNetwork_Endpoint_MaxSize);
            }

            public bool IsCreated => m_Handles.IsCreated;
            public void Dispose()
            {
                m_Handles.Dispose();
                m_PayloadArray.Dispose();
                m_EndpointArray.Dispose();
            }

            public NetworkRequest GetRequestFromHandle(int handle)
            {
                return new NetworkRequest {payload = m_PayloadArray.AtIndexAsSlice(handle, m_PayloadSize),
                    remoteEndpoint = new NetworkEndpoint{slice = m_EndpointArray.AtIndexAsSlice(handle, (uint)Binding.Baselib_RegisteredNetwork_Endpoint_MaxSize)}};
            }

            public int AcquireHandle()
            {
                return m_Handles.Pop();
            }

            public void ReleaseHandle(int handle)
            {
                m_Handles.Push(handle);
            }
        }

        private BaselibNetworkParameter configuration;

        private const int k_defaultRxQueueSize = 64;
        private const int k_defaultTxQueueSize = 64;

        unsafe struct BaselibData
        {
            public NetworkSocket m_Socket;
            public Payloads m_PayloadsTx;
        }

        [ReadOnly]
        private NativeArray<BaselibData> m_Baselib;

        [NativeDisableContainerSafetyRestriction]
        private Payloads m_PayloadsRx;
        [NativeDisableContainerSafetyRestriction]
        private Payloads m_PayloadsTx;

        private UnsafeBaselibNetworkArray m_LocalAndTempEndpoint;

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        /// <value>NetworkInterfaceEndPoint</value>
        public unsafe NetworkInterfaceEndPoint LocalEndPoint
        {
            // error handling: handle the errors...
            get
            {
                var error = default(ErrorState);
                Binding.Baselib_NetworkAddress local;
                Binding.Baselib_RegisteredNetwork_Socket_UDP_GetNetworkAddress(m_Baselib[0].m_Socket, &local, &error);
                var ep = default(NetworkInterfaceEndPoint);
                if (error.code != ErrorCode.Success)
                    return ep;
                ep.dataLength = UnsafeUtility.SizeOf<Binding.Baselib_NetworkAddress>();
                UnsafeUtility.MemCpy(ep.data, &local, ep.dataLength);
                return ep;
            }
        }

        public bool IsCreated => m_Baselib.IsCreated;

        /// <summary>
        /// Creates a interface endpoint.
        /// </summary>
        /// <value>NetworkInterfaceEndPoint</value>
        public unsafe int CreateInterfaceEndPoint(NetworkEndPoint address, out NetworkInterfaceEndPoint endpoint)
        {
            var slice = m_LocalAndTempEndpoint.AtIndexAsSlice(0, (uint)Binding.Baselib_RegisteredNetwork_Endpoint_MaxSize);
            var error = default(ErrorState);
            endpoint = default(NetworkInterfaceEndPoint);
            NetworkEndpoint local;

            local = Binding.Baselib_RegisteredNetwork_Endpoint_Create(
                (Binding.Baselib_NetworkAddress*)&address.rawNetworkAddress,
                slice,
                &error);
            if (error.code != ErrorCode.Success)
                return (int)error.code;

            endpoint.dataLength = (int)local.slice.size;
            fixed (void* ptr = endpoint.data)
            {
                UnsafeUtility.MemCpy(ptr, (void*)local.slice.data, endpoint.dataLength);
            }
            return (int) Error.StatusCode.Success;
        }

        public unsafe NetworkEndPoint GetGenericEndPoint(NetworkInterfaceEndPoint endpoint)
        {
            // Set to a valid address so length is set correctly
            var address = NetworkEndPoint.LoopbackIpv4;
            var error = default(ErrorState);
            var slice = m_LocalAndTempEndpoint.AtIndexAsSlice(0, (uint)Binding.Baselib_RegisteredNetwork_Endpoint_MaxSize);
            NetworkEndpoint local;
            local.slice = slice;
            local.slice.size = (uint)endpoint.dataLength;
            UnsafeUtility.MemCpy((void*)local.slice.data, endpoint.data, endpoint.dataLength);
            Binding.Baselib_RegisteredNetwork_Endpoint_GetNetworkAddress(local, &address.rawNetworkAddress, &error);
            if (error.code != ErrorCode.Success)
                return default;
            return address;
        }


        /// <summary>
        /// Initializes a instance of the BaselibNetworkInterface struct.
        /// </summary>
        /// <param name="param">An array of INetworkParameter. There is currently only <see cref="BaselibNetworkParameter"/> that can be passed.</param>
        public unsafe int Initialize(params INetworkParameter[] param)
        {
            if (!TryExtractParameters(out configuration, param))
            {
                configuration = DefaultParameters;
            }

            m_Baselib = new NativeArray<BaselibData>(1, Allocator.Persistent);
            var baselib = default(BaselibData);

            m_PayloadsTx = new Payloads(configuration.sendQueueCapacity, configuration.maximumPayloadSize);
            m_PayloadsRx = new Payloads(configuration.receiveQueueCapacity, configuration.maximumPayloadSize);
            m_LocalAndTempEndpoint = new UnsafeBaselibNetworkArray(2 * (int)Binding.Baselib_RegisteredNetwork_Endpoint_MaxSize);

            baselib.m_PayloadsTx = m_PayloadsTx;

            m_Baselib[0] = baselib;

            var ep = default(NetworkInterfaceEndPoint);
            var result = 0;

            if ((result = CreateInterfaceEndPoint(NetworkEndPoint.AnyIpv4, out ep)) != (int)Error.StatusCode.Success)
                return result;
            return Bind(ep);
        }

        public void Dispose()
        {
            if (m_Baselib[0].m_Socket.handle != IntPtr.Zero)
            {
                #if ENABLE_UNITY_COLLECTIONS_CHECKS
                AllSockets.OpenSockets.Remove(new SocketList.SocketId
                    {socket = m_Baselib[0].m_Socket});
                #endif
                Binding.Baselib_RegisteredNetwork_Socket_UDP_Close(m_Baselib[0].m_Socket);
            }

            m_LocalAndTempEndpoint.Dispose();
            if (m_PayloadsTx.IsCreated)
                m_PayloadsTx.Dispose();
            if (m_PayloadsRx.IsCreated)
                m_PayloadsRx.Dispose();
            m_Baselib.Dispose();
        }

        #region ReceiveJob

        [BurstCompile]
        struct FlushSendJob : IJob
        {
            public Payloads Tx;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<BaselibData> Baselib;
            public unsafe void Execute()
            {
                var error = default(ErrorState);
                var pollCount = 0;
                while(Binding.Baselib_RegisteredNetwork_Socket_UDP_ProcessSend(Baselib[0].m_Socket, &error) == Binding.Baselib_RegisteredNetwork_ProcessStatus.Pending && pollCount++ < k_defaultTxQueueSize){}
                int count;
                // InUse is not thread safe, needs to be called in a single threaded flush job
                var inFlight = Tx.InUse;
                if (inFlight > 0)
                {
                    var results = stackalloc Binding.Baselib_RegisteredNetwork_CompletionResult[inFlight];
                    count = (int)Binding.Baselib_RegisteredNetwork_Socket_UDP_DequeueSend(Baselib[0].m_Socket, results, (uint)inFlight, &error);
                    if (error.code != ErrorCode.Success)
                    {
                        // copy recv flow? e.g. pass
                        return;
                    }
                    for (int i = 0; i < count; ++i)
                    {
                        // return results[i].status through userdata, mask? or allocate space at beginning?
                        // pass through a new NetworkPacketSender.?
                        Tx.ReleaseHandle((int)results[i].requestUserdata - 1);
                    }
                }
            }
        }
        [BurstCompile]
        struct ReceiveJob : IJob
        {
            public NetworkPacketReceiver Receiver;
            public Payloads Rx;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<BaselibData> Baselib;

            public unsafe void Execute()
            {
                var count = 0;
                var outstanding = Rx.InUse;
                var error = default(ErrorState);
                var requests = stackalloc Binding.Baselib_RegisteredNetwork_Request[Rx.Capacity];

                if (outstanding > 0)
                {
                    var pollCount = 0;
                    while (Binding.Baselib_RegisteredNetwork_Socket_UDP_ProcessRecv(Baselib[0].m_Socket, &error) == Binding.Baselib_RegisteredNetwork_ProcessStatus.Pending && pollCount++ < k_defaultRxQueueSize) {}

                    var results = stackalloc Binding.Baselib_RegisteredNetwork_CompletionResult[outstanding];

                    // Pop Completed Requests off the CompletionQ
                    count = (int)Binding.Baselib_RegisteredNetwork_Socket_UDP_DequeueRecv(Baselib[0].m_Socket, results, (uint)outstanding, &error);
                    if (error.code != ErrorCode.Success)
                    {
                        Receiver.ReceiveErrorCode = (int) error.code;
                        return;
                    }

                    // Copy and run Append on each Packet.

                    var stream = Receiver.GetDataStream();
                    var headerLength = UnsafeUtility.SizeOf<UdpCHeader>();
                    var address = default(NetworkInterfaceEndPoint);

                    var indicies = stackalloc int[count];
                    for (int i = 0; i < count; i++)
                    {
                        if (results[i].status == Binding.Baselib_RegisteredNetwork_CompletionStatus.Failed)
                        {
                            continue;
                        }
                        var receivedBytes = (int) results[i].bytesTransferred;
                        var index = (int)results[i].requestUserdata - 1;
                        var packet = Rx.GetRequestFromHandle(index);

                        indicies[i] = index;
                        outstanding--;

                        var payloadLen = receivedBytes;

                        int dataStreamSize = Receiver.GetDataStreamSize();
                        if (Receiver.DynamicDataStreamSize())
                        {
                            while (dataStreamSize + payloadLen >= stream.Length)
                                stream.ResizeUninitialized(stream.Length*2);
                        }
                        else if (dataStreamSize + payloadLen > stream.Length)
                        {
                            Receiver.ReceiveErrorCode = 10040;//(int)ErrorCode.OutOfMemory;
                            continue;
                        }

                        UnsafeUtility.MemCpy(
                            (byte*)stream.GetUnsafePtr() + dataStreamSize,
                            (byte*)packet.payload.data,
                            payloadLen);

                        var remote = packet.remoteEndpoint.slice;
                        address.dataLength = (int)remote.size;
                        UnsafeUtility.MemCpy(address.data, (void*)remote.data, (int)remote.size);
                        Receiver.ReceiveCount += Receiver.AppendPacket(address, receivedBytes);
                    }
                    // Reuse the requests after they have been processed.
                    for (int i = 0; i < count; i++)
                    {
                        requests[i] = Rx.GetRequestFromHandle(indicies[i]);
                        requests[i].requestUserdata = (IntPtr)indicies[i] + 1;
                    }
                }

                while (Rx.InUse  < Rx.Capacity)
                {
                    int handle = Rx.AcquireHandle();
                    requests[count] = Rx.GetRequestFromHandle(handle);
                    requests[count].requestUserdata = (IntPtr)handle + 1;
                    ++count;
                }
                if (count > 0)
                {
                    count = (int) Binding.Baselib_RegisteredNetwork_Socket_UDP_ScheduleRecv(
                        Baselib[0].m_Socket,
                        requests,
                        (uint)count,
                        &error);
                    if (error.code != ErrorCode.Success)
                        Receiver.ReceiveErrorCode = (int) error.code;
                }
            }
        }
        #endregion

        public JobHandle ScheduleReceive(NetworkPacketReceiver receiver, JobHandle dep)
        {
            var job = new ReceiveJob
            {
                Baselib = m_Baselib,
                Rx = m_PayloadsRx,
                Receiver = receiver
            };
            return job.Schedule(dep);
        }

        public JobHandle ScheduleSend(NativeQueue<QueuedSendMessage> sendQueue, JobHandle dep)
        {
            var job = new FlushSendJob
            {
                Baselib = m_Baselib,
                Tx = m_PayloadsTx
            };
            return job.Schedule(dep);
        }


        /// <summary>
        /// Binds the BaselibNetworkInterface to the endpoint passed.
        /// </summary>
        /// <param name="endpoint">A valid ipv4 or ipv6 address</param>
        /// <value>int</value>
        public unsafe int Bind(NetworkInterfaceEndPoint endpoint)
        {
            var baselib = m_Baselib[0];
            if (m_Baselib[0].m_Socket.handle != IntPtr.Zero)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AllSockets.OpenSockets.Remove(new SocketList.SocketId
                    {socket = m_Baselib[0].m_Socket});
#endif
                Binding.Baselib_RegisteredNetwork_Socket_UDP_Close(m_Baselib[0].m_Socket);
                baselib.m_Socket.handle = IntPtr.Zero;

                // Recreate the payloads to make sure we do not loose any items from the queue
                m_PayloadsRx.Dispose();
                m_PayloadsRx = new Payloads(configuration.receiveQueueCapacity, configuration.maximumPayloadSize);
            }

            var slice = m_LocalAndTempEndpoint.AtIndexAsSlice(0, (uint)Binding.Baselib_RegisteredNetwork_Endpoint_MaxSize);
            UnsafeUtility.MemCpy((void*)slice.data, endpoint.data, endpoint.dataLength);

            var error = default(ErrorState);

            NetworkEndpoint local;
            local.slice = slice;

            Binding.Baselib_NetworkAddress localAddress;
            Binding.Baselib_RegisteredNetwork_Endpoint_GetNetworkAddress(local, &localAddress, &error);

            baselib.m_Socket = Binding.Baselib_RegisteredNetwork_Socket_UDP_Create(
                &localAddress,
                Binding.Baselib_NetworkAddress_AddressReuse.Allow,
                checked((uint)configuration.sendQueueCapacity),
                checked((uint)configuration.receiveQueueCapacity),
                &error);
            if (error.code != ErrorCode.Success)
            {
                m_Baselib[0] = baselib;
                return (int) error.code == -1 ? -1 : -(int) error.code;
            }

            // Schedule receive right away so we do not loose packets received before the first call to update
            int count = 0;
            var requests = stackalloc Binding.Baselib_RegisteredNetwork_Request[m_PayloadsRx.Capacity];
            while (m_PayloadsRx.InUse < m_PayloadsRx.Capacity)
            {
                int handle = m_PayloadsRx.AcquireHandle();
                requests[count] = m_PayloadsRx.GetRequestFromHandle(handle);
                requests[count].requestUserdata = (IntPtr)handle + 1;
                ++count;
            }
            if (count > 0)
            {
                Binding.Baselib_RegisteredNetwork_Socket_UDP_ScheduleRecv(
                    baselib.m_Socket,
                    requests,
                    (uint)count,
                    &error);
                // how should this be handled? what are the cases?
                if (error.code != ErrorCode.Success)
                    return (int) error.code == -1 ? -1 : -(int) error.code;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AllSockets.OpenSockets.Add(new SocketList.SocketId
                {socket = baselib.m_Socket});
#endif
            m_Baselib[0] = baselib;
            return 0;
        }
        public int Listen()
        {
            return 0;
        }

        static TransportFunctionPointer<NetworkSendInterface.BeginSendMessageDelegate> BeginSendMessageFunctionPointer = new TransportFunctionPointer<NetworkSendInterface.BeginSendMessageDelegate>(BeginSendMessage);
        static TransportFunctionPointer<NetworkSendInterface.EndSendMessageDelegate> EndSendMessageFunctionPointer = new TransportFunctionPointer<NetworkSendInterface.EndSendMessageDelegate>(EndSendMessage);
        static TransportFunctionPointer<NetworkSendInterface.AbortSendMessageDelegate> AbortSendMessageFunctionPointer = new TransportFunctionPointer<NetworkSendInterface.AbortSendMessageDelegate>(AbortSendMessage);

        public unsafe NetworkSendInterface CreateSendInterface()
        {
            return new NetworkSendInterface
            {
                BeginSendMessage = BeginSendMessageFunctionPointer,
                EndSendMessage = EndSendMessageFunctionPointer,
                AbortSendMessage = AbortSendMessageFunctionPointer,
                UserData = (IntPtr)m_Baselib.GetUnsafePtr()
            };
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(NetworkSendInterface.BeginSendMessageDelegate))]
        private static unsafe int BeginSendMessage(out NetworkInterfaceSendHandle handle, IntPtr userData, int requiredPayloadSize)
        {
            var baselib = (BaselibData*)userData;
            handle = default(NetworkInterfaceSendHandle);
            int index = baselib->m_PayloadsTx.AcquireHandle();
            if (index < 0)
                return (int)Error.StatusCode.NetworkSendQueueFull;

            var message = baselib->m_PayloadsTx.GetRequestFromHandle(index);
            if ((int) message.payload.size < requiredPayloadSize)
            {
                baselib->m_PayloadsTx.ReleaseHandle(index);
                return (int)Error.StatusCode.NetworkPacketOverflow;
            }

            handle.id = index;
            handle.size = 0;
            handle.data = (IntPtr)message.payload.data;
            handle.capacity = (int) message.payload.size;
            return (int)Error.StatusCode.Success;
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(NetworkSendInterface.EndSendMessageDelegate))]
        private static unsafe int EndSendMessage(ref NetworkInterfaceSendHandle handle, ref NetworkInterfaceEndPoint address, IntPtr userData, ref NetworkSendQueueHandle sendQueueHandle)
        {
            var baselib = (BaselibData*)userData;
            int index = handle.id;
            var message = baselib->m_PayloadsTx.GetRequestFromHandle(index);
            message.requestUserdata = (IntPtr) (index + 1);
            message.payload.size = (uint)handle.size;

            var addr = address;
            UnsafeUtility.MemCpy((void*)message.remoteEndpoint.slice.data, addr.data, address.dataLength);

            NetworkRequest* messagePtr = &message;

            var error = default(ErrorState);
            var count = (int) Binding.Baselib_RegisteredNetwork_Socket_UDP_ScheduleSend(
                baselib->m_Socket,
                messagePtr,
                1u,
                &error);
            if (error.code != ErrorCode.Success)
            {
                baselib->m_PayloadsTx.ReleaseHandle(index);
                return (int) error.code == -1 ? -1 : -(int) error.code;
            }
            return handle.size;
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(NetworkSendInterface.AbortSendMessageDelegate))]
        private static unsafe void AbortSendMessage(ref NetworkInterfaceSendHandle handle, IntPtr userData)
        {
            var baselib = (BaselibData*)userData;
            var id = handle.id;
            baselib->m_PayloadsTx.ReleaseHandle(id);
        }

        bool ValidateParameters(BaselibNetworkParameter param)
        {
            if (param.receiveQueueCapacity <= 0)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Debug.LogWarning("Value for receiveQueueCapacity must be larger then zero.");
#endif
                return false;
            }
            if (param.sendQueueCapacity <= 0)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Debug.LogWarning("Value for sendQueueCapacity must be larger then zero.");
#endif
                return false;
            }
            return true;
        }

        /// <summary>
        /// Tries to extract the BaselibNetworkParameter from the param's passed.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="param"></param>
        /// <value>boolean indicating if the extration was successful or not.</value>
        bool TryExtractParameters(out BaselibNetworkParameter config, params INetworkParameter[] param)
        {
            for (int i = 0; i < param.Length; ++i)
            {
                if (param[i] is BaselibNetworkParameter && ValidateParameters((BaselibNetworkParameter) param[i]))
                {
                    config = (BaselibNetworkParameter) param[i];
                    return true;
                }
            }
            config = default;
            return false;
        }
    }
}
#endif // !UNITY_WEBGL
