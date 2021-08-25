using System;
using AOT;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Utilities;
using Unity.Burst;

namespace Unity.Networking.Transport.Tests
{
    [BurstCompile]
    public unsafe struct TestPipelineStageWithHeader : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 4,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var headerData = (int*)inboundBuffer.buffer;
            if (*headerData != 1)
                throw new InvalidOperationException("Header data invalid, got " + *headerData);
            inboundBuffer = inboundBuffer.Slice(4);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            ctx.header.WriteInt((int) 1);
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
    public unsafe struct TestPipelineStageWithHeaderTwo : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 0,
                SendCapacity: 0,
                HeaderCapacity: 4,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var headerData = (int*)inboundBuffer.buffer;
            if (*headerData != 2)
                throw new InvalidOperationException("Header data invalid, got " + *headerData);

            inboundBuffer = inboundBuffer.Slice(4);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            ctx.header.WriteInt((int) 2);
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
    public unsafe struct TestEncryptPipelineStage : INetworkPipelineStage
    {
        private const int k_MaxPacketSize = 64;
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: k_MaxPacketSize,
                SendCapacity: k_MaxPacketSize,
                HeaderCapacity: 0,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            for (int i = 0; i < inboundBuffer.bufferLength; ++i)
                ctx.internalProcessBuffer[i] = (byte)(inboundBuffer.buffer[i] ^ 0xff);
            inboundBuffer.buffer = ctx.internalProcessBuffer;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var len = inboundBuffer.bufferLength;
            for (int i = 0; i < len; ++i)
                ctx.internalProcessBuffer[inboundBuffer.headerPadding + i] = (byte)(inboundBuffer.buffer[i] ^ 0xff);
            var nextInbound = default(InboundSendBuffer);
            nextInbound.bufferWithHeaders =  ctx.internalProcessBuffer;
            nextInbound.bufferWithHeadersLength = len + inboundBuffer.headerPadding;
            nextInbound.SetBufferFrombufferWithHeaders();
            inboundBuffer = nextInbound;
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
    public unsafe struct TestEncryptInPlacePipelineStage : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 0,
                SendCapacity: NetworkParameterConstants.MTU,
                HeaderCapacity: 0,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            for (int i = 0; i < inboundBuffer.bufferLength; ++i)
                inboundBuffer.buffer[i] = (byte)(inboundBuffer.buffer[i] ^ 0xff);
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var len = inboundBuffer.bufferLength;
            for (int i = 0; i < len; ++i)
                ctx.internalProcessBuffer[inboundBuffer.headerPadding + i] = (byte)(inboundBuffer.buffer[i] ^ 0xff);
            var nextInbound = default(InboundSendBuffer);
            nextInbound.bufferWithHeaders =  ctx.internalProcessBuffer;
            nextInbound.bufferWithHeadersLength = len + inboundBuffer.headerPadding;
            nextInbound.SetBufferFrombufferWithHeaders();
            inboundBuffer = nextInbound;
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
    public unsafe struct TestInvertPipelineStage : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 0,
                SendCapacity: NetworkParameterConstants.MTU,
                HeaderCapacity: 0,
                SharedStateCapacity: 0
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static unsafe int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var len = inboundBuffer.bufferLength;
            for (int i = 0; i < len; ++i)
                ctx.internalProcessBuffer[inboundBuffer.headerPadding + i] = (byte)(inboundBuffer.buffer[i] ^ 0xff);
            var nextInbound = default(InboundSendBuffer);
            nextInbound.bufferWithHeaders =  ctx.internalProcessBuffer;
            nextInbound.bufferWithHeadersLength = len + inboundBuffer.headerPadding;
            nextInbound.SetBufferFrombufferWithHeaders();
            inboundBuffer = nextInbound;
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
    public unsafe struct TestPipelineWithInitializers : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 3*UnsafeUtility.SizeOf<int>(),
                SendCapacity: 3*UnsafeUtility.SizeOf<int>(),
                HeaderCapacity: 0,
                SharedStateCapacity: 3*UnsafeUtility.SizeOf<int>()
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var receiveData = (int*)ctx.internalProcessBuffer;
            for (int i = 4; i <= 6; ++i)
            {
                Assert.AreEqual(*receiveData, i);
                receiveData++;
            }
            var sharedData = (int*)ctx.internalSharedProcessBuffer;
            for (int i = 7; i <= 8; ++i)
            {
                Assert.AreEqual(*sharedData, i);
                sharedData++;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var sendData = (int*)ctx.internalProcessBuffer;
            for (int i = 1; i <= 3; ++i)
            {
                Assert.AreEqual(*sendData, i);
                sendData++;
            }
            var sharedData = (int*)ctx.internalSharedProcessBuffer;
            for (int i = 7; i <= 8; ++i)
            {
                Assert.AreEqual(*sharedData, i);
                sharedData++;
            }
            return (int)Error.StatusCode.Success;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* recvProcessBuffer, int recvProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
            var sendData = (int*)sendProcessBuffer;
            *sendData = 1;
            sendData++;
            *sendData = 2;
            sendData++;
            *sendData = 3;
            var receiveData = (int*)recvProcessBuffer;
            *receiveData = 4;
            receiveData++;
            *receiveData = 5;
            receiveData++;
            *receiveData = 6;
            var sharedData = (int*) sharedProcessBuffer;
            *sharedData = 7;
            sharedData++;
            *sharedData = 8;
            sharedData++;
            *sharedData = 9;
        }
    }

    [BurstCompile]
    public unsafe struct TestPipelineWithInitializersTwo : INetworkPipelineStage
    {
        static TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate> ReceiveFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.ReceiveDelegate>(Receive);
        static TransportFunctionPointer<NetworkPipelineStage.SendDelegate> SendFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.SendDelegate>(Send);
        static TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate> InitializeConnectionFunctionPointer = new TransportFunctionPointer<NetworkPipelineStage.InitializeConnectionDelegate>(InitializeConnection);
        public NetworkPipelineStage StaticInitialize(byte* staticInstanceBuffer, int staticInstanceBufferLength, INetworkParameter[] netParams)
        {
            return new NetworkPipelineStage(
                Receive: ReceiveFunctionPointer,
                Send: SendFunctionPointer,
                InitializeConnection: InitializeConnectionFunctionPointer,
                ReceiveCapacity: 3*UnsafeUtility.SizeOf<int>(),
                SendCapacity: 3*UnsafeUtility.SizeOf<int>(),
                HeaderCapacity: 0,
                SharedStateCapacity: 3*UnsafeUtility.SizeOf<int>()
            );
        }
        public int StaticSize => 0;

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.ReceiveDelegate))]
        private static void Receive(ref NetworkPipelineContext ctx, ref InboundRecvBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var receiveData = (int*)ctx.internalProcessBuffer;
            for (int i = 4; i <= 6; ++i)
            {
                Assert.AreEqual(*receiveData, i*10);
                receiveData++;
            }
            var sharedData = (int*)ctx.internalSharedProcessBuffer;
            for (int i = 7; i <= 8; ++i)
            {
                Assert.AreEqual(*sharedData, i*10);
                sharedData++;
            }
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.SendDelegate))]
        private static int Send(ref NetworkPipelineContext ctx, ref InboundSendBuffer inboundBuffer, ref NetworkPipelineStage.Requests request)
        {
            var sendData = (int*)ctx.internalProcessBuffer;
            for (int i = 1; i <= 3; ++i)
            {
                Assert.AreEqual(*sendData, i*10);
                sendData++;
            }
            var sharedData = (int*)ctx.internalSharedProcessBuffer;
            for (int i = 7; i <= 8; ++i)
            {
                Assert.AreEqual(*sharedData, i*10);
                sharedData++;
            }
            return (int)Error.StatusCode.Success;
        }

        [BurstCompile(DisableDirectCall = true)]
        [MonoPInvokeCallback(typeof(NetworkPipelineStage.InitializeConnectionDelegate))]
        private static void InitializeConnection(byte* staticInstanceBuffer, int staticInstanceBufferLength,
            byte* sendProcessBuffer, int sendProcessBufferLength, byte* recvProcessBuffer, int recvProcessBufferLength,
            byte* sharedProcessBuffer, int sharedProcessBufferLength)
        {
            var sendData = (int*)sendProcessBuffer;
            *sendData = 10;
            sendData++;
            *sendData = 20;
            sendData++;
            *sendData = 30;
            var receiveData = (int*)recvProcessBuffer;
            *receiveData = 40;
            receiveData++;
            *receiveData = 50;
            receiveData++;
            *receiveData = 60;
            var sharedData = (int*) sharedProcessBuffer;
            *sharedData = 70;
            sharedData++;
            *sharedData = 80;
            sharedData++;
            *sharedData = 90;
        }
    }

    public struct TestNetworkPipelineStageCollection
    {
        public static void Register()
        {
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestPipelineStageWithHeader());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestPipelineStageWithHeaderTwo());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestEncryptPipelineStage());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestEncryptInPlacePipelineStage());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestInvertPipelineStage());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestPipelineWithInitializers());
            NetworkPipelineStageCollection.RegisterPipelineStage(new TestPipelineWithInitializersTwo());
        }
    }

    public class NetworkPipelineTest
    {
        private NetworkDriver m_ServerDriver;
        private NetworkDriver m_ClientDriver;
        private NetworkDriver m_ClientDriver2;

        [SetUp]
        public void IPC_Setup()
        {
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                fixedFrameTimeMS = 16
            };
            // NOTE: MaxPacketSize should be 64 for all the tests using simulator except needs to account for header size as well (one test has 2x2B headers)
            var simulatorParams = new SimulatorUtility.Parameters()
                {MaxPacketSize = 72, MaxPacketCount = 30, PacketDelayMs = 100};
            TestNetworkPipelineStageCollection.Register();
            m_ServerDriver = TestNetworkDriver.Create(timeoutParam, simulatorParams);
            m_ServerDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            m_ServerDriver.Listen();
            m_ClientDriver = TestNetworkDriver.Create(timeoutParam, simulatorParams);
            m_ClientDriver2 = TestNetworkDriver.Create(timeoutParam, simulatorParams);
        }

        [TearDown]
        public void IPC_TearDown()
        {
            m_ClientDriver.Dispose();
            m_ClientDriver2.Dispose();
            m_ServerDriver.Dispose();
        }
        [Test]
        public void NetworkPipeline_CreatePipelineIsSymetrical()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeader));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestPipelineStageWithHeader));
            Assert.AreEqual(clientPipe, serverPipe);
        }
        [Test]
        public void NetworkPipeline_CreatePipelineAfterConnectFails()
        {
            m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            Assert.Throws<InvalidOperationException>(() => { m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeader)); });
        }
        [Test]
        public void NetworkPipeline_CreatePipelineWithInvalidStageFails()
        {
            Assert.Throws<InvalidOperationException>(() => { m_ClientDriver.CreatePipeline(typeof(NetworkPipelineTest)); });
        }

        [Test]
        public void NetworkPipeline_CanExtendHeader()
        {
            // Create pipeline
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeader));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestPipelineStageWithHeader));
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
        public void NetworkPipeline_CanModifyAndRestoreData()
        {
            // Create pipeline
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TestEncryptPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestEncryptPipelineStage));
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
        public void NetworkPipeline_CanModifyAndRestoreDataInPlace()
        {
            // Create pipeline
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TestEncryptInPlacePipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestEncryptInPlacePipelineStage));
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
        public void NetworkPipeline_CanModifyData()
        {
            // Create pipeline
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TestInvertPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestInvertPipelineStage));
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
            Assert.AreEqual(-1^42, readStrm.ReadInt());
        }

        [Test]
        public void NetworkPipeline_MultiplePipelinesWork()
        {
            var clientPipe = m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeaderTwo), typeof(TestEncryptPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestPipelineStageWithHeaderTwo), typeof(TestEncryptPipelineStage));
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
        public void NetworkPipeline_CanStorePacketsForLaterDeliveryInReceiveLastStage()
        {
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(TestEncryptPipelineStage), typeof(SimulatorPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(TestEncryptPipelineStage), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestEncryptPipelineStage), typeof(SimulatorPipelineStage));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStorePacketsForLaterDeliveryInReceiveFirstStage()
        {
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(SimulatorPipelineStage), typeof(TestEncryptPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(SimulatorPipelineStage), typeof(TestEncryptPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(SimulatorPipelineStage), typeof(TestEncryptPipelineStage));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStorePacketsForLaterDeliveryInSendLastStage()
        {
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(TestEncryptPipelineStage), typeof(SimulatorPipelineStageInSend));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(TestEncryptPipelineStage), typeof(SimulatorPipelineStageInSend));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestEncryptPipelineStage), typeof(SimulatorPipelineStageInSend));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStorePacketsForLaterDeliveryInSendFirstStage()
        {
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(SimulatorPipelineStageInSend), typeof(TestEncryptPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(SimulatorPipelineStageInSend), typeof(TestEncryptPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(SimulatorPipelineStageInSend), typeof(TestEncryptPipelineStage));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStoreSequencedPacketsForLaterDeliveryInSendLastStage()
        {
            // Server needs the simulator as it's the only one sending
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(SimulatorPipelineStageInSend));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStoreSequencedPacketsForLaterDeliveryInSendFirstStage()
        {
            // Server needs the simulator as it's the only one sending
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(SimulatorPipelineStageInSend), typeof(UnreliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStoreSequencedPacketsForLaterDeliveryInReceiveLastStage()
        {
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(UnreliableSequencedPipelineStage), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_CanStoreSequencedPacketsForLaterDeliveryInReceiveFirstStage()
        {
            var clientPipe1 = m_ClientDriver.CreatePipeline(typeof(SimulatorPipelineStage), typeof(UnreliableSequencedPipelineStage));
            var clientPipe2 = m_ClientDriver2.CreatePipeline(typeof(SimulatorPipelineStage), typeof(UnreliableSequencedPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(UnreliableSequencedPipelineStage));
            Assert.AreEqual(clientPipe1, serverPipe);
            Assert.AreEqual(clientPipe2, serverPipe);

            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_MultiplePipelinesWithHeadersWork()
        {
            m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            m_ClientDriver2.CreatePipeline(typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            TestPipeline(30, serverPipe, 0);
        }

        [Test]
        public void NetworkPipeline_MultiplePipelinesWithHeadersWorkWithSimulator()
        {
            //m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeader), typeof(SimulatorPipelineStage), typeof(TestPipelineStageWithHeaderTwo));
            //m_ClientDriver2.CreatePipeline(typeof(SimulatorPipelineStage), typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            m_ClientDriver.CreatePipeline(typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            m_ClientDriver2.CreatePipeline(typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(SimulatorPipelineStageInSend), typeof(TestPipelineStageWithHeader), typeof(TestPipelineStageWithHeaderTwo));
            TestPipeline(30, serverPipe);
        }

        [Test]
        public void NetworkPipeline_MuliplePipelinesWithInitializers()
        {
            m_ClientDriver.CreatePipeline(typeof(TestPipelineWithInitializers), typeof(TestPipelineWithInitializersTwo));
            m_ClientDriver2.CreatePipeline(typeof(TestPipelineWithInitializers), typeof(TestPipelineWithInitializersTwo));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(TestPipelineWithInitializers), typeof(TestPipelineWithInitializersTwo));
            TestPipeline(30, serverPipe, 0);
        }

        [Test]
        public void NetworkPipeline_MuliplePipelinesWithInitializersAndSimulator()
        {
            m_ClientDriver.CreatePipeline(typeof(TestPipelineWithInitializers), typeof(SimulatorPipelineStage), typeof(TestPipelineWithInitializersTwo));
            m_ClientDriver2.CreatePipeline(typeof(TestPipelineWithInitializers), typeof(TestPipelineWithInitializersTwo), typeof(SimulatorPipelineStage));
            var serverPipe = m_ServerDriver.CreatePipeline(typeof(SimulatorPipelineStageInSend), typeof(TestPipelineWithInitializers), typeof(TestPipelineWithInitializersTwo));
            TestPipeline(30, serverPipe);
        }

        private void TestPipeline(int packetCount, NetworkPipeline serverPipe, int packetDelay = 100)
        {
            // Connect to server
            var clientToServer = m_ClientDriver.Connect(m_ServerDriver.LocalEndPoint());
            var clientToServer2 = m_ClientDriver2.Connect(m_ServerDriver.LocalEndPoint());
            Assert.AreNotEqual(default(NetworkConnection), clientToServer);
            Assert.AreNotEqual(default(NetworkConnection), clientToServer2);
            m_ClientDriver.ScheduleUpdate().Complete();
            m_ClientDriver2.ScheduleUpdate().Complete();

            // Driver only updates time in update, so must read start time before update
            var startTime = m_ServerDriver.LastUpdateTime;
            // Handle incoming connection from client
            m_ServerDriver.ScheduleUpdate().Complete();
            var serverToClient = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient);
            var serverToClient2 = m_ServerDriver.Accept();
            Assert.AreNotEqual(default(NetworkConnection), serverToClient2);

            // Send given packetCount number of packets in a row in one update
            // Write 1's for packet 1, 2's for packet 2 and so on and verify they're received in same order
            for (int i = 0; i < packetCount; i++)
            {
                if (m_ServerDriver.BeginSend(serverPipe, serverToClient, out var strm) == 0 &&
                    m_ServerDriver.BeginSend(serverPipe, serverToClient2, out var strm2) == 0)
                {
                    for (int j = 0; j < 16; j++)
                    {
                        strm.WriteInt((int) i + 1);
                        strm2.WriteInt((int) i + 1);
                    }
                    m_ServerDriver.EndSend(strm);
                    m_ServerDriver.EndSend(strm2);
                }
            }

            m_ServerDriver.ScheduleUpdate().Complete();

            // Receive incoming message from server
            m_ClientDriver.ScheduleUpdate().Complete();
            m_ClientDriver2.ScheduleUpdate().Complete();
            DataStreamReader readStrm;
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver, out readStrm));
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(m_ClientDriver2, out readStrm));

            ClientReceivePackets(m_ClientDriver, packetCount, clientToServer, startTime, packetDelay);
            ClientReceivePackets(m_ClientDriver2, packetCount, clientToServer2, startTime, packetDelay);
        }

        private void ClientReceivePackets(NetworkDriver clientDriver, int packetCount, NetworkConnection clientToServer, long startTime, int minDelay)
        {
            DataStreamReader readStrm;
            NetworkEvent.Type netEvent;
            var abortFrame = 0;
            while (true)
            {
                if (abortFrame++ > 125)
                    Assert.Fail("Did not receive first delayed packet");
                netEvent = clientToServer.PopEvent(clientDriver, out readStrm);
                if (netEvent == NetworkEvent.Type.Data)
                    break;
                m_ServerDriver.ScheduleUpdate().Complete();
                clientDriver.ScheduleUpdate().Complete();
            }

            // All delayed packets (from first patch) should be poppable now
            for (int i = 0; i < packetCount; i++)
            {
                var delay = m_ServerDriver.LastUpdateTime - startTime;
                Assert.AreEqual(NetworkEvent.Type.Data, netEvent);
                Assert.GreaterOrEqual(delay, minDelay, $"Delay too low on packet {i}");
                Assert.AreEqual(64, readStrm.Length);
                for (int j = 0; j < 16; j++)
                {
                    var read = readStrm.ReadInt();
                    Assert.AreEqual(i + 1, read);
                    Assert.True(read > 0 && read <= packetCount, "read incorrect value: " + read);
                }

                // Test done when all packets have been verified
                if (i == packetCount - 1)
                    break;

                // It could be not all patch of packets were processed in one update (depending on how the timers land)
                abortFrame = 0;
                while ((netEvent = clientToServer.PopEvent(clientDriver, out readStrm)) == NetworkEvent.Type.Empty)
                {
                    if (abortFrame++ > 75)
                        Assert.Fail("Didn't receive all delayed packets");
                    clientDriver.ScheduleUpdate().Complete();
                    m_ServerDriver.ScheduleUpdate().Complete();
                }
            }
        }
    }
}
