using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Random = Unity.Mathematics.Random;

namespace Unity.Networking.Transport.Utilities
{
    public struct SimulatorUtility
    {
        private int m_PacketCount;
        private int m_MaxPacketSize;
        private int m_PacketDelayMs;
        private int m_PacketJitterMs;

        /// <summary>
        /// Configuration parameters for the simulator pipeline stage.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Parameters : INetworkParameter
        {
            /// <summary>
            /// The maximum amount of packets the pipeline can keep track of. This used when a
            /// packet is delayed, the packet is stored in the pipeline processing buffer and can
            /// be later brought back.
            /// </summary>
            public int MaxPacketCount;
            /// <summary>
            /// The maximum size of a packet which the simulator stores. If a packet exceeds this size it will
            /// bypass the simulator.
            /// </summary>
            public int MaxPacketSize;
            /// <summary>
            /// Fixed delay to apply to all packets which pass through.
            /// </summary>
            public int PacketDelayMs;
            /// <summary>
            /// Variable delay to apply to all packets which pass through, adds or subtracts amount from fixed delay.
            /// </summary>
            public int PacketJitterMs;
            /// <summary>
            /// Fixed interval to drop packets on. This is most suitable for tests where predictable
            /// behaviour is desired, every Xth packet will be dropped. If PacketDropInterval is 5
            /// every 5th packet is dropped.
            /// </summary>
            public int PacketDropInterval;
            /// <summary>
            /// Use a drop percentage when deciding when to drop packet. For every packet
            /// a random number generator is used to determine if the packet should be dropped or not.
            /// A percentage of 5 means approximately every 20th packet will be dropped.
            /// </summary>
            public int PacketDropPercentage;
            /// <summary>
            /// Use the fuzz factor when you want to fuzz a packet. For every packet
            /// a random number generator is used to determine if the packet should have the internal bits flipped.
            /// A percentage of 5 means approximately every 20th packet will be fuzzed, and that each bit in the packet
            /// has a 5 percent chance to get flipped.
            /// </summary>
            public int FuzzFactor;
            /// <summary>
            /// Use the fuzz offset in conjunction with the fuzz factor, the fuzz offset will offset where we start
            /// flipping bits. This is useful if you want to only fuzz a part of the packet.
            /// </summary>
            public int FuzzOffset;
            /// <summary>
            /// The random seed is used to set the initial seed of the random number generator. This is useful to get
            /// deterministic runs in tests for example that are dependant on the random number generator.
            /// </summary>
            public uint RandomSeed;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Context
        {
            public int MaxPacketCount;
            public int MaxPacketSize;
            public int PacketDelayMs;
            public int PacketJitterMs;
            public int PacketDrop;
            public int FuzzOffset;
            public int FuzzFactor;

            public uint RandomSeed;
            public Random Random;

            // Statistics
            public int PacketCount;
            public int PacketDropCount;
            public int ReadyPackets;
            public int WaitingPackets;
            public long NextPacketTime;
            public long StatsTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DelayedPacket
        {
            public int processBufferOffset;
            public ushort packetSize;
            public ushort packetHeaderPadding;
            public long delayUntil;
        }

        public SimulatorUtility(int packetCount, int maxPacketSize, int packetDelayMs, int packetJitterMs)
        {
            m_PacketCount = packetCount;
            m_MaxPacketSize = maxPacketSize;
            m_PacketDelayMs = packetDelayMs;
            m_PacketJitterMs = packetJitterMs;
        }

        public static unsafe void InitializeContext(Parameters param, byte* sharedProcessBuffer)
        {
            // Store parameters in the shared buffer space
            Context* ctx = (Context*) sharedProcessBuffer;
            ctx->MaxPacketCount = param.MaxPacketCount;
            ctx->MaxPacketSize = param.MaxPacketSize;
            ctx->PacketDelayMs = param.PacketDelayMs;
            ctx->PacketJitterMs = param.PacketJitterMs;
            ctx->PacketDrop = param.PacketDropInterval;
            ctx->FuzzFactor = param.FuzzFactor;
            ctx->FuzzOffset = param.FuzzOffset;
            ctx->PacketCount = 0;
            ctx->PacketDropCount = 0;
            ctx->Random = new Random();
            if (param.RandomSeed > 0)
            {
                ctx->Random.InitState(param.RandomSeed);
                ctx->RandomSeed = param.RandomSeed;
            }
            else
                ctx->Random.InitState();
        }

        public unsafe bool GetEmptyDataSlot(byte* processBufferPtr, ref int packetPayloadOffset,
            ref int packetDataOffset)
        {
            var dataSize = UnsafeUtility.SizeOf<DelayedPacket>();
            var packetPayloadStartOffset = m_PacketCount * dataSize;

            bool foundSlot = false;
            for (int i = 0; i < m_PacketCount; i++)
            {
                packetDataOffset = dataSize * i;
                DelayedPacket* packetData = (DelayedPacket*) (processBufferPtr + packetDataOffset);

                // Check if this slot is empty
                if (packetData->delayUntil == 0)
                {
                    foundSlot = true;
                    packetPayloadOffset = packetPayloadStartOffset + m_MaxPacketSize * i;
                    break;
                }
            }

            return foundSlot;
        }

        public unsafe bool GetDelayedPacket(ref NetworkPipelineContext ctx, ref InboundSendBuffer delayedPacket,
            ref NetworkPipelineStage.Requests requests, long currentTimestamp)
        {
            requests = NetworkPipelineStage.Requests.None;

            var dataSize = UnsafeUtility.SizeOf<DelayedPacket>();
            byte* processBufferPtr = (byte*) ctx.internalProcessBuffer;
            var simCtx = (Context*) ctx.internalSharedProcessBuffer;
            int oldestPacketIndex = -1;
            long oldestTime = long.MaxValue;
            int readyPackets = 0;
            int packetsInQueue = 0;
            for (int i = 0; i < m_PacketCount; i++)
            {
                DelayedPacket* packet = (DelayedPacket*) (processBufferPtr + dataSize * i);
                if ((int) packet->delayUntil == 0) continue;
                packetsInQueue++;

                if (packet->delayUntil > currentTimestamp) continue;
                readyPackets++;

                if (oldestTime <= packet->delayUntil) continue;
                oldestPacketIndex = i;
                oldestTime = packet->delayUntil;
            }

            simCtx->ReadyPackets = readyPackets;
            simCtx->WaitingPackets = packetsInQueue;
            simCtx->NextPacketTime = oldestTime;
            simCtx->StatsTime = currentTimestamp;

            // If more than one item has expired timer we need to resume this pipeline stage
            if (readyPackets > 1)
            {
                requests |= NetworkPipelineStage.Requests.Resume;
            }
            // If more than one item is present (but doesn't have expired timer) we need to re-run the pipeline
            // in a later update call
            else if (packetsInQueue > 0)
            {
                requests |= NetworkPipelineStage.Requests.Update;
            }

            if (oldestPacketIndex >= 0)
            {
                DelayedPacket* packet = (DelayedPacket*) (processBufferPtr + dataSize * oldestPacketIndex);
                packet->delayUntil = 0;

                delayedPacket.bufferWithHeaders = ctx.internalProcessBuffer + packet->processBufferOffset;
                delayedPacket.bufferWithHeadersLength = packet->packetSize;
                delayedPacket.headerPadding = packet->packetHeaderPadding;
                delayedPacket.SetBufferFrombufferWithHeaders();
                return true;
            }

            return false;
        }

        public unsafe void FuzzPacket(Context *ctx, ref InboundSendBuffer inboundBuffer)
        {
            int fuzzFactor = ctx->FuzzFactor;
            int fuzzOffset = ctx->FuzzOffset;
            int rand = ctx->Random.NextInt(0, 100);
            if (rand > fuzzFactor)
                return;

            var length = inboundBuffer.bufferLength;
            for (int i = fuzzOffset; i < length; ++i)
            {
                for (int j = 0; j < 8; ++j)
                {
                    if (fuzzFactor > ctx->Random.NextInt(0, 100))
                    {
                        inboundBuffer.buffer[i] ^= (byte)(1 << j);
                    }
                }
            }
        }

        public unsafe bool DelayPacket(ref NetworkPipelineContext ctx, InboundSendBuffer inboundBuffer,
            ref NetworkPipelineStage.Requests requests,
            long timestamp)
        {
            // Find empty slot in bookkeeping data space to track this packet
            int packetPayloadOffset = 0;
            int packetDataOffset = 0;
            var processBufferPtr = (byte*) ctx.internalProcessBuffer;
            bool foundSlot = GetEmptyDataSlot(processBufferPtr, ref packetPayloadOffset, ref packetDataOffset);

            if (!foundSlot)
            {
                //UnityEngine.Debug.LogWarning("No space left for delaying packet (" + m_PacketCount + " packets in queue)");
                return false;
            }

            UnsafeUtility.MemCpy(ctx.internalProcessBuffer + packetPayloadOffset + inboundBuffer.headerPadding, inboundBuffer.buffer, inboundBuffer.bufferLength);

            var param = (SimulatorUtility.Context*) ctx.internalSharedProcessBuffer;
            // Add tracking for this packet so we can resurrect later
            DelayedPacket packet;
            packet.delayUntil = timestamp + m_PacketDelayMs + param->Random.NextInt(m_PacketJitterMs*2) - m_PacketJitterMs;
            packet.processBufferOffset = packetPayloadOffset;
            packet.packetSize = (ushort)(inboundBuffer.headerPadding + inboundBuffer.bufferLength);
            packet.packetHeaderPadding = (ushort)inboundBuffer.headerPadding;
            byte* packetPtr = (byte*) &packet;
            UnsafeUtility.MemCpy(processBufferPtr + packetDataOffset, packetPtr, UnsafeUtility.SizeOf<DelayedPacket>());

            // Schedule an update call so packet can be resurrected later
            requests |= NetworkPipelineStage.Requests.Update;
            return true;
        }

        public unsafe bool ShouldDropPacket(Context* ctx, Parameters param, long timestamp)
        {
            if (param.PacketDropInterval > 0 && (ctx->PacketCount - 1) % param.PacketDropInterval == 0)
                return true;
            if (param.PacketDropPercentage > 0)
            {
                //var packetLoss = new System.Random().NextDouble() * 100;
                var packetLoss = ctx->Random.NextInt(0, 100);
                if (packetLoss < param.PacketDropPercentage)
                    return true;
            }

            return false;
        }
    }
}