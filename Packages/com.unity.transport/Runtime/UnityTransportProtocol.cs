using System;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using UnityEngine.Assertions;

namespace Unity.Networking.Transport
{
    [BurstCompile]
    internal struct UnityTransportProtocol : INetworkProtocol
    {
        public void Initialize(INetworkParameter[] netParams) {}
        public void Dispose() {}

        public int Bind(INetworkInterface networkInterface, ref NetworkInterfaceEndPoint localEndPoint)
        {
            if (networkInterface.Bind(localEndPoint) != 0)
                return -1;

            return 2;
        }

        public unsafe int Connect(INetworkInterface networkInterface, NetworkEndPoint endpoint, out NetworkInterfaceEndPoint address)
        {
            return networkInterface.CreateInterfaceEndPoint(endpoint, out address);
        }

        public NetworkEndPoint GetRemoteEndPoint(INetworkInterface networkInterface, NetworkInterfaceEndPoint address)
        {
            return networkInterface.GetGenericEndPoint(address);
        }

        public NetworkProtocol CreateProtocolInterface()
        {
            return new NetworkProtocol (
                computePacketAllocationSize: new TransportFunctionPointer<NetworkProtocol.ComputePacketAllocationSizeDelegate>(ComputePacketAllocationSize),
                processReceive: new TransportFunctionPointer<NetworkProtocol.ProcessReceiveDelegate>(ProcessReceive),
                processSend: new TransportFunctionPointer<NetworkProtocol.ProcessSendDelegate>(ProcessSend),
                processSendConnectionAccept: new TransportFunctionPointer<NetworkProtocol.ProcessSendConnectionAcceptDelegate>(ProcessSendConnectionAccept),
                processSendConnectionRequest: new TransportFunctionPointer<NetworkProtocol.ProcessSendConnectionRequestDelegate>(ProcessSendConnectionRequest),
                processSendDisconnect: new TransportFunctionPointer<NetworkProtocol.ProcessSendDisconnectDelegate>(ProcessSendDisconnect),
                update: new TransportFunctionPointer<NetworkProtocol.UpdateDelegate>(Update),
                needsUpdate: false, // Update is no-op
                userData: IntPtr.Zero,
                maxHeaderSize: UdpCHeader.Length,
                maxFooterSize: 2
            );
        }
 
        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ComputePacketAllocationSizeDelegate))]
        public static int ComputePacketAllocationSize(ref NetworkDriver.Connection connection, ref int dataCapacity, out int dataOffset)
        {
            dataOffset = UdpCHeader.Length;
            var footerSize = connection.DidReceiveData == 0 ? 2 : 0;

            if (dataCapacity == 0)
                dataCapacity = NetworkParameterConstants.MTU - dataOffset - footerSize;
            
            return dataOffset + dataCapacity + footerSize;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessReceiveDelegate))]
        public static void ProcessReceive(IntPtr stream, ref NetworkInterfaceEndPoint endpoint, int size, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData, ref ProcessPacketCommand command)
        {
            unsafe
            {
                var data = (byte*)stream;
                var header = *(UdpCHeader*)data;

                if (size < UdpCHeader.Length)
                {
                    UnityEngine.Debug.LogError("Received an invalid message header");
                    command.Type = ProcessPacketCommandType.Drop;
                    return;
                }

                switch ((UdpCProtocol)header.Type)
                {
                    case UdpCProtocol.ConnectionAccept:
                        if ((header.Flags & UdpCHeader.HeaderFlags.HasConnectToken) == 0)
                        {
                            UnityEngine.Debug.LogError("Received an invalid ConnectionAccept without a token");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        if ((header.Flags & UdpCHeader.HeaderFlags.HasPipeline) != 0)
                        {
                            UnityEngine.Debug.LogError("Received an invalid ConnectionAccept with pipeline");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        if (size != UdpCHeader.Length + 2)
                        {
                            UnityEngine.Debug.LogError("Received an invalid ConnectionAccept with wrongh length");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        command.Type = ProcessPacketCommandType.ConnectionAccept;
                        command.AsConnectionAccept.Address = endpoint;
                        command.AsConnectionAccept.SessionId = header.SessionToken;
                        command.AsConnectionAccept.ConnectionToken = *(ushort*)(stream + UdpCHeader.Length);
                        return;

                    case UdpCProtocol.ConnectionReject:
                        command.Type = ProcessPacketCommandType.ConnectionReject;
                        return;
                    
                    case UdpCProtocol.ConnectionRequest:
                        if ((header.Flags & UdpCHeader.HeaderFlags.HasPipeline) != 0)
                        {
                            UnityEngine.Debug.LogError("Received an invalid ConnectionRequest with pipeline");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        command.Type = ProcessPacketCommandType.ConnectionRequest;
                        command.AsConnectionRequest.Address = endpoint;
                        command.AsConnectionRequest.SessionId = header.SessionToken;
                        return;

                    case UdpCProtocol.Disconnect:
                        if ((header.Flags & UdpCHeader.HeaderFlags.HasPipeline) != 0)
                        {
                            UnityEngine.Debug.LogError("Received an invalid Disconnect with pipeline");
                            command.Type = ProcessPacketCommandType.Drop;
                            return;
                        }

                        command.Type = ProcessPacketCommandType.Disconnect;
                        command.AsDisconnect.SessionId = header.SessionToken;
                        command.AsDisconnect.Address = endpoint;
                        return;

                    case UdpCProtocol.Data:
                        var payloadLength = size - UdpCHeader.Length;
                        var hasPipeline = (header.Flags & UdpCHeader.HeaderFlags.HasPipeline) != 0 ? (byte)1 : (byte)0;
                        var hasConnectionToken = (header.Flags & UdpCHeader.HeaderFlags.HasConnectToken) != 0;

                        if (hasConnectionToken)
                        {
                            payloadLength -= 2;
                            command.Type = ProcessPacketCommandType.DataWithImplicitConnectionAccept;
                            command.AsDataWithImplicitConnectionAccept = new ProcessPacketCommandDataWithImplicitConnectionAccept
                            {
                                SessionId = header.SessionToken,
                                Offset = UdpCHeader.Length,
                                Length = payloadLength,
                                HasPipelineByte = hasPipeline,
                                ConnectionToken = *(ushort*)(stream + UdpCHeader.Length + payloadLength)
                            };
                            command.AsDataWithImplicitConnectionAccept.Address = endpoint;
                            return;
                        }
                        else
                        {
                            command.Type = ProcessPacketCommandType.Data;
                            command.AsData = new ProcessPacketCommandData
                            {
                                SessionId = header.SessionToken,
                                Offset = UdpCHeader.Length,
                                Length = payloadLength,
                                HasPipelineByte = hasPipeline,
                            };
                            command.AsData.Address = endpoint;
                            return;
                        }
                }

                command.Type = ProcessPacketCommandType.Drop;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendDelegate))]
        public static int ProcessSend(ref NetworkDriver.Connection connection, bool hasPipeline, ref NetworkSendInterface sendInterface, ref NetworkInterfaceSendHandle sendHandle, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            WriteSendMessageHeader(ref connection, hasPipeline, ref sendHandle, 0);
            return sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref connection.Address, sendInterface.UserData, ref queueHandle);
        }

        internal static unsafe int WriteSendMessageHeader(ref NetworkDriver.Connection connection, bool hasPipeline, ref NetworkInterfaceSendHandle sendHandle, int offset)
        {
            unsafe
            {
                var flags = default(UdpCHeader.HeaderFlags);
                var capacity = sendHandle.capacity - offset;
                var size = sendHandle.size - offset;

                if (connection.DidReceiveData == 0)
                {
                    flags |= UdpCHeader.HeaderFlags.HasConnectToken;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (size + 2 > capacity)
                        throw new InvalidOperationException("SendHandle capacity overflow");
#endif
                    ushort* connectionToken = (ushort*)((byte*)sendHandle.data + sendHandle.size);
                    *connectionToken = connection.ReceiveToken;
                    sendHandle.size += 2;
                }

                if (hasPipeline)
                {
                    flags |= UdpCHeader.HeaderFlags.HasPipeline;
                }

                UdpCHeader* header = (UdpCHeader*)(sendHandle.data + offset);
                *header = new UdpCHeader
                {
                    Type = (byte)UdpCProtocol.Data,
                    SessionToken = connection.SendToken,
                    Flags = flags
                };

                return sendHandle.size - offset;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendConnectionAcceptDelegate))]
        public static void ProcessSendConnectionAccept(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            unsafe
            {
                NetworkInterfaceSendHandle sendHandle;
                if (sendInterface.BeginSendMessage.Ptr.Invoke(out sendHandle, sendInterface.UserData, UdpCHeader.Length + 2) != 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a ConnectionAccept packet");
                    return;
                }

                byte* packet = (byte*) sendHandle.data;
                var size = WriteConnectionAcceptMessage(ref connection, packet, sendHandle.capacity);

                if (size < 0)
                {
                    sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                    UnityEngine.Debug.LogError("Failed to send a ConnectionAccept packet");
                    return;
                }

                sendHandle.size = size;

                if (sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref connection.Address, sendInterface.UserData, ref queueHandle) < 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a ConnectionAccept packet");
                    return;
                }
            }
        }

        internal static int GetConnectionAcceptMessageMaxLength() => UdpCHeader.Length + 2;

        internal static unsafe int WriteConnectionAcceptMessage(ref NetworkDriver.Connection connection, byte* packet, int capacity)
        {
            var size = UdpCHeader.Length;

            if (connection.DidReceiveData == 0)
                size += 2;

            if (size > capacity)
            {
                UnityEngine.Debug.LogError("Failed to create a ConnectionAccept packet: size exceeds capacity");
                return -1;
            }
                
            var header = (UdpCHeader*) packet;
            *header = new UdpCHeader
            {
                Type = (byte) UdpCProtocol.ConnectionAccept,
                SessionToken = connection.SendToken,
                Flags = 0
            };
            
            if (connection.DidReceiveData == 0)
            {
                header->Flags |= UdpCHeader.HeaderFlags.HasConnectToken;
                *(ushort*)(packet + UdpCHeader.Length) = connection.ReceiveToken;
            }

            Assert.IsTrue(size <= GetConnectionAcceptMessageMaxLength());

            return size;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendConnectionRequestDelegate))]
        public static void ProcessSendConnectionRequest(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            unsafe
            {
                NetworkInterfaceSendHandle sendHandle;
                if (sendInterface.BeginSendMessage.Ptr.Invoke(out sendHandle, sendInterface.UserData, UdpCHeader.Length) != 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                    return;
                }

                byte* packet = (byte*) sendHandle.data;
                sendHandle.size = UdpCHeader.Length;
                if (sendHandle.size > sendHandle.capacity)
                {
                    sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                    UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                    return;
                }
                var header = (UdpCHeader*) packet;
                *header = new UdpCHeader
                {
                    Type = (byte) UdpCProtocol.ConnectionRequest,
                    SessionToken = connection.ReceiveToken,
                    Flags = 0
                };
                if (sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref connection.Address, sendInterface.UserData, ref queueHandle) < 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a ConnectionRequest packet");
                    return;
                }
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.ProcessSendDisconnectDelegate))]
        public static void ProcessSendDisconnect(ref NetworkDriver.Connection connection, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            unsafe
            {
                NetworkInterfaceSendHandle sendHandle;
                if (sendInterface.BeginSendMessage.Ptr.Invoke(out sendHandle, sendInterface.UserData, UdpCHeader.Length) != 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a Disconnect packet");
                    return;
                }

                byte* packet = (byte*) sendHandle.data;
                sendHandle.size = UdpCHeader.Length;
                if (sendHandle.size > sendHandle.capacity)
                {
                    sendInterface.AbortSendMessage.Ptr.Invoke(ref sendHandle, sendInterface.UserData);
                    UnityEngine.Debug.LogError("Failed to send a Disconnect packet");
                    return;
                }
                var header = (UdpCHeader*) packet;
                *header = new UdpCHeader
                {
                    Type = (byte) UdpCProtocol.Disconnect,
                    SessionToken = connection.SendToken,
                    Flags = 0
                };
                if (sendInterface.EndSendMessage.Ptr.Invoke(ref sendHandle, ref connection.Address, sendInterface.UserData, ref queueHandle) < 0)
                {
                    UnityEngine.Debug.LogError("Failed to send a Disconnect packet");
                    return;
                }
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkProtocol.UpdateDelegate))]
        public static void Update(long updateTime, ref NetworkSendInterface sendInterface, ref NetworkSendQueueHandle queueHandle, IntPtr userData)
        {
            // No-op
        }
    }
}