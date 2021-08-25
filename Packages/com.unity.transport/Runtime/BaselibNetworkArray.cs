using System;
using Unity.Baselib;
using Unity.Baselib.LowLevel;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using ErrorState = Unity.Baselib.LowLevel.Binding.Baselib_ErrorState;
using ErrorCode = Unity.Baselib.LowLevel.Binding.Baselib_ErrorCode;

namespace Unity.Networking.Transport
{
    using size_t = UIntPtr;

    internal unsafe struct UnsafeBaselibNetworkArray : IDisposable
    {
        [NativeDisableUnsafePtrRestriction] Binding.Baselib_RegisteredNetwork_Buffer* m_Buffer;

        /// <summary>
        /// Initializes a new instance of the UnsafeBaselibNetworkArray struct.
        /// </summary>
        /// <param name="capacity"></param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the capacity is less then 0 or if the value exceeds <see cref="int.MaxValue"/> </exception>
        public UnsafeBaselibNetworkArray(int capacity)
        {
            var totalSize = (long)capacity;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be >= 0");

            if (totalSize > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(capacity), $"Capacity * sizeof(T) cannot exceed {int.MaxValue} bytes");
#endif
            var pageInfo = stackalloc Binding.Baselib_Memory_PageSizeInfo[1];
            Binding.Baselib_Memory_GetPageSizeInfo(pageInfo);
            var defaultPageSize = (ulong)pageInfo->defaultPageSize;

            var pageCount = (ulong)1;
            if ((ulong) totalSize > defaultPageSize)
            {
                pageCount = (ulong)math.ceil(totalSize / (double) defaultPageSize);
            }

            var error = default(ErrorState);

            var pageAllocation = Binding.Baselib_Memory_AllocatePages(
                pageInfo->defaultPageSize,
                pageCount,
                1,
                Binding.Baselib_Memory_PageState.ReadWrite,
                &error);

            if (error.code != ErrorCode.Success)
                throw new Exception();

            UnsafeUtility.MemSet((void*)pageAllocation.ptr, 0, (long)(pageAllocation.pageCount * pageAllocation.pageSize));

            m_Buffer = (Binding.Baselib_RegisteredNetwork_Buffer*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<Binding.Baselib_RegisteredNetwork_Buffer>(), UnsafeUtility.AlignOf<Binding.Baselib_RegisteredNetwork_Buffer>(), Allocator.Persistent);
            *m_Buffer = Binding.Baselib_RegisteredNetwork_Buffer_Register(pageAllocation, &error);
            if (error.code != (int)ErrorCode.Success)
            {
                Binding.Baselib_Memory_ReleasePages(pageAllocation, &error);
                *m_Buffer = default;
                throw new Exception();
            }
        }

        public void Dispose()
        {
            var error = default(ErrorState);
            var pageAllocation = m_Buffer->allocation;
            Binding.Baselib_RegisteredNetwork_Buffer_Deregister(*m_Buffer);
            Binding.Baselib_Memory_ReleasePages(pageAllocation, &error);
            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
        }

        /// <summary>
        /// Gets a element at the specified index, with the size of <see cref="elementSize">.
        /// </summary>
        /// <value>A <see cref="Binding.Baselib_RegisteredNetwork_BufferSlice"> pointing to the index supplied.</value>
        public Binding.Baselib_RegisteredNetwork_BufferSlice AtIndexAsSlice(int index, uint elementSize)
        {
            var offset = elementSize * (uint)index;
            Binding.Baselib_RegisteredNetwork_BufferSlice slice;
            slice.id = m_Buffer->id;
            slice.data = (IntPtr)((byte*) m_Buffer->allocation.ptr + offset);
            slice.offset = offset;
            slice.size = elementSize;
            return slice;
        }
    }
}
