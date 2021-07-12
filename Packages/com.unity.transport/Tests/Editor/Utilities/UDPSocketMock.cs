using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Networking.Transport.Tests
{
    public class UDPSocketMock : IDisposable
    {
        private struct ExpectedPacket
        {
            public byte[] Packet;
            public Action<EndPoint, byte[]> Callback;
            public PacketParameter[] Parameters;
            public bool Optional;
        }

        private const int k_BufferSize = 1024;
        private const int k_ExpectedPacketsBufferSize = 1024;
        private Socket m_Socket;
        private byte[] m_Buffer;
        private EndPoint m_LocalEndpoint = new IPEndPoint(IPAddress.Loopback, 0);
        private ExpectedPacket[] m_ExpectedPackets;
        private int m_ExpectedPacketsCount;
        private List<Exception> m_Exceptions = new List<Exception>();

        private bool m_disposed = false;


        public UDPSocketMock(string address, ushort port)
        {
            m_Buffer = new byte[k_BufferSize];
            m_ExpectedPackets = new ExpectedPacket[k_ExpectedPacketsBufferSize];
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            m_Socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            m_Socket.BeginReceiveFrom(m_Buffer, 0, k_BufferSize, SocketFlags.None, ref m_LocalEndpoint, Receive, null);
        }

        public void Dispose()
        {
            if (m_disposed)
                return;

            Close();
            m_Socket.Dispose();

            foreach (var e in m_Exceptions)
            {
                UnityEngine.Debug.LogException(e);
                Assert.Fail($"UDP socket exception: {e.Message}");
            }

            for (int i = 0; i < m_ExpectedPacketsCount; i++)
            {
                if (!m_ExpectedPackets[i].Optional)
                    Assert.Fail($"Expected messages not received");
            }

            m_disposed = true;
        }

        public void Close()
        {
            try
            {
                m_Socket.Shutdown(SocketShutdown.Both);
            } catch (SocketException) {}
            
            m_Socket.Close(1);
        }

        private void Receive(IAsyncResult result)
        {
            int count = m_Socket.EndReceiveFrom(result, ref m_LocalEndpoint);

            ProcessPacket(m_Buffer, count, m_LocalEndpoint);

            m_Socket.BeginReceiveFrom(m_Buffer, 0, k_BufferSize, SocketFlags.None, ref m_LocalEndpoint, Receive, null);
        }

        private void ProcessPacket(byte[] buffer, int count, EndPoint endpoint)
        {
            for (var p = m_ExpectedPacketsCount - 1; p >= 0; --p)
            {
                ref var packet = ref m_ExpectedPackets[p];
                if (IsExpectedPacket(ref packet, buffer, count))
                {
                    try
                    {
                        packet.Callback?.Invoke(endpoint, buffer.Take(count).ToArray());
                    }
                    catch(Exception e)
                    {
                        m_Exceptions.Add(e);
                    }

                    // remove the packet from the list
                    --m_ExpectedPacketsCount;
                    packet = m_ExpectedPackets[m_ExpectedPacketsCount];
                    m_ExpectedPackets[m_ExpectedPacketsCount] = default;

                    return;
                }
            }

            var samePackets = new List<int>(32);
            for (var p = m_ExpectedPacketsCount - 1; p >= 0; --p)
            {
                ref var packet = ref m_ExpectedPackets[p];

                if (SameHeaders(ref packet, ref buffer))
                {
                    samePackets.Add(p);
                }
            }

            if (samePackets.Count > 0)
            {
                var message = $"A unexpected packet was received:  \n{ByteBufferToString(buffer, count)}  \nProbably were expecting one of these ({samePackets.Count} candidates):  \n";

                for (int i = 0; i < samePackets.Count; i++)
                {
                    message += $"{ByteBufferToString(m_ExpectedPackets[i].Packet, count)}\n";
                }

                m_Exceptions.Add(new Exception(message));
            }
            else
                m_Exceptions.Add(new Exception($"A completely unexpected (didn't even expect this header) packet was received:  \n{ByteBufferToString(buffer, count)}"));
        }

        private static string ByteBufferToString(byte[] buffer, int count)
        {
            const string separator = ",\t";
            var sb = new StringBuilder(count * 4 + separator.Length * (count * 4 - 1) + (count / 16 - 1));

            for (var i = 0; i < count; i++)
            {
                sb.Append("0x");
                sb.Append(buffer[i].ToString("X2").ToLowerInvariant());
                if (i != count - 1)
                {
                    sb.Append(separator);

                    if (i == 3 || (i-3) % 16 == 0)
                        sb.Append('\n');
                }
            }

            return sb.ToString();
        }

        private static bool SameHeaders(ref ExpectedPacket packet, ref byte[] buffer)
        {
            for (var i = 0; i < 4; i++)
            {
                if (packet.Packet[i] != buffer[i])
                    return false;
            }

            return true;
        }

        private static bool IsExpectedPacket(ref ExpectedPacket packet, byte[] buffer, int count)
        {
            if (count != packet.Packet.Length)
                return false;
            
            for (var i = 0; i < count; ++i)
            {
                if (buffer[i] != packet.Packet[i])
                {
                    var isParamByte = false;

                    for (var j = 0; j < packet.Parameters?.Length; ++j)
                    {
                        var parameter = packet.Parameters[j];
                        if (i >= parameter.Offset && i < (parameter.Offset + parameter.Size))
                        {
                            isParamByte = true;
                            break;
                        }
                    }

                    if (isParamByte)
                        continue;

                    return false;
                }
            }

            return true;
        }

        public void Send(byte[] packet, EndPoint endpoint)
        {
            Send(packet, packet.Length, endpoint);
        }

        public void Send(byte[] packet, int length, EndPoint endpoint)
        {
            m_Socket.SendTo(packet, 0, length, SocketFlags.None, endpoint);
        }

        public void ExpectPacket(byte[] packet, Action<EndPoint, byte[]> callback, PacketParameter[] parameters = null, bool optional = false)
        {
            m_ExpectedPackets[m_ExpectedPacketsCount++] = new ExpectedPacket
            {
                Packet = packet,
                Callback = callback,
                Parameters = parameters,
                Optional = optional,
            };
        }

        public unsafe void ExpectPacket<T>(byte[] packet, int offset, Action<EndPoint, byte[], T> callback) where T : unmanaged
        {
            var parameter = new PacketParameter
            {
                Offset = offset,
                Size = UnsafeUtility.SizeOf<T>(),
            };

            if (parameter.Offset + parameter.Size > packet.Length)
                throw new ArgumentException("The parameter doesn't fit in the packet size");

            ExpectPacket(packet, (endpoint, data) =>
            {
                fixed (byte* ptr = &data[offset])
                {
                    T paramValue = *(T*)ptr;
                    callback(endpoint, data, paramValue);
                }
            }, new []{ parameter });
        }
    }

    public struct PacketParameter
    {
        public int Offset;
        public int Size;
        public Type Type;

        public PacketParameter(int offset)
        {
            Offset = offset;
            Size = default;
            Type = default;
        }
    }
}