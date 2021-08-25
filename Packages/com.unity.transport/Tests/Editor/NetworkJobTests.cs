using System;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;

namespace Unity.Networking.Transport.Tests
{
    public class NetworkJobTests
    {
        void WaitForConnected(NetworkDriver clientDriver, NetworkDriver serverDriver,
            NetworkConnection clientToServer)
        {
            // Make sure connect message is sent
            clientDriver.ScheduleFlushSend(default).Complete();
            // Make sure connection accept message is sent back
            serverDriver.ScheduleUpdate().Complete();
            // Handle the connection accept message
            clientDriver.ScheduleUpdate().Complete();
            DataStreamReader strmReader;
            // Make sure the connected message was received
            Assert.AreEqual(NetworkEvent.Type.Connect, clientToServer.PopEvent(clientDriver, out strmReader));
        }

        [Test]
        public void ScheduleUpdateWorks()
        {
            var driver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            var updateHandle = driver.ScheduleUpdate();
            updateHandle.Complete();
            driver.Dispose();
        }
        [Test]
        public void ScheduleUpdateWithMissingDependencyThrowsException()
        {
            var driver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            var updateHandle = driver.ScheduleUpdate();
            Assert.Throws<InvalidOperationException>(() => { driver.ScheduleUpdate().Complete(); });
            updateHandle.Complete();
            driver.Dispose();
        }

        [Test]
        public void DataStremReaderIsOnlyUsableUntilUpdate()
        {
            var serverDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            serverDriver.Listen();
            var clientDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            var clientToServer = clientDriver.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver, serverDriver, clientToServer);

            if (clientDriver.BeginSend(clientToServer, out var strmWriter) == 0)
            {
                strmWriter.WriteInt(42);
                clientDriver.EndSend(strmWriter);
            }
            clientDriver.ScheduleUpdate().Complete();
            var serverToClient = serverDriver.Accept();
            serverDriver.ScheduleUpdate().Complete();
            DataStreamReader strmReader;
            Assert.AreEqual(NetworkEvent.Type.Data, serverToClient.PopEvent(serverDriver, out strmReader));
            var ctx = strmReader;
            Assert.AreEqual(42, strmReader.ReadInt());
            strmReader = ctx;
            Assert.AreEqual(42, strmReader.ReadInt());
            serverDriver.ScheduleUpdate().Complete();
            strmReader = ctx;
            Assert.Catch(() => { strmReader.ReadInt(); });
            clientDriver.Dispose();
            serverDriver.Dispose();
        }

        struct AcceptJob : IJob
        {
            public NetworkDriver driver;
            public NativeArray<NetworkConnection> connections;
            public void Execute()
            {
                for (int i = 0; i < connections.Length; ++i)
                    connections[i] = driver.Accept();
            }
        }
        [Test]
        public void AcceptInJobWorks()
        {
            var serverDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            serverDriver.Listen();
            var clientDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            /*var clientToServer =*/ clientDriver.Connect(serverDriver.LocalEndPoint());
            clientDriver.ScheduleUpdate().Complete();

            var serverToClient = new NativeArray<NetworkConnection>(1, Allocator.TempJob);
            var acceptJob = new AcceptJob {driver = serverDriver, connections = serverToClient};
            Assert.IsFalse(serverToClient[0].IsCreated);
            acceptJob.Schedule(serverDriver.ScheduleUpdate()).Complete();
            Assert.IsTrue(serverToClient[0].IsCreated);

            serverToClient.Dispose();
            clientDriver.Dispose();
            serverDriver.Dispose();
        }
        struct ReceiveJob : IJob
        {
            public NetworkDriver driver;
            public NativeArray<NetworkConnection> connections;
            public NativeArray<int> result;
            public void Execute()
            {
                DataStreamReader strmReader;
                // Data
                connections[0].PopEvent(driver, out strmReader);
                result[0] = strmReader.ReadInt();
            }
        }
        [Test]
        public void ReceiveInJobWorks()
        {
            var serverDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            serverDriver.Listen();
            var clientDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            var clientToServer = clientDriver.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver, serverDriver, clientToServer);

            if (clientDriver.BeginSend(clientToServer, out var strmWriter) == 0)
            {
                strmWriter.WriteInt(42);
                clientDriver.EndSend(strmWriter);
            }
            clientDriver.ScheduleUpdate().Complete();

            var serverToClient = new NativeArray<NetworkConnection>(1, Allocator.TempJob);
            var result = new NativeArray<int>(1, Allocator.TempJob);
            var recvJob = new ReceiveJob {driver = serverDriver, connections = serverToClient, result = result};
            Assert.AreNotEqual(42, result[0]);
            var acceptJob = new AcceptJob {driver = serverDriver, connections = serverToClient};
            recvJob.Schedule(serverDriver.ScheduleUpdate(acceptJob.Schedule())).Complete();
            Assert.AreEqual(42, result[0]);

            result.Dispose();
            serverToClient.Dispose();
            clientDriver.Dispose();
            serverDriver.Dispose();
        }

        struct SendJob : IJob
        {
            public NetworkDriver driver;
            public NetworkConnection connection;
            public void Execute()
            {
                if (driver.BeginSend(connection, out var strmWriter) == 0)
                {
                    strmWriter.WriteInt(42);
                    driver.EndSend(strmWriter);
                }
            }
        }
        [Test]
        public void SendInJobWorks()
        {
            var serverDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            serverDriver.Listen();
            var clientDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            var clientToServer = clientDriver.Connect(serverDriver.LocalEndPoint());
            WaitForConnected(clientDriver, serverDriver, clientToServer);
            var sendJob = new SendJob {driver = clientDriver, connection = clientToServer};
            clientDriver.ScheduleUpdate(sendJob.Schedule()).Complete();
            var serverToClient = serverDriver.Accept();
            serverDriver.ScheduleUpdate().Complete();
            DataStreamReader strmReader;
            Assert.AreEqual(NetworkEvent.Type.Data, serverToClient.PopEvent(serverDriver, out strmReader));
            Assert.AreEqual(42, strmReader.ReadInt());
            clientDriver.Dispose();
            serverDriver.Dispose();
        }
        struct SendReceiveParallelJob : IJobParallelFor
        {
            public NetworkDriver.Concurrent driver;
            public NativeArray<NetworkConnection> connections;
            public void Execute(int i)
            {
                DataStreamReader strmReader;
                // Data
                if (driver.PopEventForConnection(connections[i], out strmReader) != NetworkEvent.Type.Data)
                    throw new InvalidOperationException("Expected data: " + i);
                int result = strmReader.ReadInt();
                if (driver.BeginSend(connections[i], out var strmWriter) == 0)
                {
                    strmWriter.WriteInt(result + 1);
                    driver.EndSend(strmWriter);
                }
            }
        }
        [Test]
        public void SendReceiveInParallelJobWorks()
        {
            NativeArray<NetworkConnection> serverToClient;
            using (var serverDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}))
            using (var clientDriver0 = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}))
            using (var clientDriver1 = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}))
            using (serverToClient = new NativeArray<NetworkConnection>(2, Allocator.Persistent))
            {
                serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
                serverDriver.Listen();
                var clientToServer0 = clientDriver0.Connect(serverDriver.LocalEndPoint());
                var clientToServer1 = clientDriver1.Connect(serverDriver.LocalEndPoint());
                WaitForConnected(clientDriver0, serverDriver, clientToServer0);

                if (clientDriver0.BeginSend(clientToServer0, out var strmWriter) == 0)
                {
                    strmWriter.WriteInt(42);
                    serverToClient[0] = serverDriver.Accept();
                    Assert.IsTrue(serverToClient[0].IsCreated);
                    WaitForConnected(clientDriver1, serverDriver, clientToServer1);
                    serverToClient[1] = serverDriver.Accept();
                    Assert.IsTrue(serverToClient[1].IsCreated);

                    clientDriver0.EndSend(strmWriter);
                }

                if (clientDriver1.BeginSend(clientToServer1, out var strmWriter2) == 0)
                {
                    strmWriter2.WriteBytes(strmWriter.AsNativeArray());
                    clientDriver1.EndSend(strmWriter2);
                }

                clientDriver0.ScheduleUpdate().Complete();
                clientDriver1.ScheduleUpdate().Complete();

                var sendRecvJob = new SendReceiveParallelJob {driver = serverDriver.ToConcurrent(), connections = serverToClient};
                var jobHandle = serverDriver.ScheduleUpdate();
                jobHandle = sendRecvJob.Schedule(serverToClient.Length, 1, jobHandle);
                serverDriver.ScheduleUpdate(jobHandle).Complete();

                AssertDataReceived(serverDriver, serverToClient, clientDriver0, clientToServer0, 43, true);
                AssertDataReceived(serverDriver, serverToClient, clientDriver1, clientToServer1, 43, true);
            }
        }
        [BurstCompile/*(CompileSynchronously = true)*/] // FIXME: sync compilation makes tests timeout
        struct SendReceiveWithPipelineParallelJob : IJobParallelFor
        {
            public NetworkDriver.Concurrent driver;
            public NativeArray<NetworkConnection> connections;
            public NetworkPipeline pipeline;
            public void Execute(int i)
            {
                DataStreamReader strmReader;
                // Data
                if (driver.PopEventForConnection(connections[i], out strmReader) != NetworkEvent.Type.Data)
                    throw new InvalidOperationException("Expected data: " + i);
                int result = strmReader.ReadInt();
                if (driver.BeginSend(connections[i], out var strmWriter) == 0)
                {
                    strmWriter.WriteInt(result + 1);
                    driver.EndSend(strmWriter);
                }
            }
        }
        [Test]
        public void SendReceiveWithPipelineInParallelJobWorks()
        {
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = 90 * 1000,
                maxFrameTimeMS = 16
            };
            NativeArray<NetworkConnection> serverToClient;
            using (var serverDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}, timeoutParam))
            using (var clientDriver0 = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}, timeoutParam))
            using (var clientDriver1 = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}, timeoutParam))
            using (serverToClient = new NativeArray<NetworkConnection>(2, Allocator.Persistent))
            {
                var serverPipeline = serverDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
                serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
                serverDriver.Listen();
                var client0Pipeline = clientDriver0.CreatePipeline(typeof(ReliableSequencedPipelineStage));
                var client1Pipeline = clientDriver1.CreatePipeline(typeof(ReliableSequencedPipelineStage));
                var clientToServer0 = clientDriver0.Connect(serverDriver.LocalEndPoint());
                var clientToServer1 = clientDriver1.Connect(serverDriver.LocalEndPoint());
                WaitForConnected(clientDriver0, serverDriver, clientToServer0);
                serverToClient[0] = serverDriver.Accept();
                Assert.IsTrue(serverToClient[0].IsCreated);
                WaitForConnected(clientDriver1, serverDriver, clientToServer1);
                serverToClient[1] = serverDriver.Accept();
                Assert.IsTrue(serverToClient[1].IsCreated);

                if (clientDriver0.BeginSend(clientToServer0, out var strmWriter0) == 0)
                {
                    strmWriter0.WriteInt(42);
                    clientDriver0.EndSend(strmWriter0);
                }
                if (clientDriver1.BeginSend(clientToServer1, out var strmWriter1) == 0)
                {
                    strmWriter1.WriteInt(42);
                    clientDriver1.EndSend(strmWriter1);
                }

                clientDriver0.ScheduleUpdate().Complete();
                clientDriver1.ScheduleUpdate().Complete();

                var sendRecvJob = new SendReceiveWithPipelineParallelJob
                    {driver = serverDriver.ToConcurrent(), connections = serverToClient, pipeline = serverPipeline};
                var jobHandle = serverDriver.ScheduleUpdate();
                jobHandle = sendRecvJob.Schedule(serverToClient.Length, 1, jobHandle);
                serverDriver.ScheduleUpdate(jobHandle).Complete();

                AssertDataReceived(serverDriver, serverToClient, clientDriver0, clientToServer0, 43, false);
                AssertDataReceived(serverDriver, serverToClient, clientDriver1, clientToServer1, 43, false);
            }
        }

        [Test]
        public void ParallelSendReceiveStressTest()
        {
            var timeoutParam = new NetworkConfigParameter
            {
                connectTimeoutMS = NetworkParameterConstants.ConnectTimeoutMS,
                maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                disconnectTimeoutMS = 90 * 1000,
                maxFrameTimeMS = 16
            };
            NativeArray<NetworkConnection> serverToClient;
            var clientDrivers = new List<NetworkDriver>();
            var clientPipelines = new List<NetworkPipeline>();
            var clientToServer = new List<NetworkConnection>();
            try
            {
                for (int i = 0; i < 250; ++i)
                {
                    clientDrivers.Add(TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64}, timeoutParam));
                    clientPipelines.Add(clientDrivers[i].CreatePipeline(typeof(ReliableSequencedPipelineStage)));
                }
                using (var serverDriver = TestNetworkDriver.Create(new BaselibNetworkParameter {maximumPayloadSize = 64, receiveQueueCapacity = clientDrivers.Count, sendQueueCapacity = clientDrivers.Count }, timeoutParam))
                using (serverToClient = new NativeArray<NetworkConnection>(clientDrivers.Count, Allocator.Persistent))
                {
                    var serverPipeline = serverDriver.CreatePipeline(typeof(ReliableSequencedPipelineStage));
                    serverDriver.Bind(NetworkEndPoint.LoopbackIpv4);
                    serverDriver.Listen();
                    for (var i = 0; i < clientDrivers.Count; ++i)
                    {
                        var drv = clientDrivers[i];
                        var con = drv.Connect(serverDriver.LocalEndPoint());
                        WaitForConnected(drv, serverDriver, con);
                        clientToServer.Add(con);
                        serverToClient[i] = serverDriver.Accept();
                        Assert.IsTrue(serverToClient[i].IsCreated);
                    }
                    for (var i = 0; i < clientDrivers.Count; ++i)
                    {

                        if (clientDrivers[i].BeginSend(clientPipelines[i], clientToServer[i], out var strmWriter) == 0)
                        {
                            strmWriter.WriteInt(42);
                            clientDrivers[i].EndSend(strmWriter);
                        }
                        clientDrivers[i].ScheduleFlushSend(default).Complete();
                    }

                    var sendRecvJob = new SendReceiveWithPipelineParallelJob
                        {driver = serverDriver.ToConcurrent(), connections = serverToClient, pipeline = serverPipeline};
                    var jobHandle = serverDriver.ScheduleUpdate();
                    jobHandle = sendRecvJob.Schedule(serverToClient.Length, 1, jobHandle);
                    serverDriver.ScheduleUpdate(jobHandle).Complete();

                    for (var i = 0; i < clientDrivers.Count; ++i)
                        AssertDataReceived(serverDriver, serverToClient, clientDrivers[i], clientToServer[i], 43, false);
                }
            }
            finally
            {
                foreach (var drv in clientDrivers)
                    drv.Dispose();
            }
        }
        void AssertDataReceived(NetworkDriver serverDriver, NativeArray<NetworkConnection> serverConnections, NetworkDriver clientDriver, NetworkConnection clientToServerConnection, int assertValue, bool serverResend)
        {
            DataStreamReader strmReader;
            clientDriver.ScheduleUpdate().Complete();
            var evnt = clientToServerConnection.PopEvent(clientDriver, out strmReader);
            int counter = 0;
            while (evnt == NetworkEvent.Type.Empty)
            {
                serverDriver.ScheduleUpdate().Complete();
                clientDriver.ScheduleUpdate().Complete();
                evnt = clientToServerConnection.PopEvent(clientDriver, out strmReader);
                if (counter++ > 1000)
                {
                    if (!serverResend)
                        break;
                    counter = 0;
                    for (int i = 0; i < serverConnections.Length; ++i)
                    {

                        if (serverDriver.BeginSend(serverConnections[i], out var strmWriter) == 0)
                        {
                            strmWriter.WriteInt(42);
                            serverDriver.EndSend(strmWriter);
                        }
                    }
                }
            }
            Assert.AreEqual(NetworkEvent.Type.Data, evnt);
            Assert.AreEqual(assertValue, strmReader.ReadInt());
        }
    }
}
