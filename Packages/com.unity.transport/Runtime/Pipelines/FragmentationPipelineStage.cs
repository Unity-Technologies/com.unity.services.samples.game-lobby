using System;
using AOT;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Utilities;

namespace Unity.Networking.Transport
{
    [BurstCompile]
    public unsafe struct FragmentationPipelineStage : INetworkPipelineStage
    {
        public struct FragContext
        {
            public int startIndex;
            public int endIndex;
            public int sequence;
            public bool packetError;
        }

        [Flags]
        enum FragFlags
        {
            First = 1 << 15,
            Last = 1 << 14,
            SeqMask = Last - 1
        }

#if FRAGMENTATION_DEBUG
        const int FragHeaderCapacity = 2 + 4;    // 2 bits for First/Last flags, 14 bits sequence number
#else
        const int FragHeaderCapacity = 2;    // 2 bits for First/Last flags, 14 bits sequence number
#endif
        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests requests)
        {
            var fragContext = (FragContext*) ctx.internalProcessBuffer;
            var dataBuffer = ctx.internalProcessBuffer + sizeof(FragContext);
            var param = (FragmentationUtility.Parameters*)ctx.staticInstanceBuffer;

            FragFlags flags = FragFlags.First;
            int headerCapacity = ctx.header.Capacity;

            var systemHeaderCapacity = sizeof(UdpCHeader) + 1 + 2;    // Extra byte is for pipeline id, two bytes for footer
            var maxBlockLength = NetworkParameterConstants.MTU - systemHeaderCapacity - inboundBuffer.headerPadding;
            var maxBlockLengthFirstPacket = maxBlockLength - ctx.accumulatedHeaderCapacity; // The first packet has the headers for all pipeline stages before this one

            if (fragContext->endIndex > fragContext->startIndex)
            {
                var isResume = 0 == inboundBuffer.bufferLength;
                if (!isResume)
                    throw new InvalidOperationException("Internal error: we encountered data in the fragmentation buffer, but this is not a resume call.");

                // We have data left over from a previous call
                flags &= ~FragFlags.First;
                var blockLength = fragContext->endIndex - fragContext->startIndex;
                if (blockLength > maxBlockLength)
                {
                    blockLength = maxBlockLength;
                }
                var blockStart = dataBuffer + fragContext->startIndex;
                inboundBuffer.buffer = blockStart;
                inboundBuffer.bufferWithHeaders = blockStart - inboundBuffer.headerPadding;
                inboundBuffer.bufferLength = blockLength;
                inboundBuffer.bufferWithHeadersLength = blockLength + inboundBuffer.headerPadding;
                fragContext->startIndex += blockLength;
            }
            else if (inboundBuffer.bufferLength > maxBlockLengthFirstPacket)
            {
                var payloadCapacity = param->PayloadCapacity;
                var excessLength = inboundBuffer.bufferLength - maxBlockLengthFirstPacket;
                var excessStart = inboundBuffer.buffer + maxBlockLengthFirstPacket;
                if (excessLength + inboundBuffer.headerPadding > payloadCapacity)
                {
                    throw new InvalidOperationException($"Fragmentation capacity exceeded. Capacity:{payloadCapacity} Payload:{excessLength + inboundBuffer.headerPadding}");
                }
                UnsafeUtility.MemCpy(dataBuffer + inboundBuffer.headerPadding, excessStart, excessLength);
                fragContext->startIndex = inboundBuffer.headerPadding; // Leaving room for header
                fragContext->endIndex = excessLength + inboundBuffer.headerPadding;
                inboundBuffer.bufferWithHeadersLength -= excessLength;
                inboundBuffer.bufferLength -= excessLength;
            }

            if (fragContext->endIndex > fragContext->startIndex)
            {
                requests |= NetworkPipelineStage.Requests.Resume;
            }
            else
            {
                flags |= FragFlags.Last;
            }

            var sequence = fragContext->sequence++;

            var combined = (sequence & (int)FragFlags.SeqMask) | (int)flags;    // lower 14 bits sequence, top 2 bits flags
            ctx.header.WriteShort((short)combined);

#if FRAGMENTATION_DEBUG
            // For debugging - this allows WireShark to identify fragmentation packets
            ctx.header.WriteByte((byte) '@');
            ctx.header.WriteByte((byte) '@');
            ctx.header.WriteByte((byte) '@');
            ctx.header.WriteByte((byte) '@');
#endif
            return (int)Error.StatusCode.Success;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests requests)
        {
            var fragContext = (FragContext*) ctx.internalProcessBuffer;
            var dataBuffer = ctx.internalProcessBuffer + sizeof(FragContext);
            var param = (FragmentationUtility.Parameters*)ctx.staticInstanceBuffer;

            var inboundArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(inboundBuffer.buffer, inboundBuffer.bufferLength, Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandle = AtomicSafetyHandle.GetTempMemoryHandle();
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref inboundArray, safetyHandle);
#endif
            var reader = new DataStreamReader(inboundArray);

            var combined = reader.ReadShort();
            var foundSequence = combined & (int)FragFlags.SeqMask;
            var flags = (FragFlags)combined & ~FragFlags.SeqMask;
            inboundBuffer = inboundBuffer.Slice(FragHeaderCapacity);

            var expectedSequence = fragContext->sequence;
            var isFirst = 0 != (flags & FragFlags.First);
            var isLast = 0 != (flags & FragFlags.Last);

            if (isFirst)
            {
                expectedSequence = foundSequence;
                fragContext->packetError = false;
                fragContext->endIndex = 0;
            }

            if (foundSequence != expectedSequence)
            {
                // We've missed a packet.
                fragContext->packetError = true;
                fragContext->endIndex = 0;        // Discard data we have already collected
            }

            if (!fragContext->packetError)
            {
                if (!isLast || fragContext->endIndex > 0)
                {
                    if (fragContext->endIndex + inboundBuffer.bufferLength > param->PayloadCapacity)
                    {
                        throw new InvalidOperationException($"Fragmentation capacity exceeded");
                    }
                    // Append the data to the end
                    UnsafeUtility.MemCpy(dataBuffer + fragContext->endIndex, inboundBuffer.buffer, inboundBuffer.bufferLength);
                    fragContext->endIndex += inboundBuffer.bufferLength;
                }

                if (isLast && fragContext->endIndex > 0)
                {
                    // Data is complete
                    inboundBuffer = new InboundRecvBuffer
                    {
                        buffer = dataBuffer,
                        bufferLength = fragContext->endIndex
                    };
                }
            }

            if (!isLast || fragContext->packetError)
            {
                // No output if we expect more data, or if data is incomplete due to packet loss
                inboundBuffer = default;
            }

            fragContext->sequence = (foundSequence + 1) & (int)FragFlags.SeqMask;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* recvProcessBuffer, int recvProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
        }
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            FragmentationUtility.Parameters param = default;
            foreach (var netParam in netParams)
            {
                if (netParam is FragmentationUtility.Parameters)
                    param = (FragmentationUtility.Parameters)netParam;
            }

            var payloadCapacity = param.PayloadCapacity;

            if (payloadCapacity == 0)
                payloadCapacity = 4 * 1024;

            if (payloadCapacity <= NetworkParameterConstants.MTU)
                throw new InvalidOperationException($"Please specify a FragmentationUtility.Parameters with a PayloadCapacity greater than MTU, which is {NetworkParameterConstants.MTU}");

            param.PayloadCapacity = payloadCapacity;

            UnsafeUtility.MemCpy(staticInstanceBuffer, &param, UnsafeUtility.SizeOf<FragmentationUtility.Parameters>());

            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: sizeof(FragContext) + payloadCapacity,
                SendCapacity: sizeof(FragContext) + payloadCapacity,
                HeaderCapacity: FragHeaderCapacity,
                SharedStateCapacity: 0,
                param.PayloadCapacity
            );
        }

        public int StaticSize => UnsafeUtility.SizeOf<FragmentationUtility.Parameters>();
    }
}
