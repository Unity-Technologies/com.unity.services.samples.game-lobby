using System;
using AOT;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Utilities;
using UnityEngine;
using Unity.Burst;

namespace Unity.Networking.Transport.Tests
{
    [BurstCompile]
    public unsafe struct TempDisconnectPipelineStage : INetworkPipelineStage
    {
        public static byte* s_StaticInstanceBuffer;
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            s_StaticInstanceBuffer = staticInstanceBuffer;
            *staticInstanceBuffer = 1;
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 0,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 1;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            if (*ctx.staticInstanceBuffer == 0)
            {
                inboundBuffer = default;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            return (int)Error.StatusCode.Success;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* recvProcessBuffer, int recvProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
        }
    }
    [BurstCompile]
    public unsafe struct TempDisconnectSendPipelineStage : INetworkPipelineStage
    {
        public static byte* s_StaticInstanceBuffer;
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            s_StaticInstanceBuffer = staticInstanceBuffer;
            *staticInstanceBuffer = 1;
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 0,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 1;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            if (*ctx.staticInstanceBuffer == 0)
            {
                inboundBuffer = default;
            }
            return (int)Error.StatusCode.Success;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* recvProcessBuffer, int recvProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
        }
    }

    public struct TempDisconnectPipelineStageCollection
    {
        public static void Register()
        {
            NetworkPipelineStageCollection.RegisterPipelineStage(new TempDisconnectPipelineStage());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TempDisconnectSendPipelineStage());
        }
    }
    public class ReliablePipelineTests
    {
        [Test]
        public unsafe void ReliableUtility_ValidationScenarios()
        {
            // Receive a Packet Newer still gapped. [0, 1, Lost, 3, 4]
            // Massage the resend flow using the Received Mask. [0, 1, Resend, 3, 4]
            // Receive the missing packet '2' and massage the receive flow

            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 32
            };

            var processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            var sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);


            // ep1
            var ep1SharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);
            var ep1SendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            var ep1RecvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);

            // ep2
            var ep2SharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);
            var ep2SendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            var ep2RecvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);

            // packet
            var packet = new NativeArray<byte>(UnsafeUtility.SizeOf<ReliableUtility.Packet>(), Allocator.Persistent);
            packet[0] = 100;

            var header = new DataStreamWriter(UnsafeUtility.SizeOf<ReliableUtility.PacketHeader>(), Allocator.Temp);

            ReliableSequencedPipelineStage ep1Owner = new ReliableSequencedPipelineStage();
            ReliableSequencedPipelineStage ep2Owner = new ReliableSequencedPipelineStage();

            var ep1Buffer = new NativeArray<byte>(ep1Owner.StaticSize, Allocator.Persistent);
            var ep2Buffer = new NativeArray<byte>(ep2Owner.StaticSize, Allocator.Persistent);

            var paramList = new INetworkParameter[]{parameters};
            var ep1 = ep1Owner.StaticInitialize((byte*)ep1Buffer.GetUnsafePtr(), ep1Buffer.Length, paramList);
            var ep2 = ep1Owner.StaticInitialize((byte*)ep2Buffer.GetUnsafePtr(), ep2Buffer.Length, paramList);
            ep1.InitializeConnection.Ptr.Invoke((byte*)ep1Buffer.GetUnsafePtr(), ep1Buffer.Length,
                (byte*)ep1SendBuffer.GetUnsafePtr(), ep1SendBuffer.Length, (byte*)ep1RecvBuffer.GetUnsafePtr(), ep1RecvBuffer.Length,
                (byte*)ep1SharedBuffer.GetUnsafePtr(), ep1SharedBuffer.Length);
            ep2.InitializeConnection.Ptr.Invoke((byte*)ep2Buffer.GetUnsafePtr(), ep2Buffer.Length,
                (byte*)ep2SendBuffer.GetUnsafePtr(), ep2SendBuffer.Length, (byte*)ep2RecvBuffer.GetUnsafePtr(), ep2RecvBuffer.Length,
                (byte*)ep2SharedBuffer.GetUnsafePtr(), ep2SharedBuffer.Length);

            var ep1sendContext = (ReliableUtility.Context*) ep1SendBuffer.GetUnsafePtr();
            //var ep1recvContext = (ReliableUtility.Context*) ep1RecvBuffer.GetUnsafePtr();
            //var ep1sharedContext = (ReliableUtility.SharedContext*) ep1SharedBuffer.GetUnsafePtr();

            var ep2recvContext = (ReliableUtility.Context*) ep2RecvBuffer.GetUnsafePtr();
            //var ep2sendContext = (ReliableUtility.Context*) ep2SendBuffer.GetUnsafePtr();
            //var ep2sharedContext = (ReliableUtility.SharedContext*) ep2SharedBuffer.GetUnsafePtr();

            // Send a Packet - Receive a Packet
            var currentId = 0;

            var inboundSend = default(InboundSendBuffer);
            inboundSend.buffer = (byte*)packet.GetUnsafePtr();
            inboundSend.bufferLength = packet.Length;
            inboundSend.bufferWithHeaders = (byte*)packet.GetUnsafePtr();
            inboundSend.bufferWithHeadersLength = packet.Length;

            NetworkPipelineStage.Requests stageRequest = NetworkPipelineStage.Requests.None;
            var slice = default(InboundRecvBuffer);
            var output = default(InboundSendBuffer);
            {
                var ctx = new NetworkPipelineContext
                {
                    header = header,
                    internalProcessBuffer = (byte*)ep1SendBuffer.GetUnsafePtr(),
                    internalProcessBufferLength = ep1SendBuffer.Length,
                    internalSharedProcessBuffer = (byte*)ep1SharedBuffer.GetUnsafePtr(),
                    internalSharedProcessBufferLength = ep1SharedBuffer.Length,
                    staticInstanceBuffer = (byte*)ep1Buffer.GetUnsafePtr(),
                    staticInstanceBufferLength = ep1Buffer.Length
                };
                output = inboundSend;
                ep1.Send.Ptr.Invoke(ref ctx, ref output, ref stageRequest);
                Assert.True(output.buffer[0] == packet[0]);
                Assert.True((stageRequest&NetworkPipelineStage.Requests.Resume)==0);
            }
            {
                var info = ReliableUtility.GetPacketInformation((byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr(), currentId);
                var offset = ep1sendContext->DataPtrOffset; // + (index * ctx->DataStride);
                InboundRecvBuffer data;
                data.buffer = (byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr() + offset;
                data.bufferLength = info->Size;

                var ctx = new NetworkPipelineContext
                {
                    internalProcessBuffer = (byte*)ep2RecvBuffer.GetUnsafePtr(),
                    internalProcessBufferLength = ep2RecvBuffer.Length,
                    internalSharedProcessBuffer = (byte*)ep2SharedBuffer.GetUnsafePtr(),
                    internalSharedProcessBufferLength = ep2SharedBuffer.Length,
                    staticInstanceBuffer = (byte*)ep2Buffer.GetUnsafePtr(),
                    staticInstanceBufferLength = ep2Buffer.Length
                };
                slice = data;
                ep2.Receive.Ptr.Invoke(ref ctx, ref slice, ref stageRequest);

                if (slice.bufferLength > 0)
                    Assert.True(slice.buffer[0] == packet[0]);
            }
            Assert.True((stageRequest&NetworkPipelineStage.Requests.Resume)==0);
            Assert.True(ep2recvContext->Delivered == currentId);

            // Scenario: Receive a Packet Newer then expected [0, 1, Lost, 3]

            // Start by "sending" 1, 2, 3;
            for (int seq = currentId + 1; seq < 4; seq++)
            {
                packet[0] = (byte) (100 + seq);

                header.Clear();
                var ctx = new NetworkPipelineContext
                {
                    header = header,
                    internalProcessBuffer = (byte*)ep1SendBuffer.GetUnsafePtr(),
                    internalProcessBufferLength = ep1SendBuffer.Length,
                    internalSharedProcessBuffer = (byte*)ep1SharedBuffer.GetUnsafePtr(),
                    internalSharedProcessBufferLength = ep1SharedBuffer.Length,
                    staticInstanceBuffer = (byte*)ep1Buffer.GetUnsafePtr(),
                    staticInstanceBufferLength = ep1Buffer.Length
                };
                output = inboundSend;
                ep1.Send.Ptr.Invoke(ref ctx, ref output, ref stageRequest);

                Assert.True((stageRequest&NetworkPipelineStage.Requests.Resume)==0);
                Assert.True(output.buffer[0] == packet[0]);
            }

            for (int seq = currentId + 1; seq < 4; seq++)
            {
                if (seq == 2)
                    continue;

                var info = ReliableUtility.GetPacketInformation((byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr(), seq);
                var offset = ep1sendContext->DataPtrOffset + ((seq % ep1sendContext->Capacity) * ep1sendContext->DataStride);
                var inspectPacket = ReliableUtility.GetPacket((byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr(), seq);

                InboundRecvBuffer data;
                data.buffer = (byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr() + offset;
                data.bufferLength = info->Size;
                Assert.True(inspectPacket->Header.SequenceId == info->SequenceId);

                header.Clear();
                var ctx = new NetworkPipelineContext
                {
                    header = header,
                    internalProcessBuffer = (byte*)ep2RecvBuffer.GetUnsafePtr(),
                    internalProcessBufferLength = ep2RecvBuffer.Length,
                    internalSharedProcessBuffer = (byte*)ep2SharedBuffer.GetUnsafePtr(),
                    internalSharedProcessBufferLength = ep2SharedBuffer.Length,
                    staticInstanceBuffer = (byte*)ep2Buffer.GetUnsafePtr(),
                    staticInstanceBufferLength = ep2Buffer.Length
                };
                stageRequest = NetworkPipelineStage.Requests.None;
                slice = data;
                ep2.Receive.Ptr.Invoke(ref ctx, ref slice, ref stageRequest);

                if (slice.bufferLength > 0)
                {
                    Assert.True(slice.buffer[0] == seq + 100);
                }
            }

            // Receive packet number 2 and resume received packets.
            bool first = true;
            do
            {
                var data = default(InboundRecvBuffer);
                if (first)
                {
                    var seq = 2;
                    var info = ReliableUtility.GetPacketInformation((byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr(), seq);
                    var offset = ep1sendContext->DataPtrOffset +
                                 ((seq % ep1sendContext->Capacity) * ep1sendContext->DataStride);
                    var inspectPacket = ReliableUtility.GetPacket((byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr(), seq);

                    data.buffer = (byte*)ep1SendBuffer.GetUnsafeReadOnlyPtr() + offset;
                    data.bufferLength = info->Size;
                    Assert.True(inspectPacket->Header.SequenceId == info->SequenceId);

                    first = false;
                }

                var ctx = new NetworkPipelineContext
                {
                    internalProcessBuffer = (byte*)ep2RecvBuffer.GetUnsafeReadOnlyPtr(),
                    internalProcessBufferLength = ep2RecvBuffer.Length,
                    internalSharedProcessBuffer = (byte*)ep2SharedBuffer.GetUnsafeReadOnlyPtr(),
                    internalSharedProcessBufferLength = ep2SharedBuffer.Length
                };
                slice = data;
                ep2.Receive.Ptr.Invoke(ref ctx, ref slice, ref stageRequest);

                if (slice.bufferLength > 0)
                {
                    Assert.True(slice.buffer[0] == ep2recvContext->Delivered + 100);
                }
            } while ((stageRequest&NetworkPipelineStage.Requests.Resume)!=0);


            packet.Dispose();
            ep1SharedBuffer.Dispose();
            ep1SendBuffer.Dispose();
            ep1RecvBuffer.Dispose();
            ep2SharedBuffer.Dispose();
            ep2SendBuffer.Dispose();
            ep2RecvBuffer.Dispose();
            ep1Buffer.Dispose();
            ep2Buffer.Dispose();
        }


        [Test]
        public unsafe void ReliableUtility_Validation()
        {
            int capacity = 5;
            NativeArray<byte> buffer = new NativeArray<byte>(1, Allocator.Persistent);
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = capacity
            };

            int result = ReliableUtility.ProcessCapacityNeeded(parameters);
            NativeArray<byte> processBuffer = new NativeArray<byte>(result, Allocator.Persistent);

            var processBufferPtr = (byte*)processBuffer.GetUnsafePtr();

            ReliableUtility.InitializeProcessContext(processBufferPtr, processBuffer.Length, parameters);

            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 0));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 1));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 2));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 3));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 4));
            Assert.IsFalse(ReliableUtility.TryAquire(processBufferPtr, 5));

            ReliableUtility.Release(processBufferPtr, 0, 5);

            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 0));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 1));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 2));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 3));
            Assert.IsTrue(ReliableUtility.TryAquire(processBufferPtr, 4));

            buffer[0] = (byte)(1);

            ReliableUtility.SetPacket(processBufferPtr, 0, (byte*)buffer.GetUnsafeReadOnlyPtr(), buffer.Length);


            var slice = ReliableUtility.GetPacket(processBufferPtr, 0);
            Assert.IsTrue(slice->Buffer[0] == buffer[0]);

            for (int i = 0; i < capacity * 5; i++)
            {
                ReliableUtility.SetPacket(processBufferPtr, i, (byte*)buffer.GetUnsafeReadOnlyPtr(), buffer.Length);
                slice = ReliableUtility.GetPacket(processBufferPtr, i);
                Assert.IsTrue(slice->Buffer[0] == buffer[0]);
            }
            ReliableUtility.Release(processBufferPtr, 0, 5);

            processBuffer.Dispose();
            buffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_SeqIdBeginAt0()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length, timestamp = 1000};

            // Sending seqId 3, last received ID 0 (1 is not yet acked, 2 was dropped)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 0;    // Last sent is initialized to what you are sending next
            sharedContext->SentPackets.Acked = -1;
            sharedContext->SentPackets.AckMask = 0x1;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 65535, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 65535)->SendTime = 980;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 65535, 980);
                ReliableUtility.StoreReceiveTimestamp(pipelineContext.internalSharedProcessBuffer, 65535, 990, 16);
                stream.Clear();
                stream.WriteInt((int) 11);
                ReliableUtility.SetPacket(sendBufferPtr, 0, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 0)->SendTime = 990;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 0, 990);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, 65535 should be released, 0 should still be there
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 65535)->SequenceId);
                Assert.AreEqual(0, ReliableUtility.GetPacketInformation(sendBufferPtr, 0)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_SeqIdWrap1()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length, timestamp = 1000};

            // Sending seqId 3, last received ID 2 (same as last sent packet)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 3;
            sharedContext->SentPackets.Acked = 2;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 1, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 1)->SendTime = 980;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 1, 980);
                ReliableUtility.StoreReceiveTimestamp(pipelineContext.internalSharedProcessBuffer, 1, 990, 16);
                stream.Clear();
                stream.WriteInt((int) 11);
                ReliableUtility.SetPacket(sendBufferPtr, 2, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 2)->SendTime = 990;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 2, 990);
                ReliableUtility.StoreReceiveTimestamp(pipelineContext.internalSharedProcessBuffer, 2, 1000, 16);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, both packets should be released
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 1)->SequenceId);
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 2)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_SeqIdWrap2()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            // Sending seqId 3, last received ID 65535 (same as last sent)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 0;
            sharedContext->SentPackets.Acked = 65535;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 65535, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 65535)->SendTime = 980;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 65535, 980);
                ReliableUtility.StoreReceiveTimestamp(pipelineContext.internalSharedProcessBuffer, 65535, 990, 16);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, 65535 should be released
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 65535)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_SeqIdWrap3()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            // Sending seqId 3, last received ID 0 (1 is not yet acked, 2 was dropped)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 17;
            sharedContext->SentPackets.Acked = 16;
            sharedContext->SentPackets.AckMask = 0xFFFFDBB7;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 16, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, packet 16 should be released
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 16)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_ReleaseSlotWithWrappedSeqId()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            // Sending seqId 3, last received ID 0 (1 is not yet acked, 2 was dropped)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 1;
            sharedContext->SentPackets.Acked = 0;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 0, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 11);
                ReliableUtility.SetPacket(sendBufferPtr, 65535, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, slot with seqId 0 and 65535 should have been released
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 0)->SequenceId);
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 65535)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_AckMaskShiftsProperly1()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length, timestamp = 1000};

            // Sending seqId 3, last received ID 0 (1 is not yet acked, 2 was dropped)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 4;
            sharedContext->SentPackets.Acked = 3;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFD;    // bit 0 = seqId 3 (1), bit 1 = seqId 2 (0)
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                // SeqId 3 is received and ready to be released
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 3, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 3)->SendTime = 990;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 3, 980);
                ReliableUtility.StoreReceiveTimestamp(pipelineContext.internalSharedProcessBuffer, 3, 990, 16);
                stream.Clear();
                // SeqId 2 is not yet received so it should stick around
                stream.WriteInt((int) 11);
                ReliableUtility.SetPacket(sendBufferPtr, 2, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 2)->SendTime = 1000;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 2, 1000);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, packet 3 should be released (has been acked), 2 should stick around
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 3)->SequenceId);
                Assert.AreEqual(2, ReliableUtility.GetPacketInformation(sendBufferPtr, 2)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_AckPackets_AckMaskShiftsProperly2()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length, timestamp = 1000};

            // Sending seqId 3, last received ID 0 (1 is not yet acked, 2 was dropped)
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 5;
            sharedContext->SentPackets.Acked = 4;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFD;    // bit 0 = seqId 4 (1), bit 1 = seqId 3 (0)
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers in resend queue
                // SeqId 4 is received and ready to be released
                stream.WriteInt((int) 10);
                ReliableUtility.SetPacket(sendBufferPtr, 4, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 4)->SendTime = 980;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 4, 980);
                ReliableUtility.StoreReceiveTimestamp(pipelineContext.internalSharedProcessBuffer, 4, 990, 16);
                stream.Clear();
                stream.WriteInt((int) 11);
                ReliableUtility.SetPacket(sendBufferPtr, 3, (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                ReliableUtility.GetPacketInformation(sendBufferPtr, 3)->SendTime = 1000;
                ReliableUtility.StoreTimestamp(pipelineContext.internalSharedProcessBuffer, 3, 1000);

                ReliableUtility.ReleaseOrResumePackets(pipelineContext);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);

                // Validate that packet tracking state is correct, packet 3 should be released (has been acked), 2 should stick around
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 4)->SequenceId);
                Assert.AreEqual(3, ReliableUtility.GetPacketInformation(sendBufferPtr, 3)->SequenceId);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void ReliableUtility_TimestampHandling()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 3
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> ep1RecvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> ep1SendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> ep1SharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);
            NativeArray<byte> ep2RecvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> ep2SendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> ep2SharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            ReliableUtility.InitializeContext((byte*)ep1SharedBuffer.GetUnsafePtr(), ep1SharedBuffer.Length,
                (byte*)ep1SendBuffer.GetUnsafePtr(), ep1SendBuffer.Length,
                (byte*)ep1RecvBuffer.GetUnsafePtr(), ep1RecvBuffer.Length, parameters);
            ReliableUtility.InitializeContext((byte*)ep2SharedBuffer.GetUnsafePtr(), ep2SharedBuffer.Length,
                (byte*)ep2SendBuffer.GetUnsafePtr(), ep2SendBuffer.Length,
                (byte*)ep2RecvBuffer.GetUnsafePtr(), ep2RecvBuffer.Length, parameters);

            // When sending we store the send timestamp of the sequence ID (EP1 -> EP2)
            ushort ep1SeqId = 10;
            ReliableUtility.StoreTimestamp((byte*)ep1SharedBuffer.GetUnsafePtr(), ep1SeqId, 900);

            // EP2 also sends something to EP1
            ushort ep2SeqId = 100;
            ReliableUtility.StoreTimestamp((byte*)ep2SharedBuffer.GetUnsafePtr(), ep2SeqId, 910);

            // When EP2 receives the packet the receive time is stored
            ReliableUtility.StoreRemoteReceiveTimestamp((byte*)ep2SharedBuffer.GetUnsafePtr(), ep1SeqId, 920);

            // EP2 also stores the timing information in the EP1 packet (processing time for the packet it sent earlier)
            ReliableUtility.StoreReceiveTimestamp((byte*)ep2SharedBuffer.GetUnsafePtr(), ep2SeqId, 920, 10);

            // When EP2 sends another packet to EP1 it calculates ep1SeqId processing time
            int processTime = ReliableUtility.CalculateProcessingTime((byte*)ep2SharedBuffer.GetUnsafePtr(), ep1SeqId, 930);

            // ep1SeqId processing time should be 10 ms (930 - 920)
            Assert.AreEqual(10, processTime);

            // Verify information written so far (send/receive times + processing time)
            var timerData = ReliableUtility.GetLocalPacketTimer((byte*)ep2SharedBuffer.GetUnsafePtr(), ep2SeqId);
            Assert.IsTrue(timerData != null, "Packet timing data not found");
            Assert.AreEqual(ep2SeqId, timerData->SequenceId);
            Assert.AreEqual(10, timerData->ProcessingTime);
            Assert.AreEqual(910, timerData->SentTime);
            Assert.AreEqual(920, timerData->ReceiveTime);

            var ep2SharedCtx = (ReliableUtility.SharedContext*) ep2SharedBuffer.GetUnsafePtr();
            Debug.Log("LastRtt=" + ep2SharedCtx->RttInfo.LastRtt);
            Debug.Log("SmoothedRTT=" + ep2SharedCtx->RttInfo.SmoothedRtt);
            Debug.Log("ResendTimeout=" + ep2SharedCtx->RttInfo.ResendTimeout);
            Debug.Log("SmoothedVariance=" + ep2SharedCtx->RttInfo.SmoothedVariance);

            ep1RecvBuffer.Dispose();
            ep1SendBuffer.Dispose();
            ep1SharedBuffer.Dispose();
            ep2RecvBuffer.Dispose();
            ep2SendBuffer.Dispose();
            ep2SharedBuffer.Dispose();
        }

        [Test]
        public unsafe void Receive_ResumesMultipleStoredPacketsAroundWrapPoint1()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var recvBufferPtr = (byte*)recvBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                (byte*)sendBuffer.GetUnsafePtr(), sendBuffer.Length, recvBufferPtr, recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = recvBufferPtr, internalProcessBufferLength = recvBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 3; // what was last sent doesn't matter here
            sharedContext->SentPackets.Acked = 2;
            sharedContext->SentPackets.AckMask = 0xFFFFFFF7;    // bit 0,1,2 maps to seqId 2,1,0 all delivered, bit 3 is seqId 65535 which is not yet delivered
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = 65534;    // latest in sequence delivered packet, one less than what unclogs the packet jam

            var reliablePipelineStage = new ReliableSequencedPipelineStage();
            var staticBuffer = new NativeArray<byte>(reliablePipelineStage.StaticSize, Allocator.Temp);
            pipelineContext.staticInstanceBuffer = (byte*)staticBuffer.GetUnsafePtr();
            pipelineContext.staticInstanceBufferLength = staticBuffer.Length;
            var reliablePipeline = reliablePipelineStage.StaticInitialize((byte*)staticBuffer.GetUnsafePtr(), staticBuffer.Length, new INetworkParameter[0]);

            var stream = new DataStreamWriter(4, Allocator.Temp);
            var inboundStream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers to receive queue, packets which should be resume received after packet jam is unclogged
                stream.Clear();
                stream.WriteInt((int) 100);
                ReliableUtility.SetPacket(recvBufferPtr, 0, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 200);
                ReliableUtility.SetPacket(recvBufferPtr, 1, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 300);
                ReliableUtility.SetPacket(recvBufferPtr, 2, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                // Generate the packet which will be handled in receive
                InboundRecvBuffer packet = default;
                GeneratePacket(9000, 2, 0xFFFFFFFF, 65535, ref sendBuffer, out packet);

                // Process 65535, 0 should then be next in line on the resume field
                var stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(0, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                // Process 0, after that 1 is up
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(1, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                // Process 1, after that 2 is up
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(2, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                // Process 2, and we are done
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(-1, receiveContext->Resume);
                Assert.AreEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void Receive_ResumesMultipleStoredPacketsAroundWrapPoint2()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var recvBufferPtr = (byte*)recvBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
            (byte*)sendBuffer.GetUnsafePtr(), sendBuffer.Length, recvBufferPtr, recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = recvBufferPtr, internalProcessBufferLength = recvBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 2; // what was last sent doesn't matter here
            sharedContext->SentPackets.Acked = 1;
            sharedContext->SentPackets.AckMask = 0xFFFFFFF7;    // bit 0,1,2 maps to seqId 1,0,65535 all delivered, bit 3 is seqId 65534 which is not yet delivered
            sharedContext->ReceivedPackets.Sequence = 1;
            sharedContext->ReceivedPackets.AckMask = 0xFFFFFFF7;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = 65533;    // latest in sequence delivered packet, one less than what unclogs the packet jam

            var reliablePipelineStage = new ReliableSequencedPipelineStage();
            var staticBuffer = new NativeArray<byte>(reliablePipelineStage.StaticSize, Allocator.Temp);
            pipelineContext.staticInstanceBuffer = (byte*)staticBuffer.GetUnsafePtr();
            pipelineContext.staticInstanceBufferLength = staticBuffer.Length;
            var reliablePipeline = reliablePipelineStage.StaticInitialize((byte*)staticBuffer.GetUnsafePtr(), staticBuffer.Length, new INetworkParameter[0]);

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers to receive queue, packets which should be resume received after packet jam is unclogged
                stream.Clear();
                stream.WriteInt((int) 100);
                ReliableUtility.SetPacket(recvBufferPtr, 65535, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 200);
                ReliableUtility.SetPacket(recvBufferPtr, 0, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 300);
                ReliableUtility.SetPacket(recvBufferPtr, 1, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                // Generate the packet which will be handled in receive
                InboundRecvBuffer packet = default;
                GeneratePacket(9000, 65533, 0xFFFFFFFF, 65534, ref sendBuffer, out packet);

                // Process 65534, 65535 should then be next in line on the resume field
                var stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(65535, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                // Process 65535, after that 0 is up
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(0, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                // Process 0, after that 1 is up
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(1, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                // Process 1, and we are done
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(-1, receiveContext->Resume);
                Assert.AreEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void Receive_ResumesMultipleStoredPacketsAndSetsAckedAckMaskProperly()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 10
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var recvBufferPtr = (byte*)recvBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
            (byte*)sendBuffer.GetUnsafePtr(), sendBuffer.Length, recvBufferPtr, recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = recvBufferPtr, internalProcessBufferLength = recvBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 99;           // what was last sent doesn't matter here
            sharedContext->SentPackets.Acked = 97;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = 98;
            sharedContext->ReceivedPackets.AckMask = 0xFFFFFFF7;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = 94;    // latest in sequence delivered packet, one less than what unclogs the packet jam

            var reliablePipelineStage = new ReliableSequencedPipelineStage();
            var staticBuffer = new NativeArray<byte>(reliablePipelineStage.StaticSize, Allocator.Temp);
            pipelineContext.staticInstanceBuffer = (byte*)staticBuffer.GetUnsafePtr();
            pipelineContext.staticInstanceBufferLength = staticBuffer.Length;
            var reliablePipeline = reliablePipelineStage.StaticInitialize((byte*)staticBuffer.GetUnsafePtr(), staticBuffer.Length, new INetworkParameter[0]);

            var stream = new DataStreamWriter(4, Allocator.Temp);
            {
                // Add buffers to receive queue, packets which should be resume received after packet jam is unclogged
                stream.Clear();
                stream.WriteInt((int) 200);
                ReliableUtility.SetPacket(recvBufferPtr, 96, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 300);
                ReliableUtility.SetPacket(recvBufferPtr, 97, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 300);
                ReliableUtility.SetPacket(recvBufferPtr, 98, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                InboundRecvBuffer packet = default;
                GeneratePacket(9000, 98, 0xFFFFFFFF, 99, ref sendBuffer, out packet);

                // Receive 99, it's out of order so should be queued for later (waiting for 95)
                var stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(-1, receiveContext->Resume);
                Assert.AreEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);

                GeneratePacket(10000, 98, 0xFFFFFFFF, 95, ref sendBuffer, out packet);

                // First 95 is received and then receive resume runs up to 99
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(96, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(97, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(98, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(99, receiveContext->Resume);
                Assert.AreNotEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);
                stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Receive.Ptr.Invoke(ref pipelineContext, ref packet, ref stageRequest);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, sharedContext->errorCode);
                Assert.AreEqual(-1, receiveContext->Resume);
                Assert.AreEqual(NetworkPipelineStage.Requests.None, stageRequest&NetworkPipelineStage.Requests.Resume);

                // Verify that the ReceivePackets state is correct, 99 should be latest received and ackmask 0xFFFFF
                Assert.AreEqual(99, sharedContext->ReceivedPackets.Sequence);
                Assert.AreEqual(0xFFFFFFFF, sharedContext->ReceivedPackets.AckMask);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void Send_PacketsAreAcked_SendingPacket()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 3
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            var sendBufferPtr = (byte*)sendBuffer.GetUnsafePtr();

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                sendBufferPtr, sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = sendBufferPtr, internalProcessBufferLength = recvBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length};

            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 3;
            sharedContext->SentPackets.Acked = 2;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = 2;
            sharedContext->ReceivedPackets.AckMask = 0xFFFFFFFF;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = 1;

            var reliablePipelineStage = new ReliableSequencedPipelineStage();
            var staticBuffer = new NativeArray<byte>(reliablePipelineStage.StaticSize, Allocator.Temp);
            pipelineContext.staticInstanceBuffer = (byte*)staticBuffer.GetUnsafePtr();
            pipelineContext.staticInstanceBufferLength = staticBuffer.Length;
            var reliablePipeline = reliablePipelineStage.StaticInitialize((byte*)staticBuffer.GetUnsafePtr(), staticBuffer.Length, new INetworkParameter[0]);

            var stream = new DataStreamWriter(4, Allocator.Temp);
            pipelineContext.header = new DataStreamWriter(UnsafeUtility.SizeOf<ReliableUtility.PacketHeader>(), Allocator.Temp);
            {
                // Fill window capacity, next send should then clear everything
                stream.Clear();
                stream.WriteInt((int) 100);
                ReliableUtility.SetPacket(sendBufferPtr, 0, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 200);
                ReliableUtility.SetPacket(sendBufferPtr, 1, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 300);
                ReliableUtility.SetPacket(sendBufferPtr, 2, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                // Set input buffer and send, this will be seqId 3
                stream.Clear();
                stream.WriteInt((int) 9000);
                var inboundBuffer = new InboundSendBuffer();
                inboundBuffer.bufferWithHeaders = (byte*)stream.AsNativeArray().GetUnsafeReadOnlyPtr();
                inboundBuffer.bufferWithHeadersLength = stream.Length;
                inboundBuffer.SetBufferFrombufferWithHeaders();

                var stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Send.Ptr.Invoke(ref pipelineContext, ref inboundBuffer, ref stageRequest);

                // seqId 3 should now be stored in slot 0
                Assert.AreEqual(3, ReliableUtility.GetPacketInformation(sendBufferPtr, 3)->SequenceId);

                // slots 1 and 2 should be cleared
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 1)->SequenceId);
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation(sendBufferPtr, 2)->SequenceId);

                Assert.AreEqual(NetworkPipelineStage.Requests.Update, stageRequest);

                // Verify ack packet is written correctly
                ReliableUtility.PacketHeader header = default;
                ReliableUtility.WriteAckPacket(pipelineContext, ref header);
                Assert.AreEqual(header.AckedSequenceId, 2);
                Assert.AreEqual(header.AckMask, 0xFFFFFFFF);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        [Test]
        public unsafe void Send_PacketsAreAcked_UpdateAckState()
        {
            ReliableUtility.Parameters parameters = new ReliableUtility.Parameters
            {
                WindowSize = 3
            };

            int processCapacity = ReliableUtility.ProcessCapacityNeeded(parameters);
            int sharedCapacity = ReliableUtility.SharedCapacityNeeded(parameters);
            NativeArray<byte> recvBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sendBuffer = new NativeArray<byte>(processCapacity, Allocator.Persistent);
            NativeArray<byte> sharedBuffer = new NativeArray<byte>(sharedCapacity, Allocator.Persistent);

            ReliableUtility.InitializeContext((byte*)sharedBuffer.GetUnsafePtr(), sharedBuffer.Length,
                (byte*)sendBuffer.GetUnsafePtr(), sendBuffer.Length, (byte*)recvBuffer.GetUnsafePtr(), recvBuffer.Length, parameters);

            var pipelineContext = new NetworkPipelineContext
                {internalProcessBuffer = (byte*)sendBuffer.GetUnsafePtr(), internalProcessBufferLength = sendBuffer.Length,
                internalSharedProcessBuffer = (byte*)sharedBuffer.GetUnsafePtr(), internalSharedProcessBufferLength = sharedBuffer.Length, timestamp = 1000};

            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = 3;
            sharedContext->SentPackets.Acked = 2;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = 2;
            sharedContext->ReceivedPackets.AckMask = 0xFFFFFFFF;
            var receiveContext = (ReliableUtility.Context*) recvBuffer.GetUnsafePtr();
            receiveContext->Delivered = 1;

            // Set last send time to something a long time ago so the ack state is sent in Send
            var sendContext = (ReliableUtility.Context*) sendBuffer.GetUnsafePtr();
            sendContext->LastSentTime = 500;
            sendContext->PreviousTimestamp = 980;    // 20 ms ago

            var reliablePipelineStage = new ReliableSequencedPipelineStage();
            var staticBuffer = new NativeArray<byte>(reliablePipelineStage.StaticSize, Allocator.Temp);
            pipelineContext.staticInstanceBuffer = (byte*)staticBuffer.GetUnsafeReadOnlyPtr();
            pipelineContext.staticInstanceBufferLength = staticBuffer.Length;
            var reliablePipeline = reliablePipelineStage.StaticInitialize((byte*)staticBuffer.GetUnsafeReadOnlyPtr(), staticBuffer.Length, new INetworkParameter[0]);

            var stream = new DataStreamWriter(4, Allocator.Temp);
            pipelineContext.header = new DataStreamWriter(UnsafeUtility.SizeOf<ReliableUtility.PacketHeader>(), Allocator.Temp);
            {
                // Fill window capacity, next send should then clear everything
                stream.Clear();
                stream.WriteInt((int) 100);
                ReliableUtility.SetPacket((byte*)sendBuffer.GetUnsafePtr(), 0, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 200);
                ReliableUtility.SetPacket((byte*)sendBuffer.GetUnsafePtr(), 1, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);
                stream.Clear();
                stream.WriteInt((int) 300);
                ReliableUtility.SetPacket((byte*)sendBuffer.GetUnsafePtr(), 2, stream.AsNativeArray().GetUnsafeReadOnlyPtr(), stream.Length);

                var inboundBuffer = new InboundSendBuffer();

                var stageRequest = NetworkPipelineStage.Requests.None;
                reliablePipeline.Send.Ptr.Invoke(ref pipelineContext, ref inboundBuffer, ref stageRequest);

                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation((byte*)sendBuffer.GetUnsafeReadOnlyPtr(), 0)->SequenceId);
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation((byte*)sendBuffer.GetUnsafeReadOnlyPtr(), 1)->SequenceId);
                Assert.AreEqual(-1, ReliableUtility.GetPacketInformation((byte*)sendBuffer.GetUnsafeReadOnlyPtr(), 2)->SequenceId);

                Assert.AreEqual(NetworkPipelineStage.Requests.Update, stageRequest);
            }
            recvBuffer.Dispose();
            sendBuffer.Dispose();
            sharedBuffer.Dispose();
        }

        unsafe void GeneratePacket(int payload, ushort headerAckedId, uint headerAckMask, ushort headerSeqId, ref NativeArray<byte> sendBuffer, out InboundRecvBuffer packet)
        {
            DataStreamWriter inboundStream = new DataStreamWriter(4, Allocator.Temp);

            inboundStream.WriteInt((int) payload);
            InboundSendBuffer data = default;
            data.bufferWithHeaders = (byte*)inboundStream.AsNativeArray().GetUnsafePtr();
            data.bufferWithHeadersLength = inboundStream.Length;
            data.SetBufferFrombufferWithHeaders();
            ReliableUtility.PacketHeader header = new ReliableUtility.PacketHeader()
            {
                AckedSequenceId = headerAckedId,
                AckMask = headerAckMask,
                SequenceId = headerSeqId
            };
            ReliableUtility.SetHeaderAndPacket((byte*)sendBuffer.GetUnsafePtr(), headerSeqId, header, data, 1000);

            // Extract raw packet from the send buffer so it can be passed directly to receive
            var sendCtx = (ReliableUtility.Context*) sendBuffer.GetUnsafePtr();
            var index = headerSeqId % sendCtx->Capacity;
            var offset = sendCtx->DataPtrOffset + (index * sendCtx->DataStride);
            packet.buffer = (byte*)sendBuffer.GetUnsafeReadOnlyPtr() + offset;
            packet.bufferLength = sendCtx->DataStride;
        }
    }

    public class QoSNetworkPipelineTest
    {
        private NetworkDriver m_ServerDriver;
        private NetworkDriver m_ClientDriver;
        private NetworkPipelineStageId m_ReliableStageId;
        private NetworkPipelineStageId m_SimulatorStageId;

        [SetUp]
        public void IPC_Setup()
        {
            TempDisconnectPipelineStageCollection.Register();
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = 90 * 1000,
                fixedFrameTimeMS = 16
            };
            m_ServerDriver =
                TestNetworkDriver.Create(new NetworkDataStreamParameter
                    {size = 0}, timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32});
            m_ServerDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            m_ServerDriver.Listen();
            m_ClientDriver =
                TestNetworkDriver.Create(new NetworkDataStreamParameter
                    {size = 0}, timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32},
                    new SimulatorUtility.Parameters { MaxPacketCount = 30, MaxPacketSize = 16, PacketDelayMs = 0, /*PacketDropInterval = 8,*/ PacketDropPercentage = 10});
            m_ReliableStageId = NetworkPipelineStageCollection.GetStageId(typeof(ReliableSequencedPipelineStage));
            m_SimulatorStageId = NetworkPipelineStageCollection.GetStageId(typeof(SimulatorPipelineStage));
        }

        [TearDown]
        public void IPC_TearDown()
        {
            m_ClientDriver.Dispose();
            m_ServerDriver.Dispose();
        }

        [Test]
        public void NetworkPipeline_ReliableSequenced_SendRecvOnce()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe, serverPipe);

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            // Send message to client
            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
            {
                strm.WriteInt((int) 42);
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(4, readStrm.Length);
            Assert.AreEqual(42, readStrm.ReadInt());
        }

        [Test]
        public unsafe void NetworkPipeline_ReliableSequenced_SendRecvWithRTTCalculation()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            m_ClientDriver.ScheduleUpdate().Complete();
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();

            m_ServerDriver.GetPipelineBuffers(serverPipe, m_ReliableStageId, serverToClient, out var serverReceiveBuffer, out var serverSendBuffer, out var serverSharedBuffer);
            var sharedContext = (ReliableUtility.SharedContext*) serverSharedBuffer.GetUnsafePtr();

            m_ClientDriver.GetPipelineBuffers(clientPipe, m_ReliableStageId, clientToServer, out var clientReceiveBuffer, out var clientSendBuffer, out var clientSharedBuffer);

            // First the server sends a packet to the client
            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
            {
                strm.WriteInt((int) 42);
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();

            // Server sent time for the packet with seqId=0 is set
            m_ServerDriver.GetPipelineBuffers(serverPipe, m_ReliableStageId, serverToClient, out serverReceiveBuffer, out serverSendBuffer, out serverSharedBuffer);
            var serverPacketTimer = ReliableUtility.GetLocalPacketTimer((byte*)serverSharedBuffer.GetUnsafeReadOnlyPtr(), 0);
            Assert.IsTrue(serverPacketTimer->SentTime > 0);

            m_ClientDriver.ScheduleUpdate().Complete();

            // Client received seqId=0 from server and sets the receive time
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_ReliableStageId, clientToServer, out clientReceiveBuffer, out clientSendBuffer, out clientSharedBuffer);
            var clientPacketTimer = ReliableUtility.GetRemotePacketTimer((byte*)clientSharedBuffer.GetUnsafeReadOnlyPtr(), 0);
            Assert.IsTrue(clientPacketTimer->ReceiveTime >= serverPacketTimer->SentTime);

            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            // Now update client, if it's updated in the while loop it will automatically send ack packets to the server
            // so processing time will actually be recorded as almost 0
            m_ClientDriver.ScheduleUpdate().Complete();

            // Now client sends packet to the server, this should contain the ackedSeqId=0 for the servers initial packet
            if (m_ClientDriver.BeginSend(clientPipe, clientToServer, out strm) == 0)
            {
                strm.WriteInt((int) 9000);
                m_ClientDriver.EndSend(strm);
            }
            m_ClientDriver.ScheduleUpdate().Complete();

            // Receive time for the server packet is 0 at this point
            Assert.AreEqual(serverPacketTimer->ReceiveTime, 0);

            // Packet is now processed, receive+processing time recorded
            m_ServerDriver.ScheduleUpdate().Complete();

            // Server has now received a packet from the client with ackedSeqId=0 in the header and timing info for that
            Assert.GreaterOrEqual(serverPacketTimer->ReceiveTime, clientPacketTimer->ReceiveTime);
            Assert.GreaterOrEqual(serverPacketTimer->ProcessingTime, 16);
        }

        [Test]
        public void NetworkPipeline_ReliableSequenced_SendRecvMany()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe, serverPipe);

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            for (int i = 0; i < 30; ++i)
            {
                // Send message to client
                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
                {
                    strm.WriteInt((int) i);
                    m_ServerDriver.EndSend(strm);
                }
                m_ServerDriver.ScheduleUpdate().Complete();

                // Receive incoming message from server
                m_ClientDriver.ScheduleUpdate().Complete();

                var result = clientToServer.PopEvent(m_ClientDriver, out readStrm);

                Assert.AreEqual(NetworkEvent.Type.Data, result);
                Assert.AreEqual(4, readStrm.Length);
                Assert.AreEqual(i, readStrm.ReadInt());

                // Send back a message to server
                if (m_ClientDriver.BeginSend(clientPipe, clientToServer, out strm) == 0)
                {
                    strm.WriteInt((int) i*100);
                    m_ClientDriver.EndSend(strm);
                }
                m_ClientDriver.ScheduleUpdate().Complete();

                // Receive incoming message from client
                // 100 frames = 1600ms
                for (int frame = 0; frame < 100; ++frame)
                {
                    m_ServerDriver.ScheduleUpdate().Complete();
                    result = serverToClient.PopEvent(m_ServerDriver, out readStrm);
                    if (result != NetworkEvent.Type.Empty)
                        break;
                }
                Assert.AreEqual(NetworkEvent.Type.Data, result);
                Assert.AreEqual(4, readStrm.Length);
                Assert.AreEqual(i*100, readStrm.ReadInt());
            }
        }

        [Test]
        public unsafe void NetworkPipeline_ReliableSequenced_SendRecvManyWithPacketDropHighSeqId()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe, serverPipe);

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Set sequence ID to a value just below wrapping over 0, also need to set last received seqId value to one
            // less or the first packet will be considered out of order and stored for later use
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_ReliableStageId, clientToServer, out var receiveBuffer, out var sendBuffer, out var sharedBuffer);
            var sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = ushort.MaxValue - 1;
            sharedContext->SentPackets.Acked = ushort.MaxValue - 2;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            var receiveContext = (ReliableUtility.Context*) receiveBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            // This test runs fast so the minimum resend times needs to be lower (assumes 1 ms update rate)
            ReliableUtility.SetMinimumResendTime(4, m_ClientDriver, clientPipe, clientToServer);
            ReliableUtility.SetMinimumResendTime(4, m_ServerDriver, serverPipe, serverToClient);

            m_ServerDriver.GetPipelineBuffers(serverPipe, m_ReliableStageId, serverToClient, out receiveBuffer, out sendBuffer, out sharedBuffer);
            sharedContext = (ReliableUtility.SharedContext*) sharedBuffer.GetUnsafePtr();
            sharedContext->SentPackets.Sequence = ushort.MaxValue - 1;
            sharedContext->SentPackets.Acked = ushort.MaxValue - 2;
            sharedContext->SentPackets.AckMask = 0xFFFFFFFF;
            sharedContext->ReceivedPackets.Sequence = sharedContext->SentPackets.Acked;
            sharedContext->ReceivedPackets.AckMask = sharedContext->SentPackets.AckMask;
            receiveContext = (ReliableUtility.Context*) receiveBuffer.GetUnsafePtr();
            receiveContext->Delivered = sharedContext->SentPackets.Acked;

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();

            SendAndReceiveMessages(clientToServer, serverToClient, clientPipe, serverPipe);
        }

        [Test]
        public void NetworkPipeline_ReliableSequenced_SendRecvManyWithPacketDrop()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe, serverPipe);

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            // This test runs fast so the minimum resend times needs to be lower (assumes 1 ms update rate)
            ReliableUtility.SetMinimumResendTime(4, m_ClientDriver, clientPipe, clientToServer);
            ReliableUtility.SetMinimumResendTime(4, m_ServerDriver, serverPipe, serverToClient);

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();

            SendAndReceiveMessages(clientToServer, serverToClient, clientPipe, serverPipe);
        }

        unsafe void SendAndReceiveMessages(NetworkConnection clientToServer, NetworkConnection serverToClient, NetworkPipeline clientPipe, NetworkPipeline serverPipe)
        {
            DataStreamReader readStrm;

            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            // Next packet should be Empty and not Data as the packet was dropped
            Assert.AreEqual(NetworkEvent.Type.Empty, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            var totalMessageCount = 100;
            var sendMessageCount = 0;
            var lastClientReceivedNumber = 0;
            var lastServerReceivedNumber = 0;
            int frame = 0;
            m_ServerDriver.GetPipelineBuffers(serverPipe, m_ReliableStageId, serverToClient, out var tmpReceiveBuffer, out var tmpSendBuffer, out var serverReliableBuffer);
            var serverReliableCtx = (ReliableUtility.SharedContext*) serverReliableBuffer.GetUnsafePtr();
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_ReliableStageId, clientToServer, out tmpReceiveBuffer, out tmpSendBuffer, out var clientReliableBuffer);
            var clientReliableCtx = (ReliableUtility.SharedContext*) clientReliableBuffer.GetUnsafePtr();
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_SimulatorStageId, clientToServer, out tmpReceiveBuffer, out tmpSendBuffer, out var clientSimulatorBuffer);
            var clientSimulatorCtx = (SimulatorUtility.Context*) clientSimulatorBuffer.GetUnsafePtr();
            // Client is the one dropping packets, so wait for that count to reach total, server receive count will be higher
            while (lastClientReceivedNumber < totalMessageCount)
            {
                // Send message to client
                sendMessageCount++;

                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
                {
                    strm.WriteInt((int) sendMessageCount);
                    m_ServerDriver.EndSend(strm);
                }

                if (serverReliableCtx->errorCode != 0)
                {
                    UnityEngine.Debug.Log("Reliability stats\nPacketsDropped: " + serverReliableCtx->stats.PacketsDropped + "\n" +
                                          "PacketsDuplicated: " + serverReliableCtx->stats.PacketsDuplicated + "\n" +
                                          "PacketsOutOfOrder: " + serverReliableCtx->stats.PacketsOutOfOrder + "\n" +
                                          "PacketsReceived: " + serverReliableCtx->stats.PacketsReceived + "\n" +
                                          "PacketsResent: " + serverReliableCtx->stats.PacketsResent + "\n" +
                                          "PacketsSent: " + serverReliableCtx->stats.PacketsSent + "\n" +
                                          "PacketsStale: " + serverReliableCtx->stats.PacketsStale + "\n");
                    Assert.AreEqual((ReliableUtility.ErrorCodes)0, serverReliableCtx->errorCode);
                }
                m_ServerDriver.ScheduleUpdate().Complete();

                NetworkEvent.Type result;
                // Receive incoming message from server, might be empty but we still need to keep
                // sending or else a resend for a dropped packet will not happen
                m_ClientDriver.ScheduleUpdate().Complete();
                result = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                Assert.AreEqual(m_ClientDriver.ReceiveErrorCode, 0);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, clientReliableCtx->errorCode);
                while (result != NetworkEvent.Type.Empty)
                {
                    Assert.AreEqual(4, readStrm.Length);
                    var read = readStrm.ReadInt();
                    // We should be receiving in order, so last payload should be one more than the current receive count
                    Assert.AreEqual(lastClientReceivedNumber + 1, read);
                    lastClientReceivedNumber = read;
                    // Pop all events which might be pending (in case of dropped packet it should contain all the other packets already up to latest)
                    result = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                    Assert.AreEqual((ReliableUtility.ErrorCodes)0, clientReliableCtx->errorCode);
                }

                // Send back a message to server
                if (m_ClientDriver.BeginSend(clientPipe, clientToServer, out strm) == 0)
                {
                    strm.WriteInt((int) sendMessageCount * 100);
                    m_ClientDriver.EndSend(strm);
                }
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, clientReliableCtx->errorCode);
                m_ClientDriver.ScheduleUpdate().Complete();

                // Receive incoming message from client
                m_ServerDriver.ScheduleUpdate().Complete();
                result = serverToClient.PopEvent(m_ServerDriver, out readStrm);
                Assert.AreEqual(m_ServerDriver.ReceiveErrorCode, 0);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, serverReliableCtx->errorCode);
                while (result != NetworkEvent.Type.Empty)
                {
                    Assert.AreEqual(4, readStrm.Length);
                    var read = readStrm.ReadInt();
                    Assert.AreEqual(lastServerReceivedNumber + 100, read);
                    lastServerReceivedNumber = read;
                    result = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                    Assert.AreEqual((ReliableUtility.ErrorCodes)0, serverReliableCtx->errorCode);
                }

                //Assert.AreEqual(0, serverReliableCtx->stats.PacketsDuplicated);
                Assert.AreEqual(0, serverReliableCtx->stats.PacketsStale);
                //Assert.AreEqual(0, clientReliableCtx->stats.PacketsDuplicated);
                Assert.AreEqual(0, clientReliableCtx->stats.PacketsStale);

                if (frame > 100)
                    Assert.Fail("Test timeout, didn't receive all messages (" + totalMessageCount + ")");
                ++frame;
            }

            var stats = serverReliableCtx->stats;
            // You can get legtimate duplicated packets in the test, if the ack was just not received in time for the resend timer expired
            //Assert.AreEqual(stats.PacketsResent, clientSimulatorCtx->PacketDropCount);
            //Assert.AreEqual(stats.PacketsDuplicated, 0);
            Assert.AreEqual(stats.PacketsStale, 0);
            UnityEngine.Debug.Log("Server Reliability stats\nPacketsDropped: " + serverReliableCtx->stats.PacketsDropped + "\n" +
                                  "PacketsDuplicated: " + serverReliableCtx->stats.PacketsDuplicated + "\n" +
                                  "PacketsOutOfOrder: " + serverReliableCtx->stats.PacketsOutOfOrder + "\n" +
                                  "PacketsReceived: " + serverReliableCtx->stats.PacketsReceived + "\n" +
                                  "PacketsResent: " + serverReliableCtx->stats.PacketsResent + "\n" +
                                  "PacketsSent: " + serverReliableCtx->stats.PacketsSent + "\n" +
                                  "PacketsStale: " + serverReliableCtx->stats.PacketsStale + "\n");
            UnityEngine.Debug.Log("Client Reliability stats\nPacketsDropped: " + clientReliableCtx->stats.PacketsDropped + "\n" +
                                  "PacketsDuplicated: " + clientReliableCtx->stats.PacketsDuplicated + "\n" +
                                  "PacketsOutOfOrder: " + clientReliableCtx->stats.PacketsOutOfOrder + "\n" +
                                  "PacketsReceived: " + clientReliableCtx->stats.PacketsReceived + "\n" +
                                  "PacketsResent: " + clientReliableCtx->stats.PacketsResent + "\n" +
                                  "PacketsSent: " + clientReliableCtx->stats.PacketsSent + "\n" +
                                  "PacketsStale: " + clientReliableCtx->stats.PacketsStale + "\n");
            UnityEngine.Debug.Log("Client Simulator stats\n" +
                                  "PacketDropCount: " + clientSimulatorCtx->PacketDropCount + "\n" +
                                  "PacketCount: " + clientSimulatorCtx->PacketCount);
        }

        [Test]
        public void NetworkPipeline_UnreliableSequenced_SendRecvOnce()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe, serverPipe);

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            // Send message to client
            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
            {
                strm.WriteInt((int) 42);
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(4, readStrm.Length);
            Assert.AreEqual(42, readStrm.ReadInt());
        }

        [Test]
        public unsafe void NetworkPipeline_ReliableSequenced_ClientSendsNothing()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe, serverPipe);

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            // Do a loop where server sends to client but client sends nothing back, it should send empty ack packets back
            // so the servers queue will not get full
            var totalMessageCount = 100;
            var sendMessageCount = 0;
            var lastClientReceivedNumber = 0;
            int frame = 0;

            m_ServerDriver.GetPipelineBuffers(serverPipe, m_ReliableStageId, serverToClient, out var tmpReceiveBuffer, out var tmpSendBuffer, out var serverReliableBuffer);
            var serverReliableCtx = (ReliableUtility.SharedContext*) serverReliableBuffer.GetUnsafePtr();
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_ReliableStageId, clientToServer, out tmpReceiveBuffer, out tmpSendBuffer, out var clientReliableBuffer);
            var clientReliableCtx = (ReliableUtility.SharedContext*) clientReliableBuffer.GetUnsafePtr();

            // Finish when client has received all messages from server without errors
            while (lastClientReceivedNumber < totalMessageCount)
            {
                // Send message to client
                sendMessageCount++;
                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
                {
                    strm.WriteInt((int) sendMessageCount);
                    m_ServerDriver.EndSend(strm);
                }
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, serverReliableCtx->errorCode);
                m_ServerDriver.ScheduleUpdate().Complete();

                NetworkEvent.Type result;
                // Receive incoming message from server, might be empty or might be more than one message
                m_ClientDriver.ScheduleUpdate().Complete();
                result = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                Assert.AreEqual(m_ClientDriver.ReceiveErrorCode, 0);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, clientReliableCtx->errorCode);
                while (result != NetworkEvent.Type.Empty)
                {
                    Assert.AreEqual(4, readStrm.Length);
                    var read = readStrm.ReadInt();
                    // We should be receiving in order, so last payload should be one more than the current receive count
                    Assert.AreEqual(lastClientReceivedNumber + 1, read);
                    lastClientReceivedNumber = read;
                    // Pop all events which might be pending (in case of dropped packet it should contain all the other packets already up to latest)
                    result = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                    Assert.AreEqual((ReliableUtility.ErrorCodes)0, clientReliableCtx->errorCode);
                }

                // no-op
                m_ClientDriver.ScheduleUpdate().Complete();

                // Make sure no event has arrived on server and no errors seen
                m_ServerDriver.ScheduleUpdate().Complete();
                Assert.AreEqual(serverToClient.PopEvent(m_ServerDriver, out readStrm), NetworkEvent.Type.Empty);
                Assert.AreEqual(m_ServerDriver.ReceiveErrorCode, 0);
                Assert.AreEqual((ReliableUtility.ErrorCodes)0, serverReliableCtx->errorCode);

                if (frame > 100)
                    Assert.Fail("Test timeout, didn't receive all messages (" + totalMessageCount + ")");
                ++frame;
            }

            // The empty ack packets will bump the PacketsSent count, also in this test it can happen that a duplicate
            // packet is sent because the timers are tight
            //Assert.AreEqual(totalMessageCount, serverReliableCtx->stats.PacketsSent);
        }

        [Test]
        public unsafe void NetworkPipeline_ReliableSequenced_NothingIsSentAfterPingPong()
        {
            // Use simulator pipeline here just to count packets, need to reset the drivers for this setup
            m_ServerDriver.Dispose();
            m_ClientDriver.Dispose();
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = 90 * 1000,
                fixedFrameTimeMS = 16
            };
            m_ServerDriver =
                TestNetworkDriver.Create(new NetworkDataStreamParameter
                        {size = 0}, timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32},
                    new SimulatorUtility.Parameters { MaxPacketCount = 30, MaxPacketSize = 16, PacketDelayMs = 0, PacketDropPercentage = 0});
            m_ServerDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            m_ServerDriver.Listen();
            m_ClientDriver =
                TestNetworkDriver.Create(new NetworkDataStreamParameter
                        {size = 0}, timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32},
                    new SimulatorUtility.Parameters { MaxPacketCount = 30, MaxPacketSize = 16, PacketDelayMs = 0, PacketDropPercentage = 0});

            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            m_ClientDriver.ScheduleUpdate().Complete();
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();

            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            // Perform ping pong transmision

            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
            {
                strm.WriteInt((int) 100);
                Console.WriteLine("Server send");
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();
            Console.WriteLine("Client update");
            m_ClientDriver.ScheduleUpdate().Complete();
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            if (m_ClientDriver.BeginSend(clientPipe, clientToServer, out strm) == 0)
            {
                strm.WriteInt((int) 200);
                Console.WriteLine("Client send");
                m_ClientDriver.EndSend(strm);
            }
            m_ClientDriver.ScheduleUpdate().Complete();
            Console.WriteLine("Server update");
            m_ServerDriver.ScheduleUpdate().Complete();
            Assert.AreEqual(NetworkEvent.Type.Data, serverToClient.PopEvent(m_ServerDriver, out readStrm));

            // Check how many packets have been sent so far
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_SimulatorStageId, clientToServer, out var tmpReceiveBuffer, out var tmpSendBuffer, out var simulatorBuffer);
            var simulatorCtx = (SimulatorUtility.Context*) simulatorBuffer.GetUnsafePtr();

            // Do a loop and make sure nothing is being sent between client and server - 100 frames at 16ms = 1600ms
            for (int iter = 0; iter < 100; ++iter)
            {
                m_ServerDriver.ScheduleUpdate().Complete();
                m_ClientDriver.ScheduleUpdate().Complete();
                Assert.AreEqual(NetworkEvent.Type.Empty, serverToClient.PopEvent(m_ServerDriver, out readStrm));
                Assert.AreEqual(NetworkEvent.Type.Empty, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            }

            // The client simulator counts all packets which pass through the pipeline so will catch anything the
            // reliability pipeline might send, only 2 packets (data + ack packet) should have been received on client
            Assert.AreEqual(2, simulatorCtx->PacketCount);

            // Check server side as well, server only has one packet as the client included it's ack in the pong packet it sent
            m_ServerDriver.GetPipelineBuffers(serverPipe, m_SimulatorStageId, serverToClient, out tmpReceiveBuffer, out tmpSendBuffer, out simulatorBuffer);
            simulatorCtx = (SimulatorUtility.Context*) simulatorBuffer.GetUnsafePtr();
            Assert.AreEqual(1, simulatorCtx->PacketCount);
        }

        [Test]
        public unsafe void NetworkPipeline_ReliableSequenced_IdleAfterPacketDrop()
        {
            // Use simulator drop interval, then first packet will be dropped
            m_ClientDriver.Dispose();
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = 90 * 1000,
                fixedFrameTimeMS = 16
            };
            m_ClientDriver =
                TestNetworkDriver.Create(new NetworkDataStreamParameter
                        {size = 0}, timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32},
                    new SimulatorUtility.Parameters { MaxPacketCount = 30, MaxPacketSize = 16, PacketDelayMs = 0, PacketDropInterval = 10});

            var clientPipe = m_ClientDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            m_ClientDriver.ScheduleUpdate().Complete();
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();

            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            // Server sends one packet, this will be dropped, client has empty event
            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
            {
                strm.WriteInt((int) 100);
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();
            m_ClientDriver.ScheduleUpdate().Complete();
            Assert.AreEqual(NetworkEvent.Type.Empty, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            // Wait until client receives the server packet resend
            var clientEvent = NetworkEvent.Type.Empty;
            // 100 frames = 1600ms
            for (int frame = 0; frame < 100; ++frame)
            {
                m_ClientDriver.ScheduleUpdate().Complete();
                m_ServerDriver.ScheduleUpdate().Complete();
                clientEvent = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                if (clientEvent != NetworkEvent.Type.Empty)
                    break;
            }
            Assert.AreEqual(NetworkEvent.Type.Data, clientEvent);

            // Verify exactly one packet has been dropped
            m_ClientDriver.GetPipelineBuffers(clientPipe, m_SimulatorStageId, clientToServer, out var tmpReceiveBuffer, out var tmpSendBuffer, out var simulatorBuffer);
            var simulatorCtx = (SimulatorUtility.Context*) simulatorBuffer.GetUnsafePtr();
            Assert.AreEqual(simulatorCtx->PacketDropCount, 1);
        }

        [Test]
        public unsafe void NetworkPipeline_ReliableSequenced_CanRecoverFromPause()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TempDisconnectSendPipelineStage), typeof(ReliableSequencedPipelineStage), typeof(TempDisconnectPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            m_ClientDriver.ScheduleUpdate().Complete();
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();

            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            m_ServerDriver.ScheduleUpdate().Complete();
            m_ClientDriver.ScheduleUpdate().Complete();
            Assert.AreEqual(NetworkEvent.Type.Empty, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            // 100 frames = 1600ms
            int firstFailed = 0;
            int numFailed = 0;
            int nextRecv = 0;
            for (int frame = 0; frame < 300; ++frame)
            {
                if (frame == 100)
                {
                    // Ignore all send and receive calls on the client after 100 frames
                    *TempDisconnectPipelineStage.s_StaticInstanceBuffer = 0;
                    *TempDisconnectSendPipelineStage.s_StaticInstanceBuffer = 0;
                }
                else if (frame == 200)
                {
                    // Resume send and receive calls again after 200 frames
                    *TempDisconnectPipelineStage.s_StaticInstanceBuffer = 1;
                    *TempDisconnectSendPipelineStage.s_StaticInstanceBuffer = 1;
                }
                int sendStatus = -1;
                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0)
                {
                    strm.WriteInt((int) frame);
                    sendStatus = m_ServerDriver.EndSend(strm);
                }
                if (sendStatus != 4)
                {
                    if (numFailed == 0)
                        firstFailed = frame;
                    ++numFailed;
                }

                m_ServerDriver.ScheduleUpdate().Complete();
                m_ClientDriver.ScheduleUpdate().Complete();
                bool gotData = true;
                while (gotData)
                {
                    var clientEvent = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                    if (clientEvent == NetworkEvent.Type.Data)
                    {
                        if (nextRecv == firstFailed)
                            nextRecv += numFailed;
                        var recv = readStrm.ReadInt();
                        Assert.AreEqual(nextRecv, recv);
                        nextRecv = recv+1;
                    }
                    else
                        gotData = false;
                }
            }
            Assert.Greater(numFailed, 0);
            Assert.AreEqual(300, nextRecv);
        }
    }
}
