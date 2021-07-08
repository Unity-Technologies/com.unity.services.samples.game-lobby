using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Baselib.LowLevel;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Networking.Transport.Utilities.LowLevel.Unsafe;
using Unity.Networking.Transport.Protocols;
using ErrorState = Unity.Baselib.LowLevel.Binding.Baselib_ErrorState;
using ErrorCode = Unity.Baselib.LowLevel.Binding.Baselib_ErrorCode;
using Random = Unity.Mathematics.Random;
namespace Unity.Networking.Transport
{
    [BurstCompile]
    public struct WebSocketNetworkInterface : INetworkInterface
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_WEBGL
        private class SocketList
        {
            public struct SocketId
            {
                public Binding.Baselib_Socket_Handle socket;
            }
            public List<SocketId> OpenSockets = new List<SocketId>();

            ~SocketList()
            {
                foreach (var socket in OpenSockets)
                {
                    Binding.Baselib_Socket_Close(socket.socket);
                }
            }
        }
        private static SocketList AllSockets = new SocketList();
#endif
        unsafe struct BaselibData
        {
            public NetworkInterfaceEndPoint BoundAddress;
            #if !UNITY_WEBGL
            public Binding.Baselib_Socket_Handle ListenSocket;
            #endif
        }
        unsafe struct ConnectionState
        {
#if UNITY_WEBGL
            public int Socket;
#else
            public Binding.Baselib_Socket_Handle Socket;
            public WebSocketPacket Packet;
            public WebSocketPacket PendingSendPacket;
            public ulong RemainingPacketSize;
            public int HeaderSize;
            public int PacketStart;
            public uint Key0;
            public uint Key1;
            public uint Key2;
            public uint Key3;
            public long CloseTimeStamp;
#endif
            public int ConnectState;
        }
        const int WebSocketMaxHeaderSize = 14;
        const int ConnectionSentClose = 16;
        const int ConnectionIsClient = 8;
        const int ConnectionKnownByDriver = 4;
        const int ConnectionStateMask = 3;
        unsafe struct WebSocketPacket
        {
            public fixed byte Data[NetworkParameterConstants.MTU + WebSocketMaxHeaderSize];
            public int DataLength;

#if !UNITY_WEBGL
            public void ConstructBinary(void* payload, int payloadLen, bool useMask, uint mask)
            {
                // fin + binary
                int headerLen = 0;
                Data[headerLen++] = 0x82;
                byte maskFlag = (byte)(useMask ? 0x80 : 0);
                if (payloadLen < 126)
                    Data[headerLen++] = (byte)(maskFlag | payloadLen);
                else if (payloadLen <= 0xffff)
                {
                    Data[headerLen++] = (byte)(maskFlag | 126);
                    Data[headerLen++] = (byte)(payloadLen>>8);
                    Data[headerLen++] = (byte)(payloadLen&0xff);
                }
                else
                {
                    Data[headerLen++] = (byte)(maskFlag | 127);
                    Data[headerLen++] = (byte)0;
                    Data[headerLen++] = (byte)0;
                    Data[headerLen++] = (byte)0;
                    Data[headerLen++] = (byte)0;
                    Data[headerLen++] = (byte)((payloadLen>>24)&0xff);
                    Data[headerLen++] = (byte)((payloadLen>>16)&0xff);
                    Data[headerLen++] = (byte)((payloadLen>>8)&0xff);
                    Data[headerLen++] = (byte)(payloadLen&0xff);
                }
                if (useMask)
                {
                    Data[headerLen++] = (byte)(mask>>24);
                    Data[headerLen++] = (byte)((mask>>16)&0xff);
                    Data[headerLen++] = (byte)((mask>>8)&0xff);
                    Data[headerLen++] = (byte)(mask&0xff);
                    fixed (byte* ptr = Data)
                    {
                        var maskBytes = ptr+headerLen-4;
                        byte* dst = ptr + headerLen;
                        byte* src = (byte*)payload;
                        for (int i = 0; i < payloadLen; ++i)
                            dst[i] = (byte)(src[i]^maskBytes[i&3]);
                    }
                }
                else
                {
                    fixed (byte* ptr = Data)
                        UnsafeUtility.MemCpy(ptr+headerLen, payload, payloadLen);
                }
                DataLength = headerLen + payloadLen;
            }
            public void ConstructPong(void* payload, int payloadLen, bool useMask, uint mask)
            {
                ConstructBinary(payload, payloadLen, useMask, mask);
                Data[0] = 0x8a;
            }
            public void ConstructClose(ushort status, bool useMask, uint mask)
            {
                ConstructBinary(&status, 2, useMask, mask);
                Data[0] = 0x88;
            }
#endif
        }
#if UNITY_WEBGL
        static int s_NextSocketId = 0;
        private const string DLL = "__Internal";

        [DllImport(DLL, EntryPoint = "js_html_utpWebSocketCreate")]
        private static extern void WebSocketCreate(int sockId, IntPtr addrData, int addrSize, IntPtr data, int size);
        [DllImport(DLL, EntryPoint = "js_html_utpWebSocketDestroy")]
        private static extern void WebSocketDestroy(int sockId);
        [DllImport(DLL, EntryPoint = "js_html_utpWebSocketSend")]
        private static extern int WebSocketSend(int sockId, IntPtr data, int size);
        [DllImport(DLL, EntryPoint = "js_html_utpWebSocketRecv")]
        private static extern int WebSocketRecv(int sockId, IntPtr data, int size);
        [DllImport(DLL, EntryPoint = "js_html_utpWebSocketIsConnected")]
        private static extern int WebSocketIsConnected(int sockId);
#else
        unsafe struct SHA1
        {
            private void UpdateABCDE(int i, ref uint a, ref uint b, ref uint c, ref uint d, ref uint e, uint f, uint k)
            {
                var tmp = ((a << 5) | (a >> 27)) + e + f + k + words[i];
                e = d;
                d = c;
                c = (b << 30) | (b >> 2);
                b = a;
                a = tmp;
            }
            private void UpdateHash()
            {
                for (int i = 16; i < 80; ++i)
                {
                    words[i] = (words[i-3] ^ words[i-8] ^ words[i-14] ^ words[i-16]);
                    words[i] = (words[i] << 1) | (words[i] >> 31);
                }

                var a = h0;
                var b = h1;
                var c = h2;
                var d = h3;
                var e = h4;

                for (int i = 0; i < 20; ++i)
                {
                    var f = (b & c) | ((~b) & d);
                    var k = 0x5a827999u;
                    UpdateABCDE(i, ref a, ref b, ref c, ref d, ref e, f, k);
                }
                for (int i = 20; i < 40; ++i)
                {
                    var f = b ^ c ^ d;
                    var k = 0x6ed9eba1u;
                    UpdateABCDE(i, ref a, ref b, ref c, ref d, ref e, f, k);
                }
                for (int i = 40; i < 60; ++i)
                {
                    var f = (b & c) | (b & d) | (c & d);
                    var k = 0x8f1bbcdcu;
                    UpdateABCDE(i, ref a, ref b, ref c, ref d, ref e, f, k);
                }
                for (int i = 60; i < 80; ++i)
                {
                    var f = b ^ c ^ d;
                    var k = 0xca62c1d6u;
                    UpdateABCDE(i, ref a, ref b, ref c, ref d, ref e, f, k);
                }
                h0 += a;
                h1 += b;
                h2 += c;
                h3 += d;
                h4 += e;
            }
            public SHA1(in FixedString128 str)
            {
                h0 = 0x67452301u;
                h1 = 0xefcdab89u;
                h2 = 0x98badcfeu;
                h3 = 0x10325476u;
                h4 = 0xc3d2e1f0u;
                var bitLen = str.Length << 3;
                var numFullChunks = bitLen>>9;
                byte* ptr = str.GetUnsafePtr();
                for (int chunk = 0; chunk < numFullChunks; ++chunk)
                {
                    for (int i = 0; i < 16; ++i)
                    {
                        words[i] = (uint)((ptr[0]<<24) | (ptr[1]<<16) | (ptr[2]<<8) | ptr[3]);
                        ptr += 4;
                    }
                    UpdateHash();
                }
                var remainingBits = (bitLen&0x1ff);
                var remainingBytes = (remainingBits>>3);
                var fullWords = (remainingBytes>>2);
                for (int i = 0; i < fullWords; ++i)
                {
                    words[i] = (uint)((ptr[0]<<24) | (ptr[1]<<16) | (ptr[2]<<8) | ptr[3]);
                    ptr += 4;
                }
                var fullBytes = remainingBytes&3;
                switch (fullBytes)
                {
                    case 3:
                        words[fullWords] = (uint)((ptr[0]<<24) | (ptr[1]<<16) | (ptr[2]<<8) | 0x80u);
                        ptr += 3;
                        break;
                    case 2:
                        words[fullWords] = (uint)((ptr[0]<<24) | (ptr[1]<<16) | (0x80u << 8));
                        ptr += 2;
                        break;
                    case 1:
                        words[fullWords] = (uint)((ptr[0]<<24) | (0x80u << 16));
                        ptr += 1;
                        break;
                    case 0:
                        words[fullWords] = (uint)((0x80u << 24));
                        break;
                }
                ++fullWords;
                if (remainingBits >= 448)
                {
                    // Needs two chunks, one for the remaining bits and one for size
                    for (int i = fullWords; i < 16; ++i)
                        words[i] = 0;
                    UpdateHash();
                    for (int i = 0; i < 15; ++i)
                        words[i] = 0;
                    words[15] = (uint)bitLen;
                    UpdateHash();
                }
                else
                {
                    for (int i = fullWords; i < 15; ++i)
                        words[i] = 0;
                    words[15] = (uint)bitLen;
                    UpdateHash();
                }
            }
            public FixedString32 ToBase64()
            {
                FixedString32 base64 = default;
                AppendBase64(ref base64, (byte)(h0>>24), (byte)(h0>>16), (byte)(h0>>8));
                AppendBase64(ref base64, (byte)(h0), (byte)(h1>>24), (byte)(h1>>16));
                AppendBase64(ref base64, (byte)(h1>>8), (byte)(h1), (byte)(h2>>24));
                AppendBase64(ref base64, (byte)(h2>>16), (byte)(h2>>8), (byte)(h2));
                AppendBase64(ref base64, (byte)(h3>>24), (byte)(h3>>16), (byte)(h3>>8));
                AppendBase64(ref base64, (byte)(h3), (byte)(h4>>24), (byte)(h4>>16));
                AppendBase64(ref base64, (byte)(h4>>8), (byte)(h4));
                return base64;
            }
            private fixed uint words[80];
            private uint h0;
            private uint h1;
            private uint h2;
            private uint h3;
            private uint h4;
        }
        static byte ApplyTable(byte val)
        {
            if (val < 26)
                return (byte)(val + 'A');
            else if (val < 52)
                return (byte)(val + 'a' - 26);
            else if (val < 62)
                return (byte)(val + '0' - 52);
            else if (val == 62)
                return (byte)'+';
            return (byte)'/';
        }
        static void AppendBase64(ref FixedString32 base64, byte b0, byte b1, byte b2)
        {
            var c1 = ApplyTable((byte)(b0>>2));
            var c2 = ApplyTable((byte)(((b0&3)<<4) | (b1>>4)));
            var c3 = ApplyTable((byte)(((b1&0xf)<<2) | (b2>>6)));
            var c4 = ApplyTable((byte)(b2&0x3f));
            base64.Add(c1);
            base64.Add(c2);
            base64.Add(c3);
            base64.Add(c4);
        }
        static void AppendBase64(ref FixedString32 base64, byte b0, byte b1)
        {
            var c1 = ApplyTable((byte)(b0>>2));
            var c2 = ApplyTable((byte)(((b0&3)<<4) | (b1>>4)));
            var c3 = ApplyTable((byte)((b1&0xf)<<2));

            base64.Add(c1);
            base64.Add(c2);
            base64.Add(c3);
            base64.Add((byte)'=');
        }
        static void AppendBase64(ref FixedString32 base64, byte b0)
        {
            var c1 = ApplyTable((byte)(b0>>2));
            var c2 = ApplyTable((byte)((b0&3)<<4));

            base64.Add(c1);
            base64.Add(c2);
            base64.Add((byte)'=');
            base64.Add((byte)'=');
        }

        private static void WebSocketDestroy(Binding.Baselib_Socket_Handle socket)
        {
            Binding.Baselib_Socket_Close(socket);
        }
        private static void GenerateBase64Key(out FixedString32 key, uint key0, uint key1, uint key2, uint key3)
        {
            key = default;
            AppendBase64(ref key, (byte)(key0>>24), (byte)(key0>>16), (byte)(key0>>8));
            AppendBase64(ref key, (byte)(key0), (byte)(key1>>24), (byte)(key1>>16));
            AppendBase64(ref key, (byte)(key1>>8), (byte)(key1), (byte)(key2>>24));
            AppendBase64(ref key, (byte)(key2>>16), (byte)(key2>>8), (byte)(key2));
            AppendBase64(ref key, (byte)(key3>>24), (byte)(key3>>16), (byte)(key3>>8));
            AppendBase64(ref key, (byte)(key3));
        }
        private static unsafe int WebSocketIsConnected(Binding.Baselib_Socket_Handle socket, in NetworkInterfaceEndPoint address, uint key0, uint key1, uint key2, uint key3)
        {
            var error = default(ErrorState);
            var sockError = default(ErrorState);
            var sockFd = new Binding.Baselib_Socket_PollFd
            {
                handle = socket,
                requestedEvents = Binding.Baselib_Socket_PollEvents.Connected,
                errorState = &sockError
            };

            Binding.Baselib_Socket_Poll(&sockFd, 1, 0, &error);
            if (sockFd.errorState->code != ErrorCode.Success)
                return -1;
            if ((sockFd.resultEvents & Binding.Baselib_Socket_PollEvents.Connected) != 0)
            {
                FixedString32 end = "\r\n";
                FixedString512 handshake = "GET / HTTP/1.1\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Version: 13\r\n";
                FixedString32 key = "Sec-WebSocket-Key: ";
                handshake.Append(key);
                GenerateBase64Key(out key, key0, key1, key2, key3);
                handshake.Append(key);
                handshake.Append(end);
                FixedString128 host = "Host: ";
                handshake.Append(host);
                fixed (void* dataptr = address.data)
                    handshake.Append(NetworkEndPoint.AddressToString(*(Binding.Baselib_NetworkAddress*)dataptr));
                handshake.Append(end);
                handshake.Append(end);
                var count = (int) Binding.Baselib_Socket_TCP_Send(
                    socket,
                    (IntPtr)handshake.GetUnsafePtr(),
                    (uint)handshake.Length,
                    &error);
                if (sockFd.errorState->code != ErrorCode.Success || count != handshake.Length)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnityEngine.Debug.LogWarning("Failed to send WebSocket client handshake.");
#endif
                    return -1;
                }
                return 1;
            }
            return 0;
        }
        private static unsafe int WebSocketIsHandshakeComplete(ref ConnectionState connection)
        {
            var error = default(ErrorState);
            FixedString4096 recvHandshake = default;
            int receivedBytes = (int)Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)recvHandshake.GetUnsafePtr(), (uint)FixedString4096.UTF8MaxLengthInBytes, &error);
            if (error.code != ErrorCode.Success)
                return -1;
            if (receivedBytes == 0)
                return 0;
            recvHandshake.Length = (ushort)receivedBytes;
            // If handshake does not end with \r\n\r\n it was an invalid or incomplete message
            if (recvHandshake.Length < 4 ||
                recvHandshake[recvHandshake.Length-4] != '\r' || recvHandshake[recvHandshake.Length-3] != '\n' ||
                recvHandshake[recvHandshake.Length-2] != '\r' || recvHandshake[recvHandshake.Length-1] != '\n')
            {
                // Invalid header
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                UnityEngine.Debug.LogWarning("Received invalid http or incomplete message for handshake.");
#endif
                return -1;
            }

            int lineStart = 0;
            int lineEnd = 0;
            while (recvHandshake[lineEnd] != '\r' || recvHandshake[lineEnd+1] != '\n')
                ++lineEnd;
            int firstLineEnd = lineEnd;
            var headerLookup = new NativeHashMap<FixedString128, FixedString128>(16, Allocator.Temp);
            while (true)
            {
                lineEnd += 2;
                lineStart = lineEnd;
                while (recvHandshake[lineEnd] != '\r' || recvHandshake[lineEnd+1] != '\n')
                    ++lineEnd;
                if (lineStart == lineEnd)
                    break;

                // Found a line - analyze it
                int keyStart = lineStart;
                while (recvHandshake[keyStart] == ' ' || recvHandshake[keyStart] == '\t')
                    ++keyStart;
                int keyEnd = keyStart;
                FixedString128 key = default;
                // Not allowing whitespace in keys
                while (recvHandshake[keyEnd] != ':' && recvHandshake[keyEnd] != ' ' && recvHandshake[keyEnd] != '\t' && recvHandshake[keyEnd] != '\r' && recvHandshake[keyEnd] != '\n')
                {
                    byte ch = recvHandshake[keyEnd];
                    if (ch >= (byte)'A' && ch <= (byte)'Z')
                        ch = (byte)(ch + 'a' - 'A');
                    key.Add(ch);
                    ++keyEnd;
                }
                int valueStart = keyEnd;
                while (recvHandshake[valueStart] != ':')
                {
                    if (recvHandshake[valueStart] != ' ' && recvHandshake[valueStart] != '\t' && recvHandshake[valueStart] != '\r' && recvHandshake[valueStart] != '\n')
                        break;
                    ++valueStart;
                }
                if (recvHandshake[valueStart] != ':')
                    continue;
                ++valueStart;
                while (recvHandshake[valueStart] == ' ' || recvHandshake[valueStart] == '\t')
                    ++valueStart;
                FixedString128 value = default;
                int valueEnd = valueStart;
                while (recvHandshake[valueEnd] != '\r')
                {
                    value.Add(recvHandshake[valueEnd]);
                    ++valueEnd;
                }
                // Trim trailing whitespace
                while (value.Length > 0 && (value[value.Length-1] == ' ' || value[value.Length-1] == '\t'))
                    value.Length = value.Length-1;

                headerLookup.TryAdd(key, value);
            }

            FixedString128 keyMagic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            FixedString128 connectionHeader = "connection";
            FixedString128 upgradeHeader = "upgrade";
            FixedString128 headerValue;
            var invalidConnection = !headerLookup.TryGetValue(connectionHeader, out headerValue) || headerValue.Length < 7;
            // Scan for "upgrade" in a coma separated list
            if (!invalidConnection)
            {
                invalidConnection = true;
                int upPos = 0;
                int len = 0;
                while ((len = headerValue.Length - upPos) >= 7)
                {
                    invalidConnection = ((headerValue[upPos+0]|32) != 'u' || (headerValue[upPos+1]|32) != 'p' || (headerValue[upPos+2]|32) != 'g' ||
                        (headerValue[upPos+3]|32) != 'r' || (headerValue[upPos+4]|32) != 'a' || (headerValue[upPos+5]|32) != 'd' || (headerValue[upPos+6]|32) != 'e');
                    if (!invalidConnection)
                    {
                        if (len == 7 || headerValue[upPos+7] == ',' || headerValue[upPos+7] == ' ' || headerValue[upPos+7] == '\t')
                            break;
                        invalidConnection = true;
                    }
                    while (upPos < headerValue.Length && headerValue[upPos] != ',')
                        ++upPos;
                    // Skip ,
                    ++upPos;
                    // skip whitespace
                    while (upPos < headerValue.Length && (headerValue[upPos] == ' ' || headerValue[upPos] == '\t'))
                        ++upPos;
                }
            }
            var invalidUpgrade = (!headerLookup.TryGetValue(upgradeHeader, out headerValue) || headerValue.Length != 9 ||
                (headerValue[0]|32) != 'w' || (headerValue[1]|32) != 'e' || (headerValue[2]|32) != 'b' || (headerValue[3]|32) != 's' || (headerValue[4]|32) != 'o' || (headerValue[5]|32) != 'c' || (headerValue[6]|32) != 'k' || (headerValue[7]|32) != 'e' || (headerValue[8]|32) != 't');
            // Receive handshake, different handshake depending on if PayloadSize is 0 (server) or > 0 (client)
            if (connection.Packet.DataLength > 0)
            {
                var invalidStatusLine = (firstLineEnd < 14 ||
                    recvHandshake[0] != 'H' || recvHandshake[1] != 'T' || recvHandshake[2] != 'T' || recvHandshake[3] != 'P' ||
                    recvHandshake[4] != '/' || recvHandshake[5] != '1' || recvHandshake[6] != '.' || recvHandshake[7] != '1' ||
                    recvHandshake[8] != ' ' || recvHandshake[9] != '1' || recvHandshake[10] != '0' || recvHandshake[11] != '1' ||
                    recvHandshake[12] != ' ');
                if (invalidStatusLine)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnityEngine.Debug.LogWarning("Could not parse http status line.");
#endif
                    return -1;
                }
                FixedString128 protocolHeader = "sec-websocket-protocol";
                FixedString128 extensionHeader = "sec-websocket-extensions";
                FixedString128 acceptHeader = "sec-websocket-accept";
                FixedString128 wsKey;
                if (invalidConnection || invalidUpgrade || headerLookup.ContainsKey(protocolHeader) ||
                    headerLookup.ContainsKey(extensionHeader) || !headerLookup.TryGetValue(acceptHeader, out wsKey))
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (invalidConnection)
                        UnityEngine.Debug.LogWarning("Received handshake with invalid or missing Connection key.");
                    if (invalidUpgrade)
                        UnityEngine.Debug.LogWarning("Received handshake with invalid or missing Upgrade key.");
                    if (headerLookup.ContainsKey(protocolHeader))
                        UnityEngine.Debug.LogWarning("Received handshake with a subprotocol != null.");
                    if (headerLookup.ContainsKey(extensionHeader))
                        UnityEngine.Debug.LogWarning("Received handshake with an extension.");
                    if (!headerLookup.ContainsKey(acceptHeader))
                        UnityEngine.Debug.LogWarning("Received handshake with a missing sec-websocket-accept key.");
#endif
                    return -1;
                }
                // validate the accept header
                FixedString128 refWsKey = default;
                GenerateBase64Key(out var clientKey, connection.Key0, connection.Key1, connection.Key2, connection.Key3);
                refWsKey.Append(clientKey);
                refWsKey.Append(keyMagic);
                var hash = new SHA1(refWsKey);
                clientKey = hash.ToBase64();
                if (wsKey != clientKey)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnityEngine.Debug.LogWarning("Received handshake with incorrect sec-websocket-accept.");
#endif
                    return -1;
                }
                if (connection.PendingSendPacket.DataLength == 0)
                {
                    // Send the pending connect message
                    uint count;
                    fixed (byte* ptr = connection.Packet.Data)
                    {
                        count = Binding.Baselib_Socket_TCP_Send(
                            connection.Socket,
                            (IntPtr)ptr,
                            (uint)connection.Packet.DataLength,
                            &error);
                    }
                    if (count != 0 && count != connection.Packet.DataLength)
                    {
                        // Backup the pending packet for sending later
                        connection.PendingSendPacket.DataLength = connection.Packet.DataLength - (int)count;
                        fixed (byte* dst = connection.PendingSendPacket.Data)
                        fixed (byte* src = connection.Packet.Data)
                        {
                            UnsafeUtility.MemCpy(dst, src + count, connection.PendingSendPacket.DataLength);
                        }
                    }
                }
            }
            else
            {
                FixedString512 handshake;
                FixedString128 hostHeader = "host";
                FixedString128 keyHeader = "sec-websocket-key";
                FixedString128 versionHeader = "sec-websocket-version";
                var invalidRequestLine = (firstLineEnd < 14 || recvHandshake[0] != 'G' || recvHandshake[1] != 'E' || recvHandshake[2] != 'T' || recvHandshake[3] != ' ' ||
                    recvHandshake[firstLineEnd-9] != ' ' ||
                    recvHandshake[firstLineEnd-8] != 'H' || recvHandshake[firstLineEnd-7] != 'T' || recvHandshake[firstLineEnd-6] != 'T' || recvHandshake[firstLineEnd-5] != 'P' ||
                    recvHandshake[firstLineEnd-4] != '/' || recvHandshake[firstLineEnd-3] != '1' || recvHandshake[firstLineEnd-2] != '.' || recvHandshake[firstLineEnd-1] != '1');
                FixedString128 wsKey;
                var invalidVersion = (!headerLookup.TryGetValue(versionHeader, out headerValue) || headerValue.Length != 2 ||
                    headerValue[0] != '1' || headerValue[1] != '3');
                var invalidKey = (!headerLookup.TryGetValue(keyHeader, out wsKey) || wsKey.Length != 24);
                if (invalidRequestLine || !headerLookup.ContainsKey(hostHeader) || invalidKey ||
                    invalidVersion || invalidConnection || invalidUpgrade)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    if (invalidRequestLine)
                        UnityEngine.Debug.LogWarning("Received handshake with invalid http request line.");
                    if (invalidVersion)
                        UnityEngine.Debug.LogWarning("Received handshake with invalid or missing sec-websocket-version key.");
                    if (invalidConnection)
                        UnityEngine.Debug.LogWarning("Received handshake with invalid or missing Connection key.");
                    if (invalidUpgrade)
                        UnityEngine.Debug.LogWarning("Received handshake with invalid or missing Upgrade key.");
                    if (!headerLookup.ContainsKey(hostHeader))
                        UnityEngine.Debug.LogWarning("Received handshake with a missing host key.");
                    if (invalidKey)
                        UnityEngine.Debug.LogWarning("Received handshake with a missing or invalid sec-websocket-key key.");
#endif
                    // Not a valid get request
                    handshake = "HTTP/1.1 400 Bad Request\r\nSec-WebSocket-Version: 13\r\n\r\n";
                    Binding.Baselib_Socket_TCP_Send(
                        connection.Socket,
                        (IntPtr)handshake.GetUnsafePtr(),
                        (uint)handshake.Length,
                        &error);
                    return -1;
                }
                // Only / is available
                if (firstLineEnd != 14 || recvHandshake[4] != '/')
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnityEngine.Debug.LogWarning("Received handshake with an incorrect resource name.");
#endif
                    handshake = "HTTP/1.1 404 Not Found\r\n\r\n";
                    Binding.Baselib_Socket_TCP_Send(
                        connection.Socket,
                        (IntPtr)handshake.GetUnsafePtr(),
                        (uint)handshake.Length,
                        &error);
                    return -1;
                }

                handshake = "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n";
                wsKey.Append(keyMagic);
                var hash = new SHA1(wsKey);
                FixedString128 accept = "Sec-WebSocket-Accept: ";
                handshake.Append(accept);
                handshake.Append(hash.ToBase64());
                FixedString32 end = "\r\n";
                handshake.Append(end);
                handshake.Append(end);
                var count = (int) Binding.Baselib_Socket_TCP_Send(
                    connection.Socket,
                    (IntPtr)handshake.GetUnsafePtr(),
                    (uint)handshake.Length,
                    &error);
                if (error.code != ErrorCode.Success || count != handshake.Length)
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnityEngine.Debug.LogWarning("Failed to send WebSocket server handshake.");
#endif
                    return -1;
                }
            }
            return 1;
        }
#endif
        [ReadOnly]
        NativeHashMap<NetworkInterfaceEndPoint, ConnectionState> m_Connections;

        [ReadOnly]
        private NativeArray<BaselibData> m_Baselib;

        /// <summary>
        /// Returns the local endpoint.
        /// </summary>
        /// <value>NetworkInterfaceEndPoint</value>
        public unsafe NetworkInterfaceEndPoint LocalEndPoint
        {
            // error handling: handle the errors...
            get
            {
                var address = m_Baselib[0].BoundAddress;
                #if !UNITY_WEBGL
                if (m_Baselib[0].ListenSocket.handle != Binding.Baselib_Socket_Handle_Invalid.handle)
                {
                    var error = default(ErrorState);
                    Binding.Baselib_Socket_GetAddress(m_Baselib[0].ListenSocket, (Binding.Baselib_NetworkAddress*)address.data, &error);
                }
                #endif
                return address;
            }
        }

        public bool IsCreated => m_Baselib.IsCreated;

        /// <summary>
        /// Creates a interface endpoint.
        /// </summary>
        /// <value>NetworkInterfaceEndPoint</value>
        public unsafe int CreateInterfaceEndPoint(NetworkEndPoint address, out NetworkInterfaceEndPoint endpoint)
        {
            endpoint.dataLength = address.length;
            fixed (void* ptr = endpoint.data)
                *(Binding.Baselib_NetworkAddress*)ptr = address.rawNetworkAddress;
            return (int) Error.StatusCode.Success;
        }

        public unsafe NetworkEndPoint GetGenericEndPoint(NetworkInterfaceEndPoint endpoint)
        {
            // Set to a valid address so length is set correctly
            var address = NetworkEndPoint.LoopbackIpv4;
            address.rawNetworkAddress = *(Binding.Baselib_NetworkAddress*)endpoint.data;
            address.length = endpoint.dataLength;
            return address;
        }


        /// <summary>
        /// Initializes a instance of the BaselibNetworkInterface struct.
        /// </summary>
        /// <param name="param">An array of INetworkParameter. There is currently only <see cref="BaselibNetworkParameter"/> that can be passed.</param>
        public unsafe int Initialize(params INetworkParameter[] param)
        {
            m_Baselib = new NativeArray<BaselibData>(1, Allocator.Persistent);
            var baselib = default(BaselibData);

            #if !UNITY_WEBGL
            baselib.ListenSocket = Binding.Baselib_Socket_Handle_Invalid;
            #endif
            CreateInterfaceEndPoint(NetworkEndPoint.AnyIpv4, out baselib.BoundAddress);

            m_Baselib[0] = baselib;

            m_Connections = new NativeHashMap<NetworkInterfaceEndPoint, ConnectionState>(1, Allocator.Persistent);
            return 0;
        }

        public unsafe void Dispose()
        {
            #if !UNITY_WEBGL
            if (m_Baselib[0].ListenSocket.handle != Binding.Baselib_Socket_Handle_Invalid.handle)
            {
                #if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_WEBGL
                AllSockets.OpenSockets.Remove(new SocketList.SocketId
                    {socket = m_Baselib[0].ListenSocket});
                #endif
                Binding.Baselib_Socket_Close(m_Baselib[0].ListenSocket);
            }
            WebSocketPacket packet = default;
            #endif
            var keys = m_Connections.GetKeyArray(Allocator.Temp);
            for (int connectionIndex = 0; connectionIndex < keys.Length; ++connectionIndex)
            {
                var connection = m_Connections[keys[connectionIndex]];
                #if !UNITY_WEBGL
                if ((connection.ConnectState&ConnectionSentClose) == 0 && connection.PendingSendPacket.DataLength == 0)
                {
                    var error = default(ErrorState);
                    packet.ConstructClose(1001, (connection.ConnectState&ConnectionIsClient) != 0, new Random((uint)Stopwatch.GetTimestamp()).NextUInt());
                    var count = (int)Binding.Baselib_Socket_TCP_Send(
                        connection.Socket,
                        (IntPtr)packet.Data,
                        (uint)packet.DataLength,
                        &error);
                }
                #endif
                WebSocketDestroy(connection.Socket);
            }

            m_Baselib.Dispose();
            m_Connections.Dispose();
        }

        #region ReceiveJob

#if !UNITY_WEBGL // this job is calling external js methods which currently does not work from burst
        [BurstCompile]
#endif
        struct ReceiveJob : IJob
        {
            public NetworkPacketReceiver Receiver;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<BaselibData> Baselib;
            [NativeDisableContainerSafetyRestriction]
            public NativeHashMap<NetworkInterfaceEndPoint, ConnectionState> Connections;
            public Random rand;

            int headerSize;
            private unsafe bool CopyToStream(in NetworkInterfaceEndPoint address, byte* data, int dataLength)
            {
                if (dataLength < headerSize)
                    return true;

                var stream = Receiver.GetDataStream();
                int dataStreamSize = Receiver.GetDataStreamSize();
                if (Receiver.DynamicDataStreamSize())
                {
                    while (dataStreamSize + dataLength >= stream.Length)
                        stream.ResizeUninitialized(stream.Length*2);
                }
                else if (dataStreamSize + dataLength > stream.Length)
                {
                    Receiver.ReceiveErrorCode = 10040;//(int)ErrorCode.OutOfMemory;
                    return false;
                }

                UnsafeUtility.MemCpy(
                    (byte*)stream.GetUnsafePtr() + dataStreamSize,
                    (byte*)data,
                    dataLength);

                Receiver.ReceiveCount += Receiver.AppendPacket(address, dataLength);
                return true;
            }

            public unsafe void Execute()
            {
                #if !UNITY_WEBGL
                if (Baselib[0].ListenSocket.handle != Binding.Baselib_Socket_Handle_Invalid.handle)
                {
                    var error = default(ErrorState);
                    var socket = Binding.Baselib_Socket_TCP_Accept(Baselib[0].ListenSocket, &error);

                    if (socket.handle != Binding.Baselib_Socket_Handle_Invalid.handle)
                    {
                        var address = Baselib[0].BoundAddress;
                        Binding.Baselib_Socket_GetAddress(socket, (Binding.Baselib_NetworkAddress*)address.data, &error);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        if (error.code != ErrorCode.Success)
                            UnityEngine.Debug.LogWarning($"Failed to get address {error.code}");
#endif
                        while (!Connections.TryAdd(address, new ConnectionState
                        {
                            Socket = socket,
                            ConnectState = 1
                        }))
                        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                            UnityEngine.Debug.LogWarning("Duplicate address - test next");
#endif
                            // FIXME: HACK: probe for another address which is unique
                            ushort* port = (ushort*)&(((Binding.Baselib_NetworkAddress*)address.data)->port0);
                            *port = (ushort)(*port + 1);
                        }
                        // The client is responsible to send a UTP connect message which will be propagated to the driver
                    }
                }
                #endif
                headerSize = UnsafeUtility.SizeOf<UdpCHeader>();
                var keys = Connections.GetKeyArray(Allocator.Temp);
                WebSocketPacket packet = default;
                for (int connectionIndex = 0; connectionIndex < keys.Length; ++connectionIndex)
                {
                    var address = keys[connectionIndex];
                    var connection = Connections[address];

#if !UNITY_WEBGL
                    // Process any pending partial send
                    if (connection.PendingSendPacket.DataLength > 0)
                    {
                        var error = default(ErrorState);
                        var count = (int) Binding.Baselib_Socket_TCP_Send(
                            connection.Socket,
                            (IntPtr)connection.PendingSendPacket.Data,
                            (uint)connection.PendingSendPacket.DataLength,
                            &error);
                        if (count == connection.PendingSendPacket.DataLength)
                        {
                            connection.PendingSendPacket.DataLength = 0;
                            Connections[address] = connection;
                        }
                        else if (count > 0)
                        {
                            UnsafeUtility.MemMove(connection.PendingSendPacket.Data, connection.PendingSendPacket.Data + count, connection.PendingSendPacket.DataLength - count);
                            connection.PendingSendPacket.DataLength -= count;
                            Connections[address] = connection;
                        }
                    }
#endif
                    // Detect if the driver has removed this connection and clean up in case we missed a disconnect or timeout
                    if ((connection.ConnectState&ConnectionKnownByDriver) != 0 && !Receiver.IsAddressUsed(address))
                    {
#if !UNITY_WEBGL
                        if ((connection.ConnectState&ConnectionStateMask) >= 2 && connection.PendingSendPacket.DataLength == 0)
                        {
                            if ((connection.ConnectState&ConnectionSentClose) == 0)
                            {
                                var error = default(ErrorState);
                                packet.ConstructClose(1000, (connection.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                                var count = (int)Binding.Baselib_Socket_TCP_Send(
                                    connection.Socket,
                                    (IntPtr)packet.Data,
                                    (uint)packet.DataLength,
                                    &error);
                                if (count != packet.DataLength || error.code != ErrorCode.Success)
                                {
                                    WebSocketDestroy(connection.Socket);
                                    Connections.Remove(address);
                                }
                                else
                                {
                                    connection.ConnectState |= ConnectionSentClose;
                                    Connections[address] = connection;
                                }
                            }
                        }
                        else
#endif
                        {
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                            continue;
                        }
                    }
#if !UNITY_WEBGL
                    if ((connection.ConnectState&ConnectionStateMask) == 3)
                    {
                        // Client waiting for server to close connection
                        // If server did not close the connection within 30 sec the client can close it
                        if (connection.CloseTimeStamp - Receiver.LastUpdateTime > 30*1000)
                        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                            UnityEngine.Debug.LogWarning("Server did not close the socket fast enough, closing it on the client");
#endif
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                        }
                        // Read and throw away just to check for disconnect messages, if more data was sent after websoket close - just close the socket
                        var error = default(ErrorState);
                        var receivedBytes = Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)packet.Data, (uint)(math.min(connection.RemainingPacketSize, NetworkParameterConstants.MTU)), &error);
                        if (Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)packet.Data, (uint)(math.min(connection.RemainingPacketSize, NetworkParameterConstants.MTU)), &error) > 0
                            || error.code != ErrorCode.Success)
                        {
                            // Disconnected
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                        }
                        continue;
                    }
                    if ((connection.ConnectState&ConnectionSentClose) != 0)
                    {
                        // If we sent a close request and did not receive a reply within 30 sec just close the connection
                        if (connection.CloseTimeStamp == 0)
                        {
                            connection.CloseTimeStamp = Receiver.LastUpdateTime;
                            Connections[address] = connection;
                        }
                        else if (connection.CloseTimeStamp - Receiver.LastUpdateTime > 30*1000)
                        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                            UnityEngine.Debug.LogWarning("Remote end did not send a websocket close handshake fast enough, closing socket");
#endif
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                            continue;
                        }
                    }
#endif
                    if ((connection.ConnectState&ConnectionStateMask) == 0)
                    {
                        var conState = WebSocketIsConnected(connection.Socket
#if !UNITY_WEBGL
                            , address, connection.Key0, connection.Key1, connection.Key2, connection.Key3
#endif
                        );
                        if (conState == 0)
                            continue;
                        if (conState < 0)
                        {
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                            continue;
                        }
                        ++connection.ConnectState;
                        Connections[address] = connection;
                    }
                    if ((connection.ConnectState&ConnectionStateMask) == 1)
                    {
#if !UNITY_WEBGL
                        var conState = WebSocketIsHandshakeComplete(ref connection);
                        Connections[address] = connection;
                        if (conState == 0)
                            continue;
                        if (conState < 0)
                        {
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                            continue;
                        }
#endif
                        ++connection.ConnectState;
                        Connections[address] = connection;
                    }
#if UNITY_WEBGL
                    while ((packet.DataLength = WebSocketRecv(connection.Socket, (IntPtr)packet.Data, NetworkParameterConstants.MTU)) != 0)
                    {
                        if (packet.DataLength < 0)
                        {
                            // Disconnected
                            WebSocketDestroy(connection.Socket);
                            Connections.Remove(address);
                            break;
                        }
                        if (!CopyToStream(address, packet.Data, packet.DataLength))
                        {
                            Connections[address] = connection;
                            break;
                        }
                        connection.ConnectState |= ConnectionKnownByDriver;
                    }
#else
                    bool pendingData = true;
                    while (pendingData)
                    {
                        // Time to read
                        if (connection.RemainingPacketSize > 0 && connection.PacketStart > 0)
                        {
                            var error = default(ErrorState);
                            var receivedBytes = Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)connection.Packet.Data+connection.Packet.DataLength, (uint)connection.RemainingPacketSize, &error);
                            pendingData = receivedBytes>0;
                            if (error.code != ErrorCode.Success)
                            {
                                // Disconnected
                                WebSocketDestroy(connection.Socket);
                                Connections.Remove(address);
                                break;
                            }
                            connection.RemainingPacketSize -= receivedBytes;
                            connection.Packet.DataLength += (int)receivedBytes;
                            if (connection.RemainingPacketSize == 0)
                            {
                                if ((connection.Packet.Data[1]&0x80)!=0)
                                {
                                    var maskBytes = connection.Packet.Data+connection.HeaderSize-4;
                                    var wsPayload = connection.Packet.Data+connection.PacketStart;
                                    int maskLen = connection.Packet.DataLength - connection.PacketStart;
                                    for (int i = 0; i < maskLen; ++i)
                                        wsPayload[i] = (byte)(wsPayload[i]^maskBytes[i&3]);
                                }
                                connection.HeaderSize = 0;
                                connection.PacketStart = 0;
                                // This was not the final fragment, so wait for more
                                if ((connection.Packet.Data[0]&0x80) == 0)
                                {
                                    Connections[address] = connection;
                                    continue;
                                }
                                if (!CopyToStream(address, connection.Packet.Data + WebSocketMaxHeaderSize, connection.Packet.DataLength - WebSocketMaxHeaderSize))
                                {
                                    Connections[address] = connection;
                                    break;
                                }
                                connection.ConnectState |= ConnectionKnownByDriver;
                            }
                        }
                        else if (connection.RemainingPacketSize > 0)
                        {
                            // Read and throw away
                            var error = default(ErrorState);
                            var receivedBytes = Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)packet.Data, (uint)(math.min(connection.RemainingPacketSize, NetworkParameterConstants.MTU)), &error);
                            pendingData = receivedBytes>0;
                            if (error.code != ErrorCode.Success)
                            {
                                // Disconnected
                                WebSocketDestroy(connection.Socket);
                                Connections.Remove(address);
                                break;
                            }
                            connection.RemainingPacketSize -= receivedBytes;
                        }
                        else
                        {
                            // No remaining packet or skip size, so we need to read a new frame header
                            var error = default(ErrorState);
                            if (connection.HeaderSize < 2)
                            {
                                int receivedBytes = (int)Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)connection.Packet.Data + connection.HeaderSize, (uint)(2 - connection.HeaderSize), &error);
                                pendingData = receivedBytes>0;
                                if (error.code != ErrorCode.Success)
                                {
                                    // Disconnected
                                    WebSocketDestroy(connection.Socket);
                                    Connections.Remove(address);
                                    break;
                                }
                                connection.HeaderSize += receivedBytes;
                                if (connection.HeaderSize < 2)
                                {
                                    Connections[address] = connection;
                                    continue;
                                }
                            }
                            // Calculate the header size
                            int payloadByte = connection.Packet.Data[1]&0x7f;
                            var wsHeaderSize = 2;
                            if ((connection.Packet.Data[1]&0x80) != 0)
                                wsHeaderSize += 4;
                            if (payloadByte == 126)
                                wsHeaderSize += 2;
                            else if (payloadByte == 127)
                                wsHeaderSize += 8;
                            if (connection.HeaderSize < wsHeaderSize)
                            {
                                int receivedBytes = (int)Binding.Baselib_Socket_TCP_Recv(connection.Socket, (IntPtr)connection.Packet.Data + connection.HeaderSize, (uint)(wsHeaderSize - connection.HeaderSize), &error);
                                pendingData = receivedBytes>0;
                                if (error.code != ErrorCode.Success)
                                {
                                    // Disconnected
                                    WebSocketDestroy(connection.Socket);
                                    Connections.Remove(address);
                                    break;
                                }
                                connection.HeaderSize += receivedBytes;
                                if (connection.HeaderSize < wsHeaderSize)
                                {
                                    Connections[address] = connection;
                                    continue;
                                }
                            }
                            // Full header is available - figure out how big the payload is
                            ulong payloadSize = 0;
                            if (payloadByte == 127)
                            {
                                payloadSize = ((ulong)connection.Packet.Data[6]<<56) + ((ulong)connection.Packet.Data[7]<<48) +
                                    ((ulong)connection.Packet.Data[6]<<40) + ((ulong)connection.Packet.Data[7]<<32) +
                                    ((ulong)connection.Packet.Data[6]<<24) + ((ulong)connection.Packet.Data[7]<<16) +
                                    ((ulong)connection.Packet.Data[8]<<8) + (ulong)connection.Packet.Data[9];                            }
                            else if (payloadByte == 126)
                            {
                                payloadSize = ((ulong)connection.Packet.Data[2]<<8) + connection.Packet.Data[3];
                            }
                            else
                                payloadSize = (ulong)payloadByte;
                            var masked = (connection.Packet.Data[1] & 0x80) != 0;
                            var isClient = (connection.ConnectState&ConnectionIsClient) != 0;
                            // Receiving a masked message on the client is an error, receiving an unmasked message on the server is an error
                            if ((connection.Packet.Data[0] & 0x70) != 0 || masked == isClient)
                            {
                                // Bad header
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                if ((connection.Packet.Data[0] & 0x70) != 0)
                                    UnityEngine.Debug.LogWarning("Received message with invalid reserved header bits");
                                if (masked == isClient)
                                    UnityEngine.Debug.LogWarning("Received message with unexpected masking");
#endif
                                if ((connection.ConnectState&ConnectionSentClose) == 0 && connection.PendingSendPacket.DataLength == 0)
                                {
                                    packet.ConstructClose(1002, (connection.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                                    Binding.Baselib_Socket_TCP_Send(
                                        connection.Socket,
                                        (IntPtr)packet.Data,
                                        (uint)packet.DataLength,
                                        &error);
                                }
                                WebSocketDestroy(connection.Socket);
                                Connections.Remove(address);
                                break;
                            }
                            var opcode = connection.Packet.Data[0] & 0xf;
                            connection.HeaderSize = 0;
                            connection.RemainingPacketSize = payloadSize;
                            if (opcode == 0)
                            {
                                // Continuation
                                // validate that there is an actual packet to continue and that it's binary
                                // and that the full packet fits in an MTU
                                if (connection.Packet.DataLength < WebSocketMaxHeaderSize ||
                                    (ulong)connection.Packet.DataLength + payloadSize > (ulong)NetworkParameterConstants.MTU)
                                {
                                    connection.Packet.DataLength = 0;
                                }
                                else
                                {
                                    connection.PacketStart = connection.Packet.DataLength;
                                    connection.HeaderSize = wsHeaderSize;
                                }
                            }
                            else if (opcode == 2)
                            {
                                // Binary
                                // if dataLength is > 0 we probably never got the final part of a message which is a protocol error - but utp is not reliable so just drop it
                                // Reset the packet length, otherwise we'll just append data to it
                                if (payloadSize <= (ulong)NetworkParameterConstants.MTU)
                                {
                                    connection.Packet.DataLength = WebSocketMaxHeaderSize;
                                    connection.PacketStart = connection.Packet.DataLength;
                                    connection.HeaderSize = wsHeaderSize;
                                }
                            }
                            else if (opcode == 8)
                            {
                                // Close
                                if ((connection.ConnectState&ConnectionSentClose) == 0)
                                {
                                    // FIXME: should echo the status code
                                    int count = -1;
                                    if (connection.PendingSendPacket.DataLength == 0)
                                    {
                                        packet.ConstructClose(1000, (connection.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                                        count = (int)Binding.Baselib_Socket_TCP_Send(
                                            connection.Socket,
                                            (IntPtr)packet.Data,
                                            (uint)packet.DataLength,
                                            &error);
                                    }
                                    if (count != packet.DataLength || error.code != ErrorCode.Success)
                                    {
                                        WebSocketDestroy(connection.Socket);
                                        Connections.Remove(address);
                                        break;
                                    }
                                    connection.ConnectState |= ConnectionSentClose;
                                }
                                if (isClient)
                                {
                                    connection.ConnectState = (connection.ConnectState&(~ConnectionStateMask)) | 3;
                                    connection.CloseTimeStamp = Receiver.LastUpdateTime;
                                    Connections[address] = connection;
                                }
                                else
                                {
                                    WebSocketDestroy(connection.Socket);
                                    Connections.Remove(address);
                                }
                                break;
                            }
                            else if (opcode == 9)
                            {
                                // Ping
                                if ((connection.ConnectState&ConnectionSentClose) == 0 && connection.PendingSendPacket.DataLength == 0)
                                {
                                    // FIXME: should echo the ping payload
                                    packet.ConstructPong(null, 0, (connection.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                                    var count = (int)Binding.Baselib_Socket_TCP_Send(
                                        connection.Socket,
                                        (IntPtr)packet.Data,
                                        (uint)packet.DataLength,
                                        &error);
                                    if (count != 0 && count != packet.DataLength)
                                    {
                                        // Backup the pending packet for sending later
                                        connection.PendingSendPacket.DataLength = packet.DataLength - count;
                                        UnsafeUtility.MemCpy(connection.PendingSendPacket.Data, packet.Data + count, connection.PendingSendPacket.DataLength);
                                    }
                                }
                            }
                            else if (opcode == 10)
                            {
                                // Pong
                            }
                            else
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                UnityEngine.Debug.LogWarning("Received message with an unsupported opcode");
#endif
                                // Unsupported opcode
                                if ((connection.ConnectState&ConnectionSentClose) == 0 && connection.PendingSendPacket.DataLength == 0)
                                {
                                    packet.ConstructClose((ushort)((opcode==1) ? 1003 : 1002), (connection.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                                    Binding.Baselib_Socket_TCP_Send(
                                        connection.Socket,
                                        (IntPtr)packet.Data,
                                        (uint)packet.DataLength,
                                        &error);
                                }
                                WebSocketDestroy(connection.Socket);
                                Connections.Remove(address);
                                break;
                            }
                        }
                        Connections[address] = connection;
                    }
#endif
                }
            }
        }
        #endregion

        public JobHandle ScheduleReceive(NetworkPacketReceiver receiver, JobHandle dep)
        {
            var job = new ReceiveJob
            {
                Receiver = receiver,
                Baselib = m_Baselib,
                Connections = m_Connections,
                rand = new Random((uint)Stopwatch.GetTimestamp())
            };
            return job.Schedule(dep);
        }

#if !UNITY_WEBGL // this job is calling external js methods which currently does not work from burst
        [BurstCompile]
#endif
        unsafe struct SendJob : IJob
        {
            public NativeQueue<QueuedSendMessage> sendQueue;
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<BaselibData> Baselib;
            [NativeDisableContainerSafetyRestriction]
            public NativeHashMap<NetworkInterfaceEndPoint, ConnectionState> Connections;
            public Random rand;
            public void Execute()
            {
                var baselib = Baselib[0];
                #if !UNITY_WEBGL
                WebSocketPacket packet = default;
                #endif
                while (sendQueue.TryDequeue(out var msg))
                {
                    if (msg.DataLength < UnsafeUtility.SizeOf<UdpCHeader>())
                        continue;

                    bool hasConnection = Connections.TryGetValue(msg.Dest, out var state);
                    var header = (UdpCHeader*)msg.Data;
                    if (header->Type == (byte)UdpCProtocol.ConnectionRequest)
                    {
                        if (hasConnection && (state.ConnectState&ConnectionSentClose) != 0)
                        {
                            // Reconnecting while the connection is still being closed, just force close it
                            WebSocketDestroy(state.Socket);
                            Connections.Remove(msg.Dest);
                            hasConnection = false;
                        }
                        if (!hasConnection)
                        {
                            Binding.Baselib_NetworkAddress* address = (Binding.Baselib_NetworkAddress*)msg.Dest.data;
                            #if UNITY_WEBGL
                            var addressString = NetworkEndPoint.AddressToString(*address);

                            WebSocketCreate(s_NextSocketId, (IntPtr)addressString.GetUnsafePtr(), addressString.Length, (IntPtr)msg.Data, msg.DataLength);
                            var conState = new ConnectionState
                            {
                                Socket = s_NextSocketId,
                                ConnectState = ConnectionKnownByDriver | ConnectionIsClient
                            };
                            ++s_NextSocketId;
                            Connections.TryAdd(msg.Dest, conState);
                            #else
                            var error = default(ErrorState);
                            var socket = Binding.Baselib_Socket_Create(
                                (Binding.Baselib_NetworkAddress_Family)address->family, Binding.Baselib_Socket_Protocol.TCP,
                                &error);
                            if (error.code != ErrorCode.Success)
                                continue;
                            Binding.Baselib_Socket_TCP_Connect(socket, address, Binding.Baselib_NetworkAddress_AddressReuse.Allow, &error);
                            if (error.code != ErrorCode.Success)
                            {
                                Binding.Baselib_Socket_Close(socket);
                                continue;
                            }
                            var conState = new ConnectionState
                            {
                                Socket = socket,
                                ConnectState = ConnectionKnownByDriver | ConnectionIsClient,
                                Key0 = rand.NextUInt(),
                                Key1 = rand.NextUInt(),
                                Key2 = rand.NextUInt(),
                                Key3 = rand.NextUInt()
                            };
                            conState.Packet.ConstructBinary(msg.Data, msg.DataLength, true, rand.NextUInt());
                            Connections.TryAdd(msg.Dest, conState);
                            #endif
                        }
                    }
                    else if (hasConnection)
                    {
                        if ((state.ConnectState&ConnectionSentClose) != 0)
                            continue;
                        #if UNITY_WEBGL
                        WebSocketSend(state.Socket, (IntPtr)msg.Data, msg.DataLength);
                        if (header->Type == (byte)UdpCProtocol.Disconnect)
                        {
                            WebSocketDestroy(state.Socket);
                            Connections.Remove(msg.Dest);
                        }
                        #else
                        var error = default(ErrorState);
                        int count = 0;
                        if (state.PendingSendPacket.DataLength == 0)
                        {
                            packet.ConstructBinary(msg.Data, msg.DataLength, (state.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                            count = (int) Binding.Baselib_Socket_TCP_Send(
                                state.Socket,
                                (IntPtr)packet.Data,
                                (uint)packet.DataLength,
                                &error);
                            if (count != 0 && count != packet.DataLength)
                            {
                                // Backup the pending packet for sending later
                                state.PendingSendPacket.DataLength = packet.DataLength - count;
                                UnsafeUtility.MemCpy(state.PendingSendPacket.Data, packet.Data + count, state.PendingSendPacket.DataLength);
                                Connections[msg.Dest] = state;
                            }
                        }
                        if (header->Type == (byte)UdpCProtocol.Disconnect)
                        {
                            count = -1;
                            if (state.PendingSendPacket.DataLength == 0)
                            {
                                packet.ConstructClose(1000, (state.ConnectState&ConnectionIsClient) != 0, rand.NextUInt());
                                count = (int)Binding.Baselib_Socket_TCP_Send(
                                    state.Socket,
                                    (IntPtr)packet.Data,
                                    (uint)packet.DataLength,
                                    &error);
                            }
                            if (count != packet.DataLength || error.code != ErrorCode.Success)
                            {
                                WebSocketDestroy(state.Socket);
                                Connections.Remove(msg.Dest);
                            }
                            else
                            {
                                state.ConnectState |= ConnectionSentClose;
                                Connections[msg.Dest] = state;
                            }
                        }
                        #endif
                    }
                }
            }
        }
        public JobHandle ScheduleSend(NativeQueue<QueuedSendMessage> sendQueue, JobHandle dep)
        {
            var sendJob = new SendJob
            {
                sendQueue = sendQueue,
                Baselib = m_Baselib,
                Connections = m_Connections,
                rand = new Random((uint)Stopwatch.GetTimestamp())
            };
            return sendJob.Schedule(dep);
        }


        /// <summary>
        /// Binds the BaselibNetworkInterface to the endpoint passed.
        /// </summary>
        /// <param name="endpoint">A valid ipv4 or ipv6 address</param>
        /// <value>int</value>
        public unsafe int Bind(NetworkInterfaceEndPoint endpoint)
        {
            var baselib = m_Baselib[0];
            baselib.BoundAddress = endpoint;
            m_Baselib[0] = baselib;
            return 0;
        }
        public unsafe int Listen()
        {
            #if UNITY_WEBGL
            throw new InvalidOperationException("WebGL does not support listening for connections");
            #else
            var baselib = m_Baselib[0];

            var error = default(ErrorState);
            Binding.Baselib_NetworkAddress* address = (Binding.Baselib_NetworkAddress*)baselib.BoundAddress.data;

            baselib.ListenSocket = Binding.Baselib_Socket_Create(
                (Binding.Baselib_NetworkAddress_Family)address->family, Binding.Baselib_Socket_Protocol.TCP,
                &error);
            if (error.code != ErrorCode.Success)
                return (int) error.code == -1 ? -1 : -(int) error.code;
            Binding.Baselib_Socket_Bind(baselib.ListenSocket, address, Binding.Baselib_NetworkAddress_AddressReuse.Allow, &error);
            if (error.code == ErrorCode.Success)
            {
                Binding.Baselib_Socket_TCP_Listen(baselib.ListenSocket, &error);
            }

            if (error.code != ErrorCode.Success)
            {
                Binding.Baselib_Socket_Close(baselib.ListenSocket);
                return (int) error.code == -1 ? -1 : -(int) error.code;
            }

            // Update the bound address
            Binding.Baselib_Socket_GetAddress(baselib.ListenSocket, address, &error);

#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_WEBGL
            AllSockets.OpenSockets.Add(new SocketList.SocketId
                {socket = baselib.ListenSocket});
#endif
            m_Baselib[0] = baselib;
            return 0;
            #endif
        }

        static TransportFunctionPointer<NetworkSendInterface.BeginSendMessageDelegate> BeginSendMessageFunctionPointer = new TransportFunctionPointer<NetworkSendInterface.BeginSendMessageDelegate>(BeginSendMessage);
        static TransportFunctionPointer<NetworkSendInterface.EndSendMessageDelegate> EndSendMessageFunctionPointer = new TransportFunctionPointer<NetworkSendInterface.EndSendMessageDelegate>(EndSendMessage);
        static TransportFunctionPointer<NetworkSendInterface.AbortSendMessageDelegate> AbortSendMessageFunctionPointer = new TransportFunctionPointer<NetworkSendInterface.AbortSendMessageDelegate>(AbortSendMessage);

        public unsafe NetworkSendInterface CreateSendInterface()
        {
            return new NetworkSendInterface
            {
                BeginSendMessage = BeginSendMessageFunctionPointer,
                EndSendMessage = EndSendMessageFunctionPointer,
                AbortSendMessage = AbortSendMessageFunctionPointer,
                UserData = (IntPtr)m_Baselib.GetUnsafePtr()
            };
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(NetworkSendInterface.BeginSendMessageDelegate))]
        private static unsafe int BeginSendMessage(out NetworkInterfaceSendHandle handle, IntPtr userData, int requiredPayloadSize)
        {
            handle.id = 0;
            handle.size = 0;
            handle.capacity = requiredPayloadSize;
            handle.data = (IntPtr)UnsafeUtility.Malloc(handle.capacity, 8, Allocator.Temp);
            handle.flags = default;
            return 0;
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(NetworkSendInterface.EndSendMessageDelegate))]
        private static unsafe int EndSendMessage(ref NetworkInterfaceSendHandle handle, ref NetworkInterfaceEndPoint address, IntPtr userData, ref NetworkSendQueueHandle sendQueueHandle)
        {
            var sendQueue = sendQueueHandle.FromHandle();
            var msg = default(QueuedSendMessage);
            msg.Dest = address;
            msg.DataLength = handle.size;
            UnsafeUtility.MemCpy(msg.Data, (void*)handle.data, handle.size);
            sendQueue.Enqueue(msg);
            return handle.size;
        }

        [BurstCompile(DisableDirectCall = true)]
        [AOT.MonoPInvokeCallback(typeof(NetworkSendInterface.AbortSendMessageDelegate))]
        private static unsafe void AbortSendMessage(ref NetworkInterfaceSendHandle handle, IntPtr userData)
        {
        }
    }
}
