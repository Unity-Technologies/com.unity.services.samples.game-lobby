using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Unity.Networking.Transport.Protocols;
using Unity.Networking.Transport.Relay;

namespace Unity.Networking.Transport.Tests
{
    public class RelayServerMock : UDPSocketMock
    {
        private string m_Address;
        private ushort m_Port;

        private Dictionary<RelayAllocationId, EndPoint> m_ClientsAddresses = new Dictionary<RelayAllocationId, EndPoint>();

        public RelayServerMock(string address, ushort port) : base(address, port)
        {
            m_Address = address;
            m_Port = port;
        }

        // host is clientKey = 0
        public RelayServerData GetRelayConnectionData(int clientKey)
        {
            var endpoint = NetworkEndPoint.Parse(m_Address, m_Port);
            return new RelayServerData (
                endpoint:           ref endpoint,
                nonce:              23,
                allocationId:       GetAllocationIdForClient(clientKey),
                connectionData:     "sGsP0X8REiXn+PS51+lQDPBjowDlvV5zPgh15puL5YXFd3XdQ/oHkXh0a+m8A3riVvtUdZvZloYNpsi19flB6cvfpXNvib9SQ5UUMH0V+hNVExAog21jLA7PlBPp2eLHtKeCRflhR2pSq6FmRplmXLfdu4fV3eCgruvpr/pr6lVxeATN5k13OiY4lnEv5otyUsfkKFyIO+Sann97MsEklKgoAtVmlw6QurBVb+W3GDIyAHYF15UrQkkHE46fBkCbgFdry2hDQjn+6uXnxC3LVPfJw0jS4FpdbLfiUik/qATkZyX9eu97PNLw9lL1aogDLRG/ztmpi4Slwpl3awXr",
                hostConnectionData: "sGsP0X8REiXn+PS51+lQDPBjowDlvV5zPgh15puL5YXFd3XdQ/oHkXh0a+m8A3riVvtUdZvZloYNpsi19flB6cvfpXNvib9SQ5UUMH0V+hNVExAog21jLA7PlBPp2eLHtKeCRflhR2pSq6FmRplmXLfdu4fV3eCgruvpr/pr6lVxeATN5k13OiY4lnEv5otyUsfkKFyIO+Sann97MsEklKgoAtVmlw6QurBVb+W3GDIyAHYF15UrQkkHE46fBkCbgFdry2hDQjn+6uXnxC3LVPfJw0jS4FpdbLfiUik/qATkZyX9eu97PNLw9lL1aogDLRG/ztmpi4Slwpl3awXr",
                key:                "cSzQ2I5ZCQ7vnHMt9fDB2/+xDkL2VUKoUT7AVNYhe+kaTQLptQ0gUbco/Qgiicow89VtxOXcw92IozbdDG848w=="
            );
        }

        public bool CompleteBind(NetworkDriver driver, int clientKey = -1)
        {
            SetupForBind(clientKey);

            WaitForCondition(() =>
            {
                driver.ScheduleUpdate().Complete();
                return driver.Bound;
            });

            return driver.Bound;
        }

        public bool CompleteConnect(NetworkDriver host, out (NetworkConnection hostToClient, NetworkConnection clientToHost)[] resultConnections,  params NetworkDriver[] clients)
        {
            resultConnections = new (NetworkConnection hostToClient, NetworkConnection clientToHost)[clients.Length];

            if (host.Bind(NetworkEndPoint.AnyIpv4) != 0)
                return false;

            if (!CompleteBind(host, 0))
                return false;

            if (host.Listen() != 0)
                return false;

            var clientId = 0;
            foreach (var client in clients)
            {
                if (client.Bind(NetworkEndPoint.AnyIpv4) != 0)
                    return false;

                if (!CompleteBind(client, clientId + 1))
                    return false;

                SetupForConnect(clientId + 1);
                
                var clientToHost = client.Connect(GetRelayConnectionData(0).Endpoint);

                if (default(NetworkConnection) == clientToHost)
                    return false;

                RelayServerMock.WaitForCondition(() =>
                {
                    client.ScheduleUpdate(default).Complete();
                    host.ScheduleUpdate(default).Complete();

                    return client.GetConnectionState(clientToHost) == NetworkConnection.State.Connected;
                });

                if (client.GetConnectionState(clientToHost) != NetworkConnection.State.Connected)
                    return false;

                if (client.PopEvent(out resultConnections[clientId].clientToHost, out var _) != NetworkEvent.Type.Connect)
                    return false;
                
                if ((resultConnections[clientId].hostToClient = host.Accept()) == default)
                    return false;
                
                ++clientId;
            }

            return true;
        }

        public void RegisterBoundClient(RelayAllocationId allocationId, EndPoint endpoint)
        {
            m_ClientsAddresses[allocationId] = endpoint;
        }

        public void SetupForBind(int clientKey = -1)
        {
            ExpectPacket(BindPacket, (endpoint, data) => 
            {
                if (clientKey >= 0)
                    RegisterBoundClient(GetAllocationIdForClient(clientKey), endpoint);

                Send(BindReceivedPacket, endpoint);
            });
        }

        public void ExpectOptionalRepeatedPacket(byte[] packet)
        {
            Action<EndPoint, byte[]> callback = null;
            
            callback = (endpoint, data) =>
            {
                ExpectPacket(packet, callback, optional:true);
            };

            callback(null, null);
        }

        public void SetupForBindRetry(int retryCount, Action onBindReceived)
        {
            var retriesLeft = retryCount;
            Action<EndPoint, byte[]> bindReceiveMethod = null;

            bindReceiveMethod = (endpoint, data) => 
            {
                onBindReceived?.Invoke();

                if (--retriesLeft > 0)
                    ExpectPacket(BindPacket, bindReceiveMethod);
                else
                {
                    ExpectOptionalRepeatedPacket(BindPacket);
                    Send(BindReceivedPacket, endpoint);
                }
            };

            ExpectPacket(BindPacket, bindReceiveMethod);
        }

        public unsafe void SetupForConnect(int clientKey)
        {
            var connectRequestPacket = ConnectRequestPacket;
            fixed (byte* ptr = &connectRequestPacket[0])
            {
                *(RelayAllocationId*)(ptr + 4) = GetAllocationIdForClient(clientKey);
            }

            var acceptedPacket = AcceptedPacket;
            fixed (byte* ptr = &acceptedPacket[0])
            {
                *(RelayAllocationId*)(ptr + 4) = GetAllocationIdForClient(0);
                *(RelayAllocationId*)(ptr + 20) = GetAllocationIdForClient(clientKey);
            }

            ExpectPacket(connectRequestPacket, (endpoint, data) =>
            {
                Send(acceptedPacket, endpoint);
                
                SetupForRelay(clientKey, 0, UdpCHeader.Length);
                SetupForRelay(0, clientKey, UdpCHeader.Length + 2);
            });
        }

        public unsafe void SetupForConnectRetry(int clientKey, int retryCount, Action onConnectReceived)
        {
            var connectRequestPacket = ConnectRequestPacket;
            fixed (byte* ptr = &connectRequestPacket[0])
            {
                *(RelayAllocationId*)(ptr + 4) = GetAllocationIdForClient(clientKey);
            }

            var acceptedPacket = AcceptedPacket;
            fixed (byte* ptr = &acceptedPacket[0])
            {
                *(RelayAllocationId*)(ptr + 4) = GetAllocationIdForClient(0);
                *(RelayAllocationId*)(ptr + 20) = GetAllocationIdForClient(clientKey);
            }

            var retriesLeft = retryCount;
            Action<EndPoint, byte[]> connectReceiveMethod = null;

            connectReceiveMethod = (endpoint, data) =>
            {
                onConnectReceived?.Invoke();

                if (--retriesLeft > 0)
                    ExpectPacket(connectRequestPacket, connectReceiveMethod);
                else
                {
                    ExpectOptionalRepeatedPacket(connectRequestPacket);

                    Send(acceptedPacket, endpoint);
                    
                    SetupForRelay(clientKey, 0, UdpCHeader.Length);
                    SetupForRelay(0, clientKey, UdpCHeader.Length + 2);
                }
            };

            ExpectPacket(connectRequestPacket, connectReceiveMethod);
        }

        public unsafe void SetupForRelay(int from, int to, ushort dataLength)
        {
            var relayMessage = new byte[RelayPacket.Length + dataLength];
            Array.Copy(RelayPacket, relayMessage, RelayPacket.Length);

            fixed (byte* ptr = &relayMessage[0])
            {
                *(RelayAllocationId*)(ptr + 4) = GetAllocationIdForClient(from);
                *(RelayAllocationId*)(ptr + 20) = GetAllocationIdForClient(to);
                *(ushort*)(ptr + 36) = RelayNetworkProtocol.SwitchEndianness(dataLength);
            }

            var parameters = new [] {
                new PacketParameter {Offset = 38, Size = dataLength}
            };

            ExpectPacket(relayMessage, (endpoint, data) =>
            {
                if (m_ClientsAddresses.TryGetValue(GetAllocationIdForClient(to), out var toEndpoint))
                    Send(data, toEndpoint);
                else
                    throw new Exception("Relay was not sent because destination client was not bound");
            }, parameters);
        }

        public unsafe void SetupForDisconnect(int from, int to)
        {
            var disconnectPacket = DisconnectPacket;
            fixed (byte* ptr = &disconnectPacket[0])
            {
                *(RelayAllocationId*)(ptr + 4) = GetAllocationIdForClient(from);
                *(RelayAllocationId*)(ptr + 20) = GetAllocationIdForClient(to);
            }

            SetupForRelay(from, to, 4);
            ExpectPacket(disconnectPacket, null);
        }

        static private byte[] BindPacket => new byte[] {
            0xda, 0x72, 0x00, 0x00,                                                                         // Header
            0x00,                                                                                           // Accept Mode
            0x17, 0x00,                                                                                     // Nonce
            0xff,                                                                                           // ConnectionData Size
            0xb0, 0x6b, 0x0f, 0xd1, 0x7f, 0x11, 0x12, 0x25, 0xe7, 0xf8, 0xf4, 0xb9, 0xd7, 0xe9, 0x50, 0x0c, // ConnectionData
            0xf0, 0x63, 0xa3, 0x00, 0xe5, 0xbd, 0x5e, 0x73, 0x3e, 0x08, 0x75, 0xe6, 0x9b, 0x8b, 0xe5, 0x85, 
            0xc5, 0x77, 0x75, 0xdd, 0x43, 0xfa, 0x07, 0x91, 0x78, 0x74, 0x6b, 0xe9, 0xbc, 0x03, 0x7a, 0xe2,
            0x56, 0xfb, 0x54, 0x75, 0x9b, 0xd9, 0x96, 0x86, 0x0d, 0xa6, 0xc8, 0xb5, 0xf5, 0xf9, 0x41, 0xe9,
            0xcb, 0xdf, 0xa5, 0x73, 0x6f, 0x89, 0xbf, 0x52, 0x43, 0x95, 0x14, 0x30, 0x7d, 0x15, 0xfa, 0x13,
            0x55, 0x13, 0x10, 0x28, 0x83, 0x6d, 0x63, 0x2c, 0x0e, 0xcf, 0x94, 0x13, 0xe9, 0xd9, 0xe2, 0xc7,
            0xb4, 0xa7, 0x82, 0x45, 0xf9, 0x61, 0x47, 0x6a, 0x52, 0xab, 0xa1, 0x66, 0x46, 0x99, 0x66, 0x5c,
            0xb7, 0xdd, 0xbb, 0x87, 0xd5, 0xdd, 0xe0, 0xa0, 0xae, 0xeb, 0xe9, 0xaf, 0xfa, 0x6b, 0xea, 0x55,
            0x71, 0x78, 0x04, 0xcd, 0xe6, 0x4d, 0x77, 0x3a, 0x26, 0x38, 0x96, 0x71, 0x2f, 0xe6, 0x8b, 0x72,
            0x52, 0xc7, 0xe4, 0x28, 0x5c, 0x88, 0x3b, 0xe4, 0x9a, 0x9e, 0x7f, 0x7b, 0x32, 0xc1, 0x24, 0x94,
            0xa8, 0x28, 0x02, 0xd5, 0x66, 0x97, 0x0e, 0x90, 0xba, 0xb0, 0x55, 0x6f, 0xe5, 0xb7, 0x18, 0x32,
            0x32, 0x00, 0x76, 0x05, 0xd7, 0x95, 0x2b, 0x42, 0x49, 0x07, 0x13, 0x8e, 0x9f, 0x06, 0x40, 0x9b,
            0x80, 0x57, 0x6b, 0xcb, 0x68, 0x43, 0x42, 0x39, 0xfe, 0xea, 0xe5, 0xe7, 0xc4, 0x2d, 0xcb, 0x54,
            0xf7, 0xc9, 0xc3, 0x48, 0xd2, 0xe0, 0x5a, 0x5d, 0x6c, 0xb7, 0xe2, 0x52, 0x29, 0x3f, 0xa8, 0x04,
            0xe4, 0x67, 0x25, 0xfd, 0x7a, 0xef, 0x7b, 0x3c, 0xd2, 0xf0, 0xf6, 0x52, 0xf5, 0x6a, 0x88, 0x03,
            0x2d, 0x11, 0xbf, 0xce, 0xd9, 0xa9, 0x8b, 0x84, 0xa5, 0xc2, 0x99, 0x77, 0x6b, 0x05, 0xeb,
            0x51, 0x5f, 0x83, 0x86, 0x30, 0x58, 0x5f, 0x99, 0xfc, 0x95, 0x93, 0x76, 0x28, 0x41, 0x09, 0xf2, // HMAC
            0x6b, 0x62, 0x43, 0x3f, 0x70, 0x29, 0x23, 0x38, 0xc0, 0xcf, 0xd5, 0xdd, 0x74, 0x78, 0x7f, 0x09
            };

        static private byte[] BindReceivedPacket => new byte[] { 0xda, 0x72, 0x00, 0x01 };

        static private byte[] ConnectRequestPacket => new byte[] {
            0xda, 0x72, 0x00, 0x03,                                                                         // Header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Allocation Id
            0xFF,                                                                                           // ToConnectionData Size
            0xb0, 0x6b, 0x0f, 0xd1, 0x7f, 0x11, 0x12, 0x25, 0xe7, 0xf8, 0xf4, 0xb9, 0xd7, 0xe9, 0x50, 0x0c, // ToConnectionData
            0xf0, 0x63, 0xa3, 0x00, 0xe5, 0xbd, 0x5e, 0x73, 0x3e, 0x08, 0x75, 0xe6, 0x9b, 0x8b, 0xe5, 0x85, 
            0xc5, 0x77, 0x75, 0xdd, 0x43, 0xfa, 0x07, 0x91, 0x78, 0x74, 0x6b, 0xe9, 0xbc, 0x03, 0x7a, 0xe2,
            0x56, 0xfb, 0x54, 0x75, 0x9b, 0xd9, 0x96, 0x86, 0x0d, 0xa6, 0xc8, 0xb5, 0xf5, 0xf9, 0x41, 0xe9,
            0xcb, 0xdf, 0xa5, 0x73, 0x6f, 0x89, 0xbf, 0x52, 0x43, 0x95, 0x14, 0x30, 0x7d, 0x15, 0xfa, 0x13,
            0x55, 0x13, 0x10, 0x28, 0x83, 0x6d, 0x63, 0x2c, 0x0e, 0xcf, 0x94, 0x13, 0xe9, 0xd9, 0xe2, 0xc7,
            0xb4, 0xa7, 0x82, 0x45, 0xf9, 0x61, 0x47, 0x6a, 0x52, 0xab, 0xa1, 0x66, 0x46, 0x99, 0x66, 0x5c,
            0xb7, 0xdd, 0xbb, 0x87, 0xd5, 0xdd, 0xe0, 0xa0, 0xae, 0xeb, 0xe9, 0xaf, 0xfa, 0x6b, 0xea, 0x55,
            0x71, 0x78, 0x04, 0xcd, 0xe6, 0x4d, 0x77, 0x3a, 0x26, 0x38, 0x96, 0x71, 0x2f, 0xe6, 0x8b, 0x72,
            0x52, 0xc7, 0xe4, 0x28, 0x5c, 0x88, 0x3b, 0xe4, 0x9a, 0x9e, 0x7f, 0x7b, 0x32, 0xc1, 0x24, 0x94,
            0xa8, 0x28, 0x02, 0xd5, 0x66, 0x97, 0x0e, 0x90, 0xba, 0xb0, 0x55, 0x6f, 0xe5, 0xb7, 0x18, 0x32,
            0x32, 0x00, 0x76, 0x05, 0xd7, 0x95, 0x2b, 0x42, 0x49, 0x07, 0x13, 0x8e, 0x9f, 0x06, 0x40, 0x9b,
            0x80, 0x57, 0x6b, 0xcb, 0x68, 0x43, 0x42, 0x39, 0xfe, 0xea, 0xe5, 0xe7, 0xc4, 0x2d, 0xcb, 0x54,
            0xf7, 0xc9, 0xc3, 0x48, 0xd2, 0xe0, 0x5a, 0x5d, 0x6c, 0xb7, 0xe2, 0x52, 0x29, 0x3f, 0xa8, 0x04,
            0xe4, 0x67, 0x25, 0xfd, 0x7a, 0xef, 0x7b, 0x3c, 0xd2, 0xf0, 0xf6, 0x52, 0xf5, 0x6a, 0x88, 0x03,
            0x2d, 0x11, 0xbf, 0xce, 0xd9, 0xa9, 0x8b, 0x84, 0xa5, 0xc2, 0x99, 0x77, 0x6b, 0x05, 0xeb,
            };

        static private byte[] AcceptedPacket => new byte[] {
            0xda, 0x72, 0x00, 0x06,                                                                         // Header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // From Allocation Id
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // To Allocation Id
            };

        static private byte[] RelayPacket => new byte[] {
            0xda, 0x72, 0x00, 0x0a,                                                                         // Header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // From Allocation Id
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // To Allocation Id
            0x00, 0x00,                                                                                     // Data Length
            // Content...
            };

        static private byte[] DisconnectPacket => new byte[] {
            0xda, 0x72, 0x00, 0x09,                                                                         // Header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // From Allocation Id
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // To Allocation Id
            };

        static private byte[] PingPacket => new byte[] {
            0xda, 0x72, 0x00, 0x02,                                                                         // Header
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // From Allocation Id
            };

        static private RelayAllocationId GetAllocationIdForClient(int clientKey)
        {
            var allocationId = new RelayAllocationId();

            if (clientKey == 0)
                clientKey = -1;

            unsafe
            {
                *(int*)allocationId.Value = clientKey;
            }

            return allocationId;
        }

        static public void WaitForCondition(Func<bool> condition, long timeout = 1000)
        {
            var stopwatch = Stopwatch.StartNew();

            while(stopwatch.ElapsedMilliseconds <= timeout)
            {
                if (condition())
                    break;

                Thread.Sleep(5);
            }
        }
    }
}