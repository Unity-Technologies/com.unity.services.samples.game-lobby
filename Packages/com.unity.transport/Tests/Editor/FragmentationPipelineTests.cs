using System;
using System.Threading;
using AOT;
using NUnit.Framework;
using Unity.Networking.Transport.Utilities;
using Unity.Burst;

namespace Unity.Networking.Transport.Tests
{
    [BurstCompile]
    public unsafe struct TempDropPacketPipelineStage : INetworkPipelineStage
    {
        public static byte* s_StaticInstanceBuffer;
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            s_StaticInstanceBuffer = staticInstanceBuffer;
            *staticInstanceBuffer = 0;
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
        public int StaticSize => 2;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            byte idx = ctx.staticInstanceBuffer[1];
            if (ctx.staticInstanceBuffer[0] == idx)
            {
                // Drop the packet
                inboundBuffer = default;
            }
            *ctx.staticInstanceBuffer += 1;
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
    public struct TempDropPacketPipelineStageCollection
    {
        public static void Register()
        {
            NetworkPipelineStageCollection.RegisterPipelineStage(new TempDropPacketPipelineStage());
        }
    }

    public class FragmentationPipelineTests
    {
        private NetworkDriver m_ServerDriver;
        private NetworkDriver m_ClientDriver;

        [SetUp]
        public void IPC_Setup()
        {
            TempDropPacketPipelineStageCollection.Register();
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = 90 * 1000,
                fixedFrameTimeMS = 16
            };
            m_ServerDriver =
                TestNetworkDriver.Create(
                    new NetworkDataStreamParameter {size = 0},
                    timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32 },
                    new FragmentationUtility.Parameters { PayloadCapacity = 4 * 1024 });
            m_ServerDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            m_ServerDriver.Listen();
            m_ClientDriver =
                TestNetworkDriver.Create(
                    new NetworkDataStreamParameter {size = 0},
                    timeoutParam,
                    new ReliableUtility.Parameters { WindowSize = 32 },
                    new SimulatorUtility.Parameters { MaxPacketCount = 30, MaxPacketSize = 16, PacketDelayMs = 0, /*PacketDropInterval = 8,*/ PacketDropPercentage = 10},
                    new FragmentationUtility.Parameters { PayloadCapacity = 4 * 1024 });
        }

        [TearDown]
        public void IPC_TearDown()
        {
            m_ClientDriver.Dispose();
            m_ServerDriver.Dispose();
        }

        [Test]
        public void NetworkPipeline_Fragmentation_SendRecvOnce()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(FragmentationPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(FragmentationPipelineStage));

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
                strm.WriteInt(42);
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(sizeof(int), readStrm.Length);
            Assert.AreEqual(42, readStrm.ReadInt());
        }

        [Test]
        public void NetworkPipeline_Fragmentation_SendRecvOversized()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(FragmentationPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(FragmentationPipelineStage));

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            int messageSize = 3000;
            int intCount = messageSize / sizeof(int);

            // Send message to client
            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm, messageSize) == 0)
            {
                for (int i = 0; i < intCount; ++i)
                {
                    strm.WriteInt(i);
                }
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            Assert.AreEqual(messageSize, readStrm.Length);
            for (int i = 0; i < intCount; ++i)
            {
                Assert.AreEqual(i, readStrm.ReadInt());
            }
        }
        [Test]
        public void NetworkPipeline_Fragmentation_SendRecvMaxSize()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(FragmentationPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(FragmentationPipelineStage));

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            int messageSize = 4*1024-m_ServerDriver.MaxHeaderSize(serverPipe);

            // Send message to client
            if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm, messageSize) == 0)
            {
                for (int i = 0; i < messageSize; ++i)
                {
                    strm.WriteByte((byte)i);
                }
                m_ServerDriver.EndSend(strm);
            }
            m_ServerDriver.ScheduleUpdate().Complete();

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));

            Assert.AreEqual(messageSize, readStrm.Length);
            for (int i = 0; i < messageSize; ++i)
            {
                Assert.AreEqual((byte)i, readStrm.ReadByte());
            }
        }

        [Test]
        public unsafe void NetworkPipeline_Fragmentation_DroppedPacket()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(FragmentationPipelineStage),
                typeof(TempDropPacketPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(FragmentationPipelineStage));

            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            m_ClientDriver.ScheduleUpdate().Complete();

            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);

            int messageSize = 3000;
            int intCount = messageSize / sizeof(int);
            int messageCount = 3;

            int packetCount = -1;
            for(int dropIndex = 0; dropIndex != packetCount; ++dropIndex)
            {
                TempDropPacketPipelineStage.s_StaticInstanceBuffer[0] = 0; // Reset packet counter
                TempDropPacketPipelineStage.s_StaticInstanceBuffer[1] = (byte)dropIndex;

                for (int j = 0; j < messageCount; ++j)
                {
                    // Send one message
                    if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm, messageSize) == 0)
                    {
                        for (int i = 0; i < intCount; ++i)
                        {
                            strm.WriteInt(i);
                        }

                        m_ServerDriver.EndSend(strm);
                    }
                }

                m_ServerDriver.ScheduleUpdate().Complete();
                m_ClientDriver.ScheduleUpdate().Complete();

                packetCount = TempDropPacketPipelineStage.s_StaticInstanceBuffer[0];

                {
                    // We have dropped one fragment. The result should be that one complete fragmented message
                    // is discarded, and the remaining messageCount - 1 are intact.
                    DataStreamReader readStrm;
                    NetworkEvent.Type eventType;
                    if (dropIndex == 0)    // First pass only
                    {
                        eventType = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                        Assert.AreEqual(NetworkEvent.Type.Connect, eventType);
                    }

                    for (int j = 0; j < messageCount - 1; ++j)
                    {
                        eventType = clientToServer.PopEvent(m_ClientDriver, out readStrm);
                        Assert.AreEqual(NetworkEvent.Type.Data, eventType);
                        Assert.AreEqual(messageSize, readStrm.Length);
                        for (int i = 0; i < intCount; ++i)
                        {
                            Assert.AreEqual(i, readStrm.ReadInt());
                        }
                    }

                    Assert.AreEqual(NetworkEvent.Type.Empty, clientToServer.PopEvent(m_ClientDriver, out readStrm));
                }
            }
        }
        [Test]
        public void NetworkPipeline_Fragmentation_Unreliable_SendRecv1380_Plus()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(FragmentationPipelineStage), typeof(UnreliableSequencedPipelineStage));

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
            for (int messageSize = 1380; messageSize <= 1400; ++messageSize)
            {
                // Send message to client
                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm, messageSize) == 0)
                {
                    for (int i = 0; i < messageSize; ++i)
                    {
                        strm.WriteByte((byte)i);
                    }
                    m_ServerDriver.EndSend(strm);
                }
                m_ServerDriver.ScheduleUpdate().Complete();

                // Receive incoming message from server
                m_ClientDriver.ScheduleUpdate().Complete();
                Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));

                Assert.AreEqual(messageSize, readStrm.Length);
                for (int i = 0; i < messageSize; ++i)
                {
                    Assert.AreEqual((byte)i, readStrm.ReadByte());
                }
            }
        }
        [Test]
        public void NetworkPipeline_Unreliable_Fragmentation_SendRecv1380_Plus()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(FragmentationPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(FragmentationPipelineStage));

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

            for (int messageSize = 1380; messageSize <= 1400; ++messageSize)
            {
                // Send message to client
                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm, messageSize) == 0)
                {
                    for (int i = 0; i < messageSize; ++i)
                    {
                        strm.WriteByte((byte)i);
                    }
                    m_ServerDriver.EndSend(strm);
                }
                m_ServerDriver.ScheduleUpdate().Complete();

                // Receive incoming message from server
                m_ClientDriver.ScheduleUpdate().Complete();
                Assert.AreEqual(NetworkEvent.Type.Data, clientToServer.PopEvent(m_ClientDriver, out readStrm));

                Assert.AreEqual(messageSize, readStrm.Length);
                for (int i = 0; i < messageSize; ++i)
                {
                    Assert.AreEqual((byte)i, readStrm.ReadByte());
                }
            }
        }
    }
}
