using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Networking.Transport.Utilities;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Networking.Transport.Protocols;
using Random = Unity.Mathematics.Random;

namespace Unity.Networking.Transport
{
    internal struct IPCManager
    {
        public static IPCManager Instance = new IPCManager();

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct IPCData
        {
            [FieldOffset(0)] public int from;
            [FieldOffset(4)] public int length;
            [FieldOffset(8)] public fixed byte data[NetworkParameterConstants.MTU];
        }

        private NativeMultiQueue<IPCData> m_IPCQueue;
        private NativeHashMap<ushort, int> m_IPCChannels;

        internal static JobHandle ManagerAccessHandle;

        public bool IsCreated => m_IPCQueue.IsCreated;

        private int m_RefCount;

        public void AddRef()
        {
            if (m_RefCount == 0)
            {
                m_IPCQueue = new NativeMultiQueue<IPCData>(128);
                m_IPCChannels = new NativeHashMap<ushort, int>(64, Allocator.Persistent);
            }
            ++m_RefCount;
        }

        public void Release()
        {
            --m_RefCount;
            if (m_RefCount == 0)
            {
                ManagerAccessHandle.Complete();
                m_IPCQueue.Dispose();
                m_IPCChannels.Dispose();
            }
        }

        internal unsafe void Update(NetworkInterfaceEndPoint local, NativeQueue<QueuedSendMessage> queue)
        {
            QueuedSendMessage val;
            while (queue.TryDequeue(out val))
            {
                var ipcData = new IPCData();
                UnsafeUtility.MemCpy(ipcData.data, val.Data, val.DataLength);
                ipcData.length = val.DataLength;
                ipcData.from = *(int*)local.data;
                m_IPCQueue.Enqueue(*(int*)val.Dest.data, ipcData);
            }
        }

        public unsafe NetworkInterfaceEndPoint CreateEndPoint(ushort port)
        {
            ManagerAccessHandle.Complete();
            int id = 0;
            if (port == 0)
            {
                while (id == 0)
                {
                    port = RandomHelpers.GetRandomUShort();
                    if (!m_IPCChannels.TryGetValue(port, out _))
                    {
                        id = m_IPCChannels.Count() + 1;
                        m_IPCChannels.TryAdd(port, id);
                    }
                }

            }
            else
            {
                if (!m_IPCChannels.TryGetValue(port, out id))
                {
                    id = m_IPCChannels.Count() + 1;
                    m_IPCChannels.TryAdd(port, id);
                }
            }

            var endpoint = default(NetworkInterfaceEndPoint);
            endpoint.dataLength = 4;
            *(int*) endpoint.data = id;

            return endpoint;
        }
        public unsafe bool GetEndPointPort(NetworkInterfaceEndPoint ep, out ushort port)
        {
            ManagerAccessHandle.Complete();
            int id = *(int*) ep.data;
            var values = m_IPCChannels.GetValueArray(Allocator.Temp);
            var keys = m_IPCChannels.GetKeyArray(Allocator.Temp);
            port = 0;
            for (var i = 0; i < m_IPCChannels.Count(); ++i)
            {
                if (values[i] == id)
                {
                    port = keys[i];
                    return true;
                }
            }

            return false;
        }

        public unsafe int PeekNext(NetworkInterfaceEndPoint local, void* slice, out int length, out NetworkInterfaceEndPoint from)
        {
            ManagerAccessHandle.Complete();
            IPCData data;
            from = default(NetworkInterfaceEndPoint);
            length = 0;

            if (m_IPCQueue.Peek(*(int*)local.data, out data))
            {
                UnsafeUtility.MemCpy(slice, data.data, data.length);

                length = data.length;
            }

            GetEndPointByHandle(data.from, out from);

            return length;
        }

        public unsafe int ReceiveMessageEx(NetworkInterfaceEndPoint local, void* payloadData, int payloadLen, ref NetworkInterfaceEndPoint remote)
        {
            IPCData data;
            if (!m_IPCQueue.Peek(*(int*)local.data, out data))
                return 0;
            GetEndPointByHandle(data.from, out remote);

            var totalLength = Math.Min(payloadLen, data.length);
            UnsafeUtility.MemCpy(payloadData, data.data, totalLength);

            if (totalLength < data.length)
                return -1;
            m_IPCQueue.Dequeue(*(int*)local.data, out data);

            return totalLength;
        }

        private unsafe void GetEndPointByHandle(int handle, out NetworkInterfaceEndPoint endpoint)
        {
            var temp = default(NetworkInterfaceEndPoint);
            temp.dataLength = 4;
            *(int*)temp.data = handle;

            endpoint = temp;
        }
    }
}