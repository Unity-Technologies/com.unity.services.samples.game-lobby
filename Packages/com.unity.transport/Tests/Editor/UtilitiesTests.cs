using NUnit.Framework;
using Unity.Networking.Transport.Utilities;
using Unity.Networking.Transport.Utilities.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;

namespace Unity.Networking.Transport.Tests
{
    public class NetworkUtilities_Tests
    {
        [Test]
        public void NativeMultiQueue_SimpleScenarios()
        {
            using (NativeMultiQueue<int> eventQ = new NativeMultiQueue<int>(5))
            {
                for (int connection = 0; connection < 5; connection++)
                {
                    // Test Add
                    int item = 0;

                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);
                    eventQ.Enqueue(connection, 1);

                    // Add grows capacity
                    eventQ.Enqueue(connection, 1);

                    // Test Rem
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));
                    Assert.True(eventQ.Dequeue(connection, out item));

                    // Remove with grown capacity
                    Assert.True(eventQ.Dequeue(connection, out item));
                }
            }
        }

        struct FreeJob : IJobParallelFor
        {
            public UnsafeAtomicFreeList freeList;
            public void Execute(int i)
            {
                var indices = new NativeArray<int>(100, Allocator.Temp);
                for (int asd = 0; asd < indices.Length; ++asd)
                    indices[asd] = freeList.Pop();
                for (int asd = 0; asd < indices.Length; ++asd)
                {
                    if (indices[asd] >= 0)
                        freeList.Push(indices[asd]);
                }
            }
        }

        [Test]
        //[Repeat( 25 )]
        public void AtomicFreeList()
        {
            using (var freeList = new UnsafeAtomicFreeList(1024, Allocator.Persistent))
            {
                var job = new FreeJob {freeList = freeList};
                job.Schedule(1024*100, 1).Complete();
                var foo = new HashSet<int>();
                for (int i = 0; i < freeList.Capacity; ++i)
                {
                    var idx = freeList.Pop();
                    Assert.IsTrue(idx < 1024 && idx >= 0);
                    Assert.IsFalse(foo.Contains(idx));
                }
                Assert.AreEqual(-1, freeList.Pop());
            }
        }
    }
}