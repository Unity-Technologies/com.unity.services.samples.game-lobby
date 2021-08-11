using NUnit.Framework;
using Unity.Collections;

namespace Unity.Networking.Transport.Tests
{
    public static class SharedConstants
    {
        public static byte[] ping =
        {
            (byte) 'p',
            (byte) 'i',
            (byte) 'n',
            (byte) 'g'
        };

        public static byte[] pong =
        {
            (byte) 'p',
            (byte) 'o',
            (byte) 'n',
            (byte) 'g'
        };
    }

    public class NetworkConnectionUnitTests
    {
        private NetworkDriver Driver;
        private NetworkDriver RemoteDriver;

        [SetUp]
        public void IPC_Setup()
        {
            Driver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});
            RemoteDriver = TestNetworkDriver.Create(new NetworkDataStreamParameter {size = 64});

            RemoteDriver.Bind(NetworkEndPoint.LoopbackIpv4);
            RemoteDriver.Listen();
        }

        [TearDown]
        public void IPC_TearDown()
        {
            Driver.Dispose();
            RemoteDriver.Dispose();
        }

        [Test]
        public void CreateAndConnect_NetworkConnection_ToRemoteEndPoint()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            Assert.That(RemoteDriver.Accept().IsCreated);

            Driver.ScheduleUpdate().Complete();
            DataStreamReader reader;
            Assert.That(connection.PopEvent(Driver, out reader) == NetworkEvent.Type.Connect);
        }


        [Test]
        public void CreateConnectPopAndClose_NetworkConnection_ToRemoteEndPoint()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            var remoteId = default(NetworkConnection);
            Assert.That((remoteId = RemoteDriver.Accept()) != default(NetworkConnection));

            DataStreamReader reader;

            Driver.ScheduleUpdate().Complete();
            Assert.That(connection.PopEvent(Driver, out reader) == NetworkEvent.Type.Connect);

            connection.Close(Driver);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            Assert.That(
                RemoteDriver.PopEventForConnection(remoteId, out reader) == NetworkEvent.Type.Disconnect);
        }

        [Test]
        public void Connection_SetupSendAndReceive()
        {
            var connection = Driver.Connect(RemoteDriver.LocalEndPoint());
            Assert.That(connection.IsCreated);
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            var remoteId = default(NetworkConnection);
            Assert.That((remoteId = RemoteDriver.Accept()) != default(NetworkConnection));

            DataStreamReader reader;

            Driver.ScheduleUpdate().Complete();
            Assert.That(connection.PopEvent(Driver, out reader) == NetworkEvent.Type.Connect);

            if (Driver.BeginSend(NetworkPipeline.Null, connection, out var writer) == 0)
            {
                writer.WriteBytes(new NativeArray<byte>(SharedConstants.ping, Allocator.Temp));
                Driver.EndSend(writer);
            }
            Driver.ScheduleUpdate().Complete();

            RemoteDriver.ScheduleUpdate().Complete();
            var ev = RemoteDriver.PopEventForConnection(remoteId, out reader);
            Assert.That(ev == NetworkEvent.Type.Data);
            Assert.That(reader.Length == SharedConstants.ping.Length);

            connection.Close(Driver);
            Driver.ScheduleUpdate().Complete();
            RemoteDriver.ScheduleUpdate().Complete();

            Assert.That(
                RemoteDriver.PopEventForConnection(remoteId, out reader) == NetworkEvent.Type.Disconnect);
        }
    }
}