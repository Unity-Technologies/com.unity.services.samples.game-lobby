using System.Collections;
using Unity.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Protocols;

namespace Tests
{
    public class ServerAndClientTests
    {
        NetworkDriver server_driver;
        NetworkConnection connectionToClient;

        NetworkDriver client_driver;
        NetworkConnection clientToServerConnection;

        NetworkEvent.Type ev;
        DataStreamReader stream;

        private const string backend = "baselib";

        NetworkEvent.Type PollDriverAndFindDataEvent(ref NetworkDriver driver, NetworkConnection connection, NetworkEvent.Type eventType, int maxRetryCount=10)
        {
            for (int retry = 0; retry < maxRetryCount; ++retry)
            {
                driver.ScheduleUpdate().Complete();
                ev = driver.PopEventForConnection(connection, out stream);
                if (ev == eventType || ev != NetworkEvent.Type.Empty)
                {
                    return ev;
                }
            }
            return NetworkEvent.Type.Empty;
        }

        public void SetupServerAndClientAndConnectThem(int bufferSize)
        {
            //setup server
            server_driver = NetworkDriver.Create(new NetworkDataStreamParameter { size = bufferSize });
            NetworkEndPoint server_endpoint = NetworkEndPoint.LoopbackIpv4;
            server_endpoint.Port = 1337;
            var ret = server_driver.Bind(server_endpoint);
            int maxRetry = 10;
            while (ret != 0 && --maxRetry > 0)
            {
                server_endpoint.Port += 17;
                ret = server_driver.Bind(server_endpoint);
            };
            Assert.AreEqual(ret, 0, "Failed to bind the socket");
            server_driver.Listen();

            //setup client
            client_driver = NetworkDriver.Create(new NetworkDataStreamParameter { size = bufferSize });
            clientToServerConnection = client_driver.Connect(server_endpoint);

            //update drivers
            client_driver.ScheduleUpdate().Complete();

            for (int i = 0; i < 10; ++i)
            {
                server_driver.ScheduleUpdate().Complete();
                //accept connection
                connectionToClient = server_driver.Accept();
                if (connectionToClient.IsCreated)
                    break;
            }
            Assert.IsTrue(connectionToClient.IsCreated, "Failed to accept the connection");

            ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClient, NetworkEvent.Type.Empty);
            Assert.IsTrue(ev == NetworkEvent.Type.Empty, $"Not empty NetworkEvent on the server appeared, got {ev} using {backend}");

            ev = PollDriverAndFindDataEvent(ref client_driver, clientToServerConnection, NetworkEvent.Type.Connect);
            Assert.IsTrue(ev == NetworkEvent.Type.Connect, $"NetworkEvent should have Type.Connect on the client, but got {ev} using {backend}");
        }

        public void DisconnectAndCleanup()
        {
            clientToServerConnection.Close(client_driver);

            //update drivers
            client_driver.ScheduleUpdate().Complete();
            ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClient, NetworkEvent.Type.Disconnect);
            Assert.IsTrue(ev == NetworkEvent.Type.Disconnect, "NetworkEvent.Type.Disconnect was expected to appear, but " + ev + " appeared");

            server_driver.Dispose();
            client_driver.Dispose();
        }

        [UnityTest]
        public IEnumerator ServerAndClient_Connect_Successfully()
        {
            SetupServerAndClientAndConnectThem(0);
            DisconnectAndCleanup();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ServerAnd5Clients_Connect_Successfully()
        {
            int numberOfClients = 5;
            NativeArray<NetworkConnection> connectionToClientArray;
            NetworkDriver[] client_driversArray = new NetworkDriver[numberOfClients];
            NativeArray<NetworkConnection> clientToServerConnectionsArray;

            //setup server
            connectionToClientArray = new NativeArray<NetworkConnection>(numberOfClients, Allocator.Persistent);
            server_driver = NetworkDriver.Create(new NetworkDataStreamParameter { size = 0 });
            NetworkEndPoint server_endpoint = NetworkEndPoint.LoopbackIpv4;
            server_endpoint.Port = 1337;
            server_driver.Bind(server_endpoint);
            server_driver.Listen();

            //setup clients
            clientToServerConnectionsArray = new NativeArray<NetworkConnection>(numberOfClients, Allocator.Persistent);

            for (int i = 0; i < numberOfClients; i++)
            {
                client_driversArray[i] = NetworkDriver.Create(new NetworkDataStreamParameter { size = 0 });
                clientToServerConnectionsArray[i] = client_driversArray[i].Connect(server_endpoint);
            }

            //update drivers
            for (int i = 0; i < numberOfClients; i++)
                client_driversArray[i].ScheduleUpdate().Complete();
            server_driver.ScheduleUpdate().Complete();

            //accept connections
            for (int i = 0; i < numberOfClients; i++)
            {
                connectionToClientArray[i] = server_driver.Accept();
                ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClientArray[i], NetworkEvent.Type.Empty);
                Assert.IsTrue(ev == NetworkEvent.Type.Empty, "Not empty NetworkEvent on the server appeared");
                ev = PollDriverAndFindDataEvent(ref client_driversArray[i], clientToServerConnectionsArray[i], NetworkEvent.Type.Connect);
                Assert.IsTrue(ev == NetworkEvent.Type.Connect, "NetworkEvent should have Type.Connect on the client");
            }
            //close connections
            for (int i = 0; i < numberOfClients; i++)
            {
                clientToServerConnectionsArray[i].Close(client_driversArray[i]);

                //update drivers
                client_driversArray[i].ScheduleUpdate().Complete();
                ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClientArray[i], NetworkEvent.Type.Disconnect);
                Assert.IsTrue(ev == NetworkEvent.Type.Disconnect, "NetworkEvent.Type.Disconnect was expected to appear, but " + ev + " appeared");
            }

            server_driver.Dispose();
            for (int i = 0; i < numberOfClients; i++)
            {
                client_driversArray[i].Dispose();
            }
            connectionToClientArray.Dispose();
            clientToServerConnectionsArray.Dispose();

            yield return null;
        }

        [UnityTest]
        public IEnumerator ServerAndClient_PingPong_Successfully()
        {
            SetupServerAndClientAndConnectThem(0);

            //send data from client
            if (client_driver.BeginSend(clientToServerConnection, out var m_OutStream) == 0)
            {
                m_OutStream.Clear();
                m_OutStream.WriteBytes(new NativeArray<byte>(SharedConstants.ping, Allocator.Temp));
                client_driver.EndSend(m_OutStream);
            }
            client_driver.ScheduleFlushSend(default).Complete();

            //handle sent data
            ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClient, NetworkEvent.Type.Data);
            Assert.IsTrue(ev == NetworkEvent.Type.Data, "Expected to get Type.Data");
            var msg = new NativeArray<byte>(stream.Length, Allocator.Temp);
            stream.ReadBytes(msg);
            if (msg.Length == SharedConstants.ping.Length)
            {
                for (var i = 0; i < msg.Length; i++)
                {
                    if (SharedConstants.ping[i] != msg[i])
                    {
                        Assert.Fail("Data reading error");
                    }
                }
            }

            client_driver.ScheduleUpdate().Complete();

            //send data from server
            if (server_driver.BeginSend(connectionToClient, out m_OutStream) == 0)
            {
                m_OutStream.WriteBytes(new NativeArray<byte>(SharedConstants.pong, Allocator.Temp));
                server_driver.EndSend(m_OutStream);
            }

            //handle sent data
            server_driver.ScheduleUpdate().Complete();

            ev = PollDriverAndFindDataEvent(ref client_driver, clientToServerConnection, NetworkEvent.Type.Data);
            Assert.IsTrue(ev == NetworkEvent.Type.Data, "Expected to get Type.Data");
            msg = new NativeArray<byte>(stream.Length, Allocator.Temp);
            stream.ReadBytes(msg);
            if (msg.Length == SharedConstants.pong.Length)
            {
                for (var i = 0; i < msg.Length; i++)
                {
                    if (SharedConstants.pong[i] != msg[i])
                    {
                        Assert.Fail("Data reading error");
                    }
                }
            }

            DisconnectAndCleanup();
            yield return null;
        }

        //test for buffer overflow
        [UnityTest, UnityPlatform (RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        public IEnumerator ServerAndClient_SendBigMessage_OverflowsIncomingDriverBuffer()
        {
            SetupServerAndClientAndConnectThem(8);

            //send data from client
            if (client_driver.BeginSend(clientToServerConnection, out var m_OutStream) == 0)
            {
                m_OutStream.Clear();
                m_OutStream.WriteBytes(new NativeArray<byte>(SharedConstants.ping, Allocator.Temp));
                client_driver.EndSend(m_OutStream);
            }
            client_driver.ScheduleFlushSend(default).Complete();

            LogAssert.Expect(LogType.Error, "Error on receive, errorCode = 10040");

            //handle sent data
            server_driver.ScheduleUpdate().Complete();
            client_driver.ScheduleUpdate().Complete();

            Assert.AreEqual(10040, server_driver.ReceiveErrorCode);

            DisconnectAndCleanup();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ServerAndClient_SendMessageWithMaxLength_SentAndReceivedWithoutErrors()
        {
            SetupServerAndClientAndConnectThem(0);

            int messageLength = 1400-UdpCHeader.Length;
            var messageToSend = new NativeArray<byte>(messageLength, Allocator.Temp);
            for (int i = 0; i < messageLength; i++)
            {
                messageToSend[i] = (byte)(33 + (i % 93));
            }
            //send data from client
            if (client_driver.BeginSend(clientToServerConnection, out var m_OutStream) == 0)
            {
                m_OutStream.WriteBytes(messageToSend);
                client_driver.EndSend(m_OutStream);
            }

            client_driver.ScheduleFlushSend(default).Complete();

            ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClient, NetworkEvent.Type.Data);
            Assert.IsTrue(ev == NetworkEvent.Type.Data, "Expected to get Type.Data");
            var msg = new NativeArray<byte>(stream.Length, Allocator.Temp);
            stream.ReadBytes(msg);
            Assert.IsTrue(msg.Length == messageLength, "Lenghts of sent and received messages are different");

            DisconnectAndCleanup();
            yield return null;
        }

        [UnityTest, UnityPlatform (RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        public IEnumerator ServerAndClient_SendMessageWithMoreThenMaxLength_OverflowsIncomingDriverBuffer()
        {
            SetupServerAndClientAndConnectThem(0);

            //send data from client

            if (client_driver.BeginSend(clientToServerConnection, out var m_OutStream) == 0)
            {
                m_OutStream.Clear();
                int messageLength = 1401-UdpCHeader.Length;
                var messageToSend = new NativeArray<byte>(messageLength, Allocator.Temp);
                for (int i = 0; i < messageLength; i++)
                {
                    messageToSend[i] = (byte)(33 + (i % 93));
                }

                Assert.IsFalse(m_OutStream.WriteBytes(messageToSend));
                Assert.AreEqual(0, client_driver.EndSend(m_OutStream));
            }
            client_driver.ScheduleFlushSend(default).Complete();

            //handle sent data
            client_driver.ScheduleUpdate().Complete();
            ev = PollDriverAndFindDataEvent(ref server_driver, connectionToClient, NetworkEvent.Type.Data);
            Assert.IsTrue(ev == NetworkEvent.Type.Data, "Expected to get Type.Empty");
            Assert.IsFalse(stream.IsCreated);

            DisconnectAndCleanup();
            yield return null;
        }

        [UnityTest, UnityPlatform (RuntimePlatform.LinuxEditor, RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor)]
        public IEnumerator ServerAndClient_SendMessageWithoutReadingIt_GivesErrorOnDriverUpdate()
        {
            SetupServerAndClientAndConnectThem(0);

            //send data from client
            if (client_driver.BeginSend(clientToServerConnection, out var m_OutStream) == 0)
            {
                m_OutStream.WriteBytes(new NativeArray<byte>(SharedConstants.ping, Allocator.Temp));
                client_driver.EndSend(m_OutStream);
            }
            client_driver.ScheduleFlushSend(default).Complete();

            server_driver.ScheduleUpdate().Complete();
            client_driver.ScheduleUpdate().Complete();

            LogAssert.Expect(LogType.Error, "Resetting event queue with pending events (Count=1, ConnectionID=0) Listening: 1");
            server_driver.ScheduleUpdate().Complete();

            DisconnectAndCleanup();
            yield return null;
        }
    }
}

public class SharedConstants
{
    public static byte[] ping = {
        (byte)'f',
        (byte)'r',
        (byte)'o',
        (byte)'m',
        (byte)'s',
        (byte)'e',
        (byte)'r',
        (byte)'v',
        (byte)'e',
        (byte)'r'
    };

    public static byte[] pong = {
        (byte)'c',
        (byte)'l',
        (byte)'i',
        (byte)'e',
        (byte)'n',
        (byte)'t'
    };
}
