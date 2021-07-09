using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Relay;

namespace Unity.Networking.Transport.Tests
{
    public class RelayNetworkDriverTests
    {
        private ushort m_port = 1234;

        [Test]
        public void RelayCheckStructSizes()
        {
            Assert.AreEqual(RelayMessageHeader.Length, UnsafeUtility.SizeOf<RelayMessageHeader>());
            Assert.AreEqual(RelayAllocationId.k_Length, UnsafeUtility.SizeOf<RelayAllocationId>());
            
            Assert.AreEqual(RelayMessageAccepted.Length, UnsafeUtility.SizeOf<RelayMessageAccepted>());
            Assert.AreEqual(RelayMessageConnectRequest.Length, UnsafeUtility.SizeOf<RelayMessageConnectRequest>());
            Assert.AreEqual(RelayMessageDisconnect.Length, UnsafeUtility.SizeOf<RelayMessageDisconnect>());
            Assert.AreEqual(RelayMessagePing.Length, UnsafeUtility.SizeOf<RelayMessagePing>());
            Assert.AreEqual(RelayMessageRelay.Length, UnsafeUtility.SizeOf<RelayMessageRelay>());
            Assert.AreEqual(RelayConnectionData.k_Length, UnsafeUtility.SizeOf<RelayConnectionData>());
            Assert.AreEqual(RelayHMACKey.k_Length, UnsafeUtility.SizeOf<RelayHMACKey>());
        }

        [Test]
        public void RelayNetworkDriver_Bind_Succeed()
        {
           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var driver = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0), RelayConnectionTimeMS = 10000000 }))
           {
               server.SetupForBind();

               Assert.Zero(driver.Bind(NetworkEndPoint.AnyIpv4));
               driver.ScheduleUpdate(default).Complete();

               RelayServerMock.WaitForCondition(() =>
               {
                   driver.ScheduleUpdate().Complete();
                   return driver.Bound;
               });

               Assert.IsTrue(driver.Bound);
           }
        }

        [Test]
        public void RelayNetworkDriver_Bind_Retry()
        {
           const int k_RetryCount = 10;

           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var driver = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(),
               new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0)},
               new NetworkConfigParameter
               {
                   maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                   connectTimeoutMS = 50,
                   disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                   maxFrameTimeMS = 0
               }))
           {
               var retriesLeft = k_RetryCount;
               server.SetupForBindRetry(k_RetryCount, () => --retriesLeft);

               Assert.Zero(driver.Bind(NetworkEndPoint.AnyIpv4));
               driver.ScheduleFlushSend(default).Complete();

               RelayServerMock.WaitForCondition(() =>
               {
                   driver.ScheduleUpdate().Complete();
                   return driver.Bound;
               });

               Assert.IsTrue(retriesLeft <= 0);
               Assert.IsTrue(driver.Bound);
           }
        }

        [Test]
        public void RelayNetworkDriver_Listen_Succeed()
        {
           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var host = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0), RelayConnectionTimeMS = 10000000 }))
           {
               host.Bind(NetworkEndPoint.AnyIpv4);
               Assert.True(server.CompleteBind(host));
               Assert.Zero(host.Listen());
           }
        }

        [Test]
        public void RelayNetworkDriver_ListenWithUnbound_Fail()
        {
           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var host = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0), RelayConnectionTimeMS = 10000000 }))
           {
               host.Bind(NetworkEndPoint.AnyIpv4);
               Assert.IsFalse(host.Bound);
               Assert.Throws<System.InvalidOperationException>(() =>
               {
                   host.Listen();
               });
           }
        }

        [Test]
        public void RelayNetworkDriver_Connect_Succeed()
        {
           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var host = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0), RelayConnectionTimeMS = 10000000 }))
           using (var client = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(1), RelayConnectionTimeMS = 10000000 }))
           {
               host.Bind(NetworkEndPoint.AnyIpv4);
               Assert.True(server.CompleteBind(host, 0));

               client.Bind(NetworkEndPoint.AnyIpv4);
               Assert.True(server.CompleteBind(client, 1));

               Assert.Zero(host.Listen());

               server.SetupForConnect(1);

               var clientToHost = client.Connect(server.GetRelayConnectionData(0).Endpoint);

               Assert.AreNotEqual(default(NetworkConnection), clientToHost);

               RelayServerMock.WaitForCondition(() =>
               {
                   client.ScheduleUpdate(default).Complete();
                   host.ScheduleUpdate(default).Complete();

                   return client.GetConnectionState(clientToHost) == NetworkConnection.State.Connected;
               });

               Assert.AreEqual(NetworkConnection.State.Connected, client.GetConnectionState(clientToHost));

               var evt = client.PopEvent(out var clientToHostConnected, out var reader);
               Assert.AreEqual(NetworkEvent.Type.Connect, evt);
               Assert.AreEqual(clientToHost, clientToHostConnected);

               Assert.AreEqual(NetworkEvent.Type.Empty, host.PopEvent(out clientToHostConnected, out reader));

               var hostToClient = host.Accept();
               Assert.AreNotEqual(default(NetworkConnection), hostToClient);
               Assert.AreEqual(NetworkConnection.State.Connected, host.GetConnectionState(hostToClient));
           }
        }

        [Test]
        public void RelayNetworkDriver_Connect_Retry()
        {
           const int k_RetryCount = 10;

           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var host = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0), RelayConnectionTimeMS = 10000000 }))
           using (var client = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(),
               new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(1)},
               new NetworkConfigParameter
               {
                   maxConnectAttempts = NetworkParameterConstants.MaxConnectAttempts,
                   connectTimeoutMS = 50,
                   disconnectTimeoutMS = NetworkParameterConstants.DisconnectTimeoutMS,
                   maxFrameTimeMS = 0
               }))
           {
               host.Bind(NetworkEndPoint.AnyIpv4);
               Assert.True(server.CompleteBind(host, 0));

               client.Bind(NetworkEndPoint.AnyIpv4);
               Assert.True(server.CompleteBind(client, 1));

               Assert.Zero(host.Listen());

               var retriesLeft = k_RetryCount;
               server.SetupForConnectRetry(1, k_RetryCount, () => --retriesLeft);

               var clientToHost = client.Connect(server.GetRelayConnectionData(0).Endpoint);

               Assert.AreNotEqual(default(NetworkConnection), clientToHost);

               RelayServerMock.WaitForCondition(() =>
               {
                   client.ScheduleUpdate(default).Complete();
                   host.ScheduleUpdate(default).Complete();

                   return client.GetConnectionState(clientToHost) == NetworkConnection.State.Connected;
               });

               Assert.LessOrEqual(0, retriesLeft);

               Assert.AreEqual(NetworkConnection.State.Connected, client.GetConnectionState(clientToHost));

               var evt = client.PopEvent(out var clientToHostConnected, out var reader);
               Assert.AreEqual(NetworkEvent.Type.Connect, evt);
               Assert.AreEqual(clientToHost, clientToHostConnected);

               Assert.AreEqual(NetworkEvent.Type.Empty, host.PopEvent(out clientToHostConnected, out reader));

               var hostToClient = host.Accept();
               Assert.AreNotEqual(default(NetworkConnection), hostToClient);
               Assert.AreEqual(NetworkConnection.State.Connected, host.GetConnectionState(hostToClient));
           }
        }

        [Test]
        public void RelayNetworkDriver_Disconnect_Succeed()
        {
           //using (var server = new RelayServerMock("127.0.0.1", m_port++))
           //using (var host = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(),new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0) }))
           //using (var client = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(),new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(1) })) {
           //    Assert.True(server.CompleteConnect(host, out var connections, client));
           //    var connection = connections[0];

           //    server.SetupForDisconnect(1, 0);

           //    Assert.AreEqual(NetworkConnection.State.Connected, client.GetConnectionState(connections[0].clientToHost));
           //    Assert.AreEqual(NetworkConnection.State.Connected, host.GetConnectionState(connections[0].hostToClient));

           //    Assert.Zero(client.Disconnect(connections[0].clientToHost));

           //    RelayServerMock.WaitForCondition(() =>
           //    {
           //        client.ScheduleUpdate(default).Complete();
           //        host.ScheduleUpdate(default).Complete();

           //        return host.GetConnectionState(connections[0].hostToClient) == NetworkConnection.State.Disconnected;
           //    });

           //    Assert.AreEqual(NetworkConnection.State.Disconnected, client.GetConnectionState(connections[0].clientToHost));
           //    Assert.AreEqual(NetworkConnection.State.Disconnected, host.GetConnectionState(connections[0].hostToClient));
           //}
        }

        [Test]
        public void RelayNetworkDriver_Send_Succeed()
        {
           const int k_PayloadSize = 100;

           using (var server = new RelayServerMock("127.0.0.1", m_port++))
           using (var host = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(0), RelayConnectionTimeMS = 10000000 }))
           using (var client = new NetworkDriver(new BaselibNetworkInterface(), new RelayNetworkProtocol(), new RelayNetworkParameter { ServerData = server.GetRelayConnectionData(1), RelayConnectionTimeMS = 10000000 }))
           {
               Assert.True(server.CompleteConnect(host, out var connections, client));

               var connection = connections[0];

               server.SetupForRelay(1, 0, k_PayloadSize + UdpCHeader.Length);

               Assert.Zero(client.BeginSend(connection.clientToHost, out var writer, k_PayloadSize));
               for (int i = 0; i < k_PayloadSize; i++)
               {
                   writer.WriteByte((byte)i);
               }
               Assert.AreEqual(k_PayloadSize, client.EndSend(writer));

               client.ScheduleFlushSend(default).Complete();

               RelayServerMock.WaitForCondition(() =>
               {
                   host.ScheduleUpdate(default).Complete();
                   return host.GetEventQueueSizeForConnection(connection.hostToClient) > 0;
               });

               Assert.AreEqual(NetworkEvent.Type.Data, host.PopEventForConnection(connection.hostToClient, out var reader));

               for (int i = 0; i < k_PayloadSize; i++)
               {
                   Assert.AreEqual((byte)i, reader.ReadByte());
               }
           }
        }
    }
}
