using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Diagnostics;
using Unity.Burst;

namespace Unity.Networking.Transport
{
    [StructLayout(LayoutKind.Explicit)]
    internal struct UIntFloat
    {
        [FieldOffset(0)] public float floatValue;

        [FieldOffset(0)] public uint intValue;

        [FieldOffset(0)] public double doubleValue;

        [FieldOffset(0)] public ulong longValue;
    }

    /// <summary>
    /// Data streams can be used to serialize data over the network. The
    /// <c>DataStreamWriter</c> and <c>DataStreamReader</c> classes work together
    /// to serialize data for sending and then to deserialize when receiving.
    /// </summary>
    /// <remarks>
    /// The reader can be used to deserialize the data from a NativeArray<byte>, writing data
    /// to a NativeArray<byte> and reading it back can be done like this:
    /// <code>
    /// using (var data = new NativeArray<byte>(16, Allocator.Persistent))
    /// {
    ///     var dataWriter = new DataStreamWriter(data);
    ///     dataWriter.WriteInt(42);
    ///     dataWriter.WriteInt(1234);
    ///     // Length is the actual amount of data inside the writer,
    ///     // Capacity is the total amount.
    ///     var dataReader = new DataStreamReader(nativeArrayOfBytes.GetSubArray(0, dataWriter.Length));
    ///     var myFirstInt = dataReader.ReadInt();
    ///     var mySecondInt = dataReader.ReadInt();
    /// }
    /// </code>
    ///
    /// There are a number of functions for various data types. If a copy of the writer
    /// is stored it can be used to overwrite the data later on, this is particularly useful when
    /// the size of the data is written at the start and you want to write it at
    /// the end when you know the value.
    ///
    /// <code>
    /// using (var data = new NativeArray<byte>(16, Allocator.Persistent))
    /// {
    ///     var dataWriter = new DataStreamWriter(data);
    ///     // My header data
    ///     var headerSizeMark = dataWriter;
    ///     dataWriter.WriteUShort((ushort)0);
    ///     var payloadSizeMark = dataWriter;
    ///     dataWriter.WriteUShort((ushort)0);
    ///     dataWriter.WriteInt(42);
    ///     dataWriter.WriteInt(1234);
    ///     var headerSize = data.Length;
    ///     // Update header size to correct value
    ///     headerSizeMark.WriteUShort((ushort)headerSize);
    ///     // My payload data
    ///     byte[] someBytes = Encoding.ASCII.GetBytes("some string");
    ///     dataWriter.Write(someBytes, someBytes.Length);
    ///     // Update payload size to correct value
    ///     payloadSizeMark.WriteUShort((ushort)(dataWriter.Length - headerSize));
    /// }
    /// </code>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DataStreamWriter
    {
        struct IsLittleEndianStructKey { }
        private static readonly SharedStatic<int> m_IsLittleEndian = SharedStatic<int>.GetOrCreate<IsLittleEndianStructKey>();
        public static bool IsLittleEndian
        {
            get
            {
                if (m_IsLittleEndian.Data == 0)
                {
                    uint test = 1;
                    byte* testPtr = (byte*) &test;
                    m_IsLittleEndian.Data = testPtr[0] == 1 ? 1 : 2;
                }
                return m_IsLittleEndian.Data == 1;
            }
        }

        struct StreamData
        {
            public byte* buffer;
            public int length;
            public int capacity;
            public ulong bitBuffer;
            public int bitIndex;
            public int failedWrites;
        }

        [NativeDisableUnsafePtrRestriction] StreamData m_Data;
        internal IntPtr m_SendHandleData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
#endif

        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct.
        /// </summary>
        /// <param name="length">The length of the buffer.</param>
        /// <param name="allocator">The <see cref="Allocator"/> used to allocate the memory.</param>
        public DataStreamWriter(int length, Allocator allocator)
        {
            CheckAllocator(allocator);
            Initialize(out this, new NativeArray<byte>(length, allocator));
        }

        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct with a NativeArray{byte}
        /// </summary>
        /// <param name="data">The buffer we want to attach to our DataStreamWriter.</param>
        public DataStreamWriter(NativeArray<byte> data)
        {
            Initialize(out this, data);
        }

        /// <summary>
        /// Initializes a new instance of the DataStreamWriter struct with a memory we don't own
        /// </summary>
        /// <param name="data">Pointer to the data</param>
        /// <param name="length">Length of the data</param>
        public DataStreamWriter(byte* data, int length)
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, length, Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            Initialize(out this, na);
        }

        public NativeArray<byte> AsNativeArray()
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(m_Data.buffer, Length, Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, m_Safety);
#endif
            return na;
        }
        private static void Initialize(out DataStreamWriter self, NativeArray<byte> data)
        {
            self.m_SendHandleData = IntPtr.Zero;

            self.m_Data.capacity = data.Length;
            self.m_Data.length = 0;
            self.m_Data.buffer = (byte*)data.GetUnsafePtr();
            self.m_Data.bitBuffer = 0;
            self.m_Data.bitIndex = 0;
            self.m_Data.failedWrites = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            self.m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(data);
#endif
        }

        private static short ByteSwap(short val)
        {
            return (short)(((val & 0xff) << 8) | ((val >> 8)&0xff));
        }
        private static int ByteSwap(int val)
        {
            return (int)(((val & 0xff) << 24) |((val&0xff00)<<8) | ((val>>8)&0xff00) | ((val >> 24)&0xff));
        }

        /// <summary>
        /// True if there is a valid data buffer present. This would be false
        /// if the writer was created with no arguments.
        /// </summary>
        public bool IsCreated
        {
            get { return m_Data.buffer != null; }
        }

        public bool HasFailedWrites => m_Data.failedWrites > 0;

        /// <summary>
        /// The total size of the data buffer, see <see cref="Length"/> for
        /// the size of space used in the buffer.
        /// </summary>
        public int Capacity
        {
            get
            {
                CheckRead();
                return m_Data.capacity;
            }
        }

        /// <summary>
        /// The size of the buffer used. See <see cref="Capacity"/> for the total size.
        /// </summary>
        public int Length
        {
            get
            {
                CheckRead();
                SyncBitData();
                return m_Data.length + ((m_Data.bitIndex + 7) >> 3);
            }
        }
        /// <summary>
        /// The size of the buffer used in bits. See <see cref="Length"/> for the length in bytes.
        /// </summary>
        public int LengthInBits
        {
            get
            {
                CheckRead();
                SyncBitData();
                return m_Data.length*8 + m_Data.bitIndex;
            }
        }

        private void SyncBitData()
        {
            var bitIndex = m_Data.bitIndex;
            if (bitIndex <= 0)
                return;
            CheckWrite();

            var bitBuffer = m_Data.bitBuffer;
            int offset = 0;
            while (bitIndex > 0)
            {
                m_Data.buffer[m_Data.length + offset] = (byte)bitBuffer;
                bitIndex -= 8;
                bitBuffer >>= 8;
                ++offset;
            }
        }
        public void Flush()
        {
            while (m_Data.bitIndex > 0)
            {
                m_Data.buffer[m_Data.length++] = (byte)m_Data.bitBuffer;
                m_Data.bitIndex -= 8;
                m_Data.bitBuffer >>= 8;
            }

            m_Data.bitIndex = 0;
        }

        public bool WriteBytes(byte* data, int bytes)
        {
            CheckWrite();

            if (m_Data.length + ((m_Data.bitIndex + 7) >> 3) + bytes > m_Data.capacity)
            {
                ++m_Data.failedWrites;
                return false;
            }
            Flush();
            UnsafeUtility.MemCpy(m_Data.buffer + m_Data.length, data, bytes);
            m_Data.length += bytes;
            return true;
        }

        public bool WriteByte(byte value)
        {
            return WriteBytes((byte*) &value, sizeof(byte));
        }

        /// <summary>
        /// Copy NativeArray of bytes into the writers data buffer.
        /// </summary>
        /// <param name="value">Source byte array</param>
        public bool WriteBytes(NativeArray<byte> value)
        {
            return WriteBytes((byte*)value.GetUnsafeReadOnlyPtr(), value.Length);
        }

        public bool WriteShort(short value)
        {
            return WriteBytes((byte*) &value, sizeof(short));
        }

        public bool WriteUShort(ushort value)
        {
            return WriteBytes((byte*) &value, sizeof(ushort));
        }

        public bool WriteInt(int value)
        {
            return WriteBytes((byte*) &value, sizeof(int));
        }

        public bool WriteUInt(uint value)
        {
            return WriteBytes((byte*) &value, sizeof(uint));
        }

        public bool WriteLong(long value)
        {
            return WriteBytes((byte*) &value, sizeof(long));
        }

        public bool WriteULong(ulong value)
        {
            return WriteBytes((byte*) &value, sizeof(ulong));
        }

        public bool WriteShortNetworkByteOrder(short value)
        {
            short netValue = IsLittleEndian ? ByteSwap(value) : value;
            return WriteBytes((byte*) &netValue, sizeof(short));
        }

        public bool WriteUShortNetworkByteOrder(ushort value)
        {
            return WriteShortNetworkByteOrder((short) value);
        }

        public bool WriteIntNetworkByteOrder(int value)
        {
            int netValue = IsLittleEndian ? ByteSwap(value) : value;
            return WriteBytes((byte*) &netValue, sizeof(int));
        }

        public bool WriteUIntNetworkByteOrder(uint value)
        {
            return WriteIntNetworkByteOrder((int)value);
        }

        public bool WriteFloat(float value)
        {
            UIntFloat uf = new UIntFloat();
            uf.floatValue = value;
            return WriteInt((int) uf.intValue);
        }

        private void FlushBits()
        {
            while (m_Data.bitIndex >= 8)
            {
                m_Data.buffer[m_Data.length++] = (byte)m_Data.bitBuffer;
                m_Data.bitIndex -= 8;
                m_Data.bitBuffer >>= 8;
            }
        }
        void WriteRawBitsInternal(uint value, int numbits)
        {
            CheckBits(value, numbits);

            m_Data.bitBuffer |= ((ulong)value << m_Data.bitIndex);
            m_Data.bitIndex += numbits;
        }
        public bool WriteRawBits(uint value, int numbits)
        {
            CheckWrite();

            if (m_Data.length + ((m_Data.bitIndex + numbits + 7) >> 3) > m_Data.capacity)
            {
                ++m_Data.failedWrites;
                return false;
            }
            WriteRawBitsInternal(value, numbits);
            FlushBits();
            return true;
        }

        public bool WritePackedUInt(uint value, NetworkCompressionModel model)
        {
            CheckWrite();
            int bucket = model.CalculateBucket(value);
            uint offset = model.bucketOffsets[bucket];
            int bits = model.bucketSizes[bucket];
            ushort encodeEntry = model.encodeTable[bucket];

            if (m_Data.length + ((m_Data.bitIndex + (encodeEntry&0xff) + bits + 7) >> 3) > m_Data.capacity)
            {
                ++m_Data.failedWrites;
                return false;
            }
            WriteRawBitsInternal((uint)(encodeEntry >> 8), encodeEntry & 0xFF);
            WriteRawBitsInternal(value - offset, bits);
            FlushBits();
            return true;
        }

        public bool WritePackedULong(ulong value, NetworkCompressionModel model)
        {
            return WritePackedUInt((uint) (value >> 32), model) &
                   WritePackedUInt((uint) (value & 0xFFFFFFFF), model);
        }

        public bool WritePackedInt(int value, NetworkCompressionModel model)
        {
            uint interleaved = (uint)((value >> 31) ^ (value << 1));      // interleave negative values between positive values: 0, -1, 1, -2, 2
            return WritePackedUInt(interleaved, model);
        }
        public bool WritePackedLong(long value, NetworkCompressionModel model)
        {
            ulong interleaved = (ulong)((value >> 63) ^ (value << 1));      // interleave negative values between positive values: 0, -1, 1, -2, 2
            return WritePackedULong(interleaved, model);
        }
        public bool WritePackedFloat(float value, NetworkCompressionModel model)
        {
            return WritePackedFloatDelta(value, 0, model);
        }
        public bool WritePackedUIntDelta(uint value, uint baseline, NetworkCompressionModel model)
        {
            int diff = (int)(baseline - value);
            return WritePackedInt(diff, model);
        }
        public bool WritePackedIntDelta(int value, int baseline, NetworkCompressionModel model)
        {
            int diff = (int)(baseline - value);
            return WritePackedInt(diff, model);
        }

        public bool WritePackedLongDelta(long value, long baseline, NetworkCompressionModel model)
        {
            long diff = (long)(baseline - value);
            return WritePackedLong(diff, model);
        }
        public bool WritePackedULongDelta(ulong value, ulong baseline, NetworkCompressionModel model)
        {
            long diff = (long)(baseline - value);
            return WritePackedLong(diff, model);
        }

        public bool WritePackedFloatDelta(float value, float baseline, NetworkCompressionModel model)
        {
            CheckWrite();
            var bits = 0;
            if (value != baseline)
                bits = 32;
            if (m_Data.length + ((m_Data.bitIndex + 1 + bits + 7) >> 3) > m_Data.capacity)
            {
                ++m_Data.failedWrites;
                return false;
            }
            if (bits == 0)
                WriteRawBitsInternal(0, 1);
            else
            {
                WriteRawBitsInternal(1, 1);
                UIntFloat uf = new UIntFloat();
                uf.floatValue = value;
                WriteRawBitsInternal(uf.intValue, bits);
            }
            FlushBits();
            return true;
        }

        public unsafe bool WriteFixedString32(FixedString32 str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytes(data, length);
        }
        public unsafe bool WriteFixedString64(FixedString64 str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytes(data, length);
        }
        public unsafe bool WriteFixedString128(FixedString128 str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytes(data, length);
        }
        public unsafe bool WriteFixedString512(FixedString512 str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytes(data, length);
        }
        public unsafe bool WriteFixedString4096(FixedString4096 str)
        {
            int length = (int)*((ushort*)&str) + 2;
            byte* data = ((byte*)&str);
            return WriteBytes(data, length);
        }

        public unsafe bool WritePackedFixedString32Delta(FixedString32 str, FixedString32 baseline, NetworkCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }
        public unsafe bool WritePackedFixedString64Delta(FixedString64 str, FixedString64 baseline, NetworkCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }
        public unsafe bool WritePackedFixedString128Delta(FixedString128 str, FixedString128 baseline, NetworkCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }
        public unsafe bool WritePackedFixedString512Delta(FixedString512 str, FixedString512 baseline, NetworkCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }
        public unsafe bool WritePackedFixedString4096Delta(FixedString4096 str, FixedString4096 baseline, NetworkCompressionModel model)
        {
            ushort length = *((ushort*)&str);
            byte* data = ((byte*)&str) + 2;
            return WritePackedFixedStringDelta(data, length, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
        }
        private unsafe bool WritePackedFixedStringDelta(byte* data, uint length, byte* baseData, uint baseLength, NetworkCompressionModel model)
        {
            var oldData = m_Data;
            if (!WritePackedUIntDelta(length, baseLength, model))
                return false;
            bool didFailWrite = false;
            if (length <= baseLength)
            {
                for (uint i = 0; i < length; ++i)
                    didFailWrite |= !WritePackedUIntDelta(data[i], baseData[i], model);
            }
            else
            {
                for (uint i = 0; i < baseLength; ++i)
                    didFailWrite |= !WritePackedUIntDelta(data[i], baseData[i], model);
                for (uint i = baseLength; i < length; ++i)
                    didFailWrite |= !WritePackedUInt(data[i], model);
            }
            // If anything was not written, rewind to the previous position
            if (didFailWrite)
            {
                m_Data = oldData;
                ++m_Data.failedWrites;
            }
            return !didFailWrite;
        }

        /// <summary>
        /// Moves the write position to the start of the data buffer used.
        /// </summary>
        public void Clear()
        {
            m_Data.length = 0;
            m_Data.bitIndex = 0;
            m_Data.bitBuffer = 0;
            m_Data.failedWrites = 0;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckAllocator(Allocator allocator)
        {
            if (allocator != Allocator.Temp)
                throw new InvalidOperationException("DataStreamWriters can only be created with temp memory");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckBits(uint value, int numbits)
        {
            if (numbits < 0 || numbits > 32)
                throw new ArgumentOutOfRangeException("Invalid number of bits");
            if (value >= (1UL << numbits))
                throw new ArgumentOutOfRangeException("Value does not fit in the specified number of bits");
        }
    }

    /// <summary>
    /// The <c>DataStreamReader</c> class is the counterpart of the
    /// <c>DataStreamWriter</c> class and can be be used to deserialize
    /// data which was prepared with it.
    /// </summary>
    /// <remarks>
    /// Simple usage example:
    /// <code>
    /// using (var dataWriter = new DataStreamWriter(16, Allocator.Persistent))
    /// {
    ///     dataWriter.Write(42);
    ///     dataWriter.Write(1234);
    ///     // Length is the actual amount of data inside the writer,
    ///     // Capacity is the total amount.
    ///     var dataReader = new DataStreamReader(dataWriter, 0, dataWriter.Length);
    ///     var context = default(DataStreamReader.Context);
    ///     var myFirstInt = dataReader.ReadInt(ref context);
    ///     var mySecondInt = dataReader.ReadInt(ref context);
    /// }
    /// </code>
    ///
    /// The <c>DataStreamReader</c> carries the position of the read pointer inside the struct,
    /// taking a copy of the reader will also copy the read position. This includes passing the
    /// reader to a method by value instead of by ref.
    ///
    /// See the <see cref="DataStreamWriter"/> class for more information
    /// and examples.
    /// </remarks>
    public unsafe struct DataStreamReader
    {
        struct Context
        {
            public int m_ReadByteIndex;
            public int m_BitIndex;
            public ulong m_BitBuffer;
            public int m_FailedReads;
        }

        byte* m_bufferPtr;
        Context m_Context;
        int m_Length;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
#endif

        public DataStreamReader(NativeArray<byte> array)
        {
            Initialize(out this, array);
        }

        public DataStreamReader(byte* data, int length)
        {
            var na = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(data, length, Allocator.Invalid);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref na, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            Initialize(out this, na);
        }
        
        private static void Initialize(out DataStreamReader self, NativeArray<byte> array)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            self.m_Safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array);
#endif
            self.m_bufferPtr = (byte*)array.GetUnsafeReadOnlyPtr();
            self.m_Length = array.Length;
            self.m_Context = default;
        }

        public bool IsLittleEndian => DataStreamWriter.IsLittleEndian;

        private static short ByteSwap(short val)
        {
            return (short)(((val & 0xff) << 8) | ((val >> 8)&0xff));
        }
        private static int ByteSwap(int val)
        {
            return (int)(((val & 0xff) << 24) |((val&0xff00)<<8) | ((val>>8)&0xff00) | ((val >> 24)&0xff));
        }

        public bool HasFailedReads => m_Context.m_FailedReads > 0;
        /// <summary>
        /// The total size of the buffer space this reader is working with.
        /// </summary>
        public int Length
        {
            get
            {
                CheckRead();
                return m_Length;
            }
        }

        /// <summary>
        /// True if the reader has been pointed to a valid buffer space. This
        /// would be false if the reader was created with no arguments.
        /// </summary>
        public bool IsCreated
        {
            get { return m_bufferPtr != null; }
        }

        /// <summary>
        /// Read and copy data to the memory location pointed to, an exception will
        /// be thrown if it does not fit.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the length
        /// will put the reader out of bounds based on the current read pointer
        /// position.</exception>
        public void ReadBytes(byte* data, int length)
        {
            CheckRead();
            if (GetBytesRead() + length > m_Length)
            {
                ++m_Context.m_FailedReads;
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_DOTSRUNTIME
                UnityEngine.Debug.LogError($"Trying to read {length} bytes from a stream where only {m_Length - GetBytesRead()} are available");
#endif
                UnsafeUtility.MemClear(data, length);
                return;
            }
            // Restore the full bytes moved to the bit buffer but no consumed
            m_Context.m_ReadByteIndex -= (m_Context.m_BitIndex >> 3);
            m_Context.m_BitIndex = 0;
            m_Context.m_BitBuffer = 0;
            UnsafeUtility.MemCpy(data, m_bufferPtr + m_Context.m_ReadByteIndex, length);
            m_Context.m_ReadByteIndex += length;
        }

        /// <summary>
        /// Read and copy data into the given NativeArray of bytes, an exception will
        /// be thrown if not enough bytes are available.
        /// </summary>
        /// <param name="array"></param>
        public void ReadBytes(NativeArray<byte> array)
        {
            ReadBytes((byte*)array.GetUnsafePtr(), array.Length);
        }

        public int GetBytesRead()
        {
            return m_Context.m_ReadByteIndex - (m_Context.m_BitIndex >> 3);
        }
        public int GetBitsRead()
        {
            return (m_Context.m_ReadByteIndex<<3) - m_Context.m_BitIndex;
        }
        public void SeekSet(int pos)
        {
            if (pos > m_Length)
            {
                ++m_Context.m_FailedReads;
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_DOTSRUNTIME
                UnityEngine.Debug.LogError($"Trying to seek to {pos} in a stream of length {m_Length}");
#endif
                return;
            }
            m_Context.m_ReadByteIndex = pos;
            m_Context.m_BitIndex = 0;
            m_Context.m_BitBuffer = 0UL;
        }

        public byte ReadByte()
        {
            byte data;
            ReadBytes((byte*) &data, sizeof(byte));
            return data;
        }

        public short ReadShort()
        {
            short data;
            ReadBytes((byte*) &data, sizeof(short));
            return data;
        }

        public ushort ReadUShort()
        {
            ushort data;
            ReadBytes((byte*) &data, sizeof(ushort));
            return data;
        }

        public int ReadInt()
        {
            int data;
            ReadBytes((byte*) &data, sizeof(int));
            return data;
        }

        public uint ReadUInt()
        {
            uint data;
            ReadBytes((byte*) &data, sizeof(uint));
            return data;
        }
        public ulong ReadULong()
        {
            ulong data;
            ReadBytes((byte*) &data, sizeof(ulong));
            return data;
        }

        public short ReadShortNetworkByteOrder()
        {
            short data;
            ReadBytes((byte*) &data, sizeof(short));
            return IsLittleEndian ? ByteSwap(data) : data;
        }

        public ushort ReadUShortNetworkByteOrder()
        {
            return (ushort) ReadShortNetworkByteOrder();
        }

        public int ReadIntNetworkByteOrder()
        {
            int data;
            ReadBytes((byte*) &data, sizeof(int));
            return IsLittleEndian ? ByteSwap(data) : data;
        }

        public uint ReadUIntNetworkByteOrder()
        {
            return (uint) ReadIntNetworkByteOrder();
        }

        public float ReadFloat()
        {
            UIntFloat uf = new UIntFloat();
            uf.intValue = (uint) ReadInt();
            return uf.floatValue;
        }
        public uint ReadPackedUInt(NetworkCompressionModel model)
        {
            CheckRead();
            FillBitBuffer();
            uint peekMask = (1u << NetworkCompressionModel.k_MaxHuffmanSymbolLength) - 1u;
            uint peekBits = (uint)m_Context.m_BitBuffer & peekMask;
            ushort huffmanEntry = model.decodeTable[(int)peekBits];
            int symbol = huffmanEntry >> 8;
            int length = huffmanEntry & 0xFF;

            if (m_Context.m_BitIndex < length)
            {
                ++m_Context.m_FailedReads;
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_DOTSRUNTIME
                UnityEngine.Debug.LogError($"Trying to read {length} bits from a stream where only {m_Context.m_BitIndex} are available");
#endif
                return 0;
            }

            // Skip Huffman bits
            m_Context.m_BitBuffer >>= length;
            m_Context.m_BitIndex -= length;

            uint offset = model.bucketOffsets[symbol];
            int bits = model.bucketSizes[symbol];
            return ReadRawBitsInternal(bits) + offset;
        }
        void FillBitBuffer()
        {
            while (m_Context.m_BitIndex <= 56 && m_Context.m_ReadByteIndex < m_Length)
            {
                m_Context.m_BitBuffer |= (ulong)m_bufferPtr[m_Context.m_ReadByteIndex++] << m_Context.m_BitIndex;
                m_Context.m_BitIndex += 8;
            }
        }
        uint ReadRawBitsInternal(int numbits)
        {
            CheckBits(numbits);
            if (m_Context.m_BitIndex < numbits)
            {
                ++m_Context.m_FailedReads;
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_DOTSRUNTIME
                UnityEngine.Debug.LogError($"Trying to read {numbits} bits from a stream where only {m_Context.m_BitIndex} are available");
#endif
                return 0;
            }
            uint res = (uint)(m_Context.m_BitBuffer & ((1UL << numbits) - 1UL));
            m_Context.m_BitBuffer >>= numbits;
            m_Context.m_BitIndex -= numbits;
            return res;
        }
        public uint ReadRawBits(int numbits)
        {
            CheckRead();
            FillBitBuffer();
            return ReadRawBitsInternal(numbits);
        }

        public ulong ReadPackedULong(NetworkCompressionModel model)
        {
            //hi
            ulong hi = ReadPackedUInt(model);
            hi <<= 32;
            hi |= ReadPackedUInt(model);
            return hi;
        }

        public int ReadPackedInt(NetworkCompressionModel model)
        {
            uint folded = ReadPackedUInt(model);
            return (int)(folded >> 1) ^ -(int)(folded & 1);    // Deinterleave values from [0, -1, 1, -2, 2...] to [..., -2, -1, -0, 1, 2, ...]
        }
        public long ReadPackedLong(NetworkCompressionModel model)
        {
            ulong folded = ReadPackedULong(model);
            return (long)(folded >> 1) ^ -(long)(folded & 1);    // Deinterleave values from [0, -1, 1, -2, 2...] to [..., -2, -1, -0, 1, 2, ...]
        }
        public float ReadPackedFloat(NetworkCompressionModel model)
        {
            return ReadPackedFloatDelta(0, model);
        }
        public int ReadPackedIntDelta(int baseline, NetworkCompressionModel model)
        {
            int delta = ReadPackedInt(model);
            return baseline - delta;
        }

        public uint ReadPackedUIntDelta(uint baseline, NetworkCompressionModel model)
        {
            uint delta = (uint)ReadPackedInt(model);
            return baseline - delta;
        }

        public long ReadPackedLongDelta(long baseline, NetworkCompressionModel model)
        {
            long delta = ReadPackedLong(model);
            return baseline - delta;
        }

        public ulong ReadPackedULongDelta(ulong baseline, NetworkCompressionModel model)
        {
            ulong delta = (ulong)ReadPackedLong(model);
            return baseline - delta;
        }


        public float ReadPackedFloatDelta(float baseline, NetworkCompressionModel model)
        {
            CheckRead();
            FillBitBuffer();
            if (ReadRawBitsInternal(1) == 0)
                return baseline;

            var bits = 32;
            UIntFloat uf = new UIntFloat();
            uf.intValue = ReadRawBitsInternal(bits);
            return uf.floatValue;
        }

        public unsafe FixedString32 ReadFixedString32()
        {
            FixedString32 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedString(data, str.Capacity);
            return str;
        }
        public unsafe FixedString64 ReadFixedString64()
        {
            FixedString64 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedString(data, str.Capacity);
            return str;
        }
        public unsafe FixedString128 ReadFixedString128()
        {
            FixedString128 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedString(data, str.Capacity);
            return str;
        }
        public unsafe FixedString512 ReadFixedString512()
        {
            FixedString512 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedString(data, str.Capacity);
            return str;
        }
        public unsafe FixedString4096 ReadFixedString4096()
        {
            FixedString4096 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadFixedString(data, str.Capacity);
            return str;
        }
        public unsafe ushort ReadFixedString(byte* data, int maxLength)
        {
            ushort length = ReadUShort();
            if (length > maxLength)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_DOTSRUNTIME
                UnityEngine.Debug.LogError($"Trying to read a string of length {length} but max length is {maxLength}");
#endif
                return 0;
            }
            ReadBytes(data, length);
            return length;
        }

        public unsafe FixedString32 ReadPackedFixedString32Delta(FixedString32 baseline, NetworkCompressionModel model)
        {
            FixedString32 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDelta(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }
        public unsafe FixedString64 ReadPackedFixedString64Delta(FixedString64 baseline, NetworkCompressionModel model)
        {
            FixedString64 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDelta(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }
        public unsafe FixedString128 ReadPackedFixedString128Delta(FixedString128 baseline, NetworkCompressionModel model)
        {
            FixedString128 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDelta(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }
        public unsafe FixedString512 ReadPackedFixedString512Delta(FixedString512 baseline, NetworkCompressionModel model)
        {
            FixedString512 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDelta(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }
        public unsafe FixedString4096 ReadPackedFixedString4096Delta(FixedString4096 baseline, NetworkCompressionModel model)
        {
            FixedString4096 str;
            byte* data = ((byte*)&str) + 2;
            *(ushort*)&str = ReadPackedFixedStringDelta(data, str.Capacity, ((byte*)&baseline) + 2, *((ushort*)&baseline), model);
            return str;
        }
        public unsafe ushort ReadPackedFixedStringDelta(byte* data, int maxLength, byte* baseData, ushort baseLength, NetworkCompressionModel model)
        {
            uint length = ReadPackedUIntDelta(baseLength, model);
            if (length > (uint)maxLength)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS && !UNITY_DOTSRUNTIME
                UnityEngine.Debug.LogError($"Trying to read a string of length {length} but max length is {maxLength}");
#endif
                return 0;
            }
            if (length <= baseLength)
            {
                for (int i = 0; i < length; ++i)
                    data[i] = (byte)ReadPackedUIntDelta(baseData[i], model);
            }
            else
            {
                for (int i = 0; i < baseLength; ++i)
                    data[i] = (byte)ReadPackedUIntDelta(baseData[i], model);
                for (int i = baseLength; i < length; ++i)
                    data[i] = (byte)ReadPackedUInt(model);
            }
            return (ushort)length;
        }
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckRead()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void CheckBits(int numbits)
        {
            if (numbits < 0 || numbits > 32)
                throw new ArgumentOutOfRangeException("Invalid number of bits");
        }
    }
}
