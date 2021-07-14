using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Jobs.Tests.ManagedJobs
{
#if UNITY_DOTSRUNTIME
    public class DotsRuntimeFixmeAttribute : IgnoreAttribute
    {
        public DotsRuntimeFixmeAttribute(string msg = null) : base(msg == null ? "Test should work in DOTS Runtime but currently doesn't. Ignoring until fixed..." : msg)
        {
        }
    }
#else
	public class DotsRuntimeFixmeAttribute : Attribute
	{
        public DotsRuntimeFixmeAttribute(string msg = null)
        {
        }
	}
#endif

	[JobProducerType(typeof(IJobTestExtensions.JobTestProducer<>))]
	public interface IJobTest
	{
		void Execute();
	}

	public static class IJobTestExtensions
	{
        internal struct JobTestWrapper<T> where T : struct
        {
            internal T JobData;

            [NativeDisableContainerSafetyRestriction]
            [DeallocateOnJobCompletion]
            internal NativeArray<byte> ProducerResourceToClean;
        }

		internal struct JobTestProducer<T> where T : struct, IJobTest
		{
			static IntPtr s_JobReflectionData;

			public static IntPtr Initialize()
			{
				if (s_JobReflectionData == IntPtr.Zero)
				{
#if UNITY_2020_2_OR_NEWER
					s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobTestWrapper<T>), typeof(T), (ExecuteJobFunction)Execute);
#else
					s_JobReflectionData = JobsUtility.CreateJobReflectionData(typeof(JobTestWrapper<T>), typeof(T),
						JobType.Single, (ExecuteJobFunction)Execute);
#endif
				}

				return s_JobReflectionData;
			}

			public delegate void ExecuteJobFunction(ref JobTestWrapper<T> jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);
			public unsafe static void Execute(ref JobTestWrapper<T> jobWrapper, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
			{
				jobWrapper.JobData.Execute();
			}
		}

		public static unsafe JobHandle ScheduleTest<T>(this T jobData, NativeArray<byte> dataForProducer, JobHandle dependsOn = new JobHandle()) where T : struct, IJobTest
		{
			JobTestWrapper<T> jobTestWrapper = new JobTestWrapper<T>
			{
				JobData = jobData,
				ProducerResourceToClean = dataForProducer
			};

			var scheduleParams = new JobsUtility.JobScheduleParameters(
				UnsafeUtility.AddressOf(ref jobTestWrapper),
				JobTestProducer<T>.Initialize(),
				dependsOn,
#if UNITY_2020_2_OR_NEWER
				ScheduleMode.Parallel
#else
				ScheduleMode.Batched
#endif
			);

			return JobsUtility.Schedule(ref scheduleParams);
		}
	}

	public struct MyGenericResizeJob<T> : IJob where T : unmanaged
    {
		public int m_ListLength;
		public NativeList<T> m_GenericList;
		public void Execute()
		{
			m_GenericList.Resize(m_ListLength, NativeArrayOptions.UninitializedMemory);
		}
	}

	public struct MyGenericJobDefer<T> : IJobParallelForDefer where T: unmanaged
	{
		public T m_Value;
		[NativeDisableParallelForRestriction]
		public NativeList<T> m_GenericList;
		public void Execute(int index)
		{
			m_GenericList[index] = m_Value;
		}
	}

    public struct GenericContainerResizeJob<T, U> : IJob
        where T : struct, INativeList<U>
        where U : struct
    {
        public int m_ListLength;
        public T m_GenericList;
        public void Execute()
        {
            m_GenericList.Length = m_ListLength;
        }
    }

    public struct GenericContainerJobDefer<T, U> : IJobParallelForDefer
        where T : struct, INativeList<U>
        where U : struct
    {
        public U m_Value;
        [NativeDisableParallelForRestriction]
        public T m_GenericList;

        public void Execute(int index)
        {
            m_GenericList[index] = m_Value;
        }
    }

    public class JobTests : JobTestsFixture
    {
        public void ScheduleGenericContainerJob<T, U>(T container, U value)
            where T : struct, INativeList<U>
            where U : unmanaged
        {
            var j0 = new GenericContainerResizeJob<T, U>();
            var length = 5;
            j0.m_ListLength = length;
            j0.m_GenericList = container;
            var handle0 = j0.Schedule();

            var j1 = new GenericContainerJobDefer<T, U>();
            j1.m_Value = value;
            j1.m_GenericList = j0.m_GenericList;
            INativeList<U> iList = j0.m_GenericList;
            j1.Schedule((NativeList<U>)iList, 1, handle0).Complete();

            Assert.AreEqual(length, j1.m_GenericList.Length);
            for (int i = 0; i != j1.m_GenericList.Length; i++)
                Assert.AreEqual(value, j1.m_GenericList[i]);
        }

        [Test]
        public void ValidateContainerSafetyInGenericJob_ContainerIsGenericParameter()
        {
            var list = new NativeList<int>(1, Allocator.TempJob);
            ScheduleGenericContainerJob(list, 5);
            list.Dispose();
        }

        public void GenericScheduleJobPair<T>(T value) where T : unmanaged
        {
            var j0 = new MyGenericResizeJob<T>();
            var length = 5;
            j0.m_ListLength = length;
            j0.m_GenericList = new NativeList<T>(1, Allocator.TempJob);
            var handle0 = j0.Schedule();

            var j1 = new MyGenericJobDefer<T>();
            j1.m_Value = value;
            j1.m_GenericList = j0.m_GenericList;
            j1.Schedule(j0.m_GenericList, 1, handle0).Complete();

            Assert.AreEqual(length, j1.m_GenericList.Length);
            for (int i = 0; i != j1.m_GenericList.Length; i++)
                Assert.AreEqual(value, j1.m_GenericList[i]);
            j0.m_GenericList.Dispose();
        }

        [Test]
        public void ScheduleGenericJobPairFloat()
        {
            GenericScheduleJobPair(10f);
        }

        [Test]
        public void ScheduleGenericJobPairDouble()
        {
            GenericScheduleJobPair<double>(10.0);
        }

        [Test]
        public void ScheduleGenericJobPairInt()
        {
            GenericScheduleJobPair(20);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
	    public void SchedulingGenericJobUnsafelyThrows()
	    {
		    var j0 = new MyGenericResizeJob<int>();
		    var length = 5;
		    j0.m_ListLength = length;
		    j0.m_GenericList = new NativeList<int>(1, Allocator.TempJob);
		    var handle0 = j0.Schedule();
		    var j1 = new MyGenericJobDefer<int>();
		    j1.m_Value = 6;
		    j1.m_GenericList = j0.m_GenericList;
		    Assert.Throws<InvalidOperationException>(()=>j1.Schedule(j0.m_GenericList, 1).Complete());
		    handle0.Complete();
		    j0.m_GenericList.Dispose();
	    }
#endif

        [Test, DotsRuntimeFixme("From a pure generic context, DOTS Runtime cannot determine what closed generic jobs are scheduled. See DOTSR-2347")]
        public void SchedulingGenericJobFromGenericContextUnsafelyThrows()
        {
            var list = new NativeList<int>(1, Allocator.TempJob);
            ScheduleGenericJobUnsafely(list, 5);
            list.Dispose();
        }

        void ScheduleGenericJobUnsafely<T, U>(T container, U value)
            where T : struct, INativeList<U>
            where U : unmanaged
        {
            var j0 = new GenericContainerResizeJob<T, U>();
            var length = 5;
            j0.m_ListLength = length;
            j0.m_GenericList = container;
            var handle0 = j0.Schedule();

            var j1 = new GenericContainerJobDefer<T, U>();
            j1.m_Value = value;
            j1.m_GenericList = j0.m_GenericList;
            INativeList<U> iList = j0.m_GenericList;
            Assert.Throws<InvalidOperationException>(()=>j1.Schedule((NativeList<U>)iList, 1).Complete());
            // Note we now pass the correct dependency to complete the job otherwise we won't be able to dispose the list
            // which will cause other tests to fail when they detect leaks. We can't just throw and then dispose since the
            // safety system will see that the list was scheduled and should first have the job completed (however we
            // are intentionally setting up a job that cannot complete)
            j1.Schedule((NativeList<U>)iList, 1, handle0).Complete();
        }

        /*
	     * these two tests used to test that a job that inherited from both IJob and IJobParallelFor would work as expected
	     * but that's probably crazy.
	     */
        /*[Test]
        public void Scheduling()
        {
            var job = data.Schedule();
            job.Complete();
            ExpectOutputSumOfInput0And1();
        }*/


        /*[Test]

        public void Scheduling_With_Dependencies()
        {
            data.input0 = input0;
            data.input1 = input1;
            data.output = output2;
            var job1 = data.Schedule();

            // Schedule job2 with dependency against the first job
            data.input0 = output2;
            data.input1 = input2;
            data.output = output;
            var job2 = data.Schedule(job1);

            // Wait for completion
            job2.Complete();
            ExpectOutputSumOfInput0And1And2();
        }*/

        [Test]
        public void ForEach_Scheduling_With_Dependencies()
        {
            data.input0 = input0;
            data.input1 = input1;
            data.output = output2;
            var job1 = data.Schedule(output.Length, 1);

            // Schedule job2 with dependency against the first job
            data.input0 = output2;
            data.input1 = input2;
            data.output = output;
            var job2 = data.Schedule(output.Length, 1, job1);

            // Wait for completion
            job2.Complete();
            ExpectOutputSumOfInput0And1And2();
        }

        struct EmptyComputeParallelForJob : IJobParallelFor
        {
            public void Execute(int i)
            {
            }
        }

        [Test]
        public void ForEach_Scheduling_With_Zero_Size()
        {
            var test = new EmptyComputeParallelForJob();
            var job = test.Schedule(0, 1);
            job.Complete();
        }

        [Test]
        public void Deallocate_Temp_NativeArray_From_Job()
        {
            TestDeallocateNativeArrayFromJob(Allocator.TempJob);
        }

        [Test]
        public void Deallocate_Persistent_NativeArray_From_Job()
        {
            TestDeallocateNativeArrayFromJob(Allocator.Persistent);
        }

        private void TestDeallocateNativeArrayFromJob(Allocator label)
        {
            var tempNativeArray = new NativeArray<int>(expectedInput0, label);

            var copyAndDestroyJob = new CopyAndDestroyNativeArrayParallelForJob
            {
                input = tempNativeArray,
                output = output
            };

            // NativeArray can safely be accessed before scheduling
            Assert.AreEqual(10, tempNativeArray.Length);

            tempNativeArray[0] = tempNativeArray[0];

            var job = copyAndDestroyJob.Schedule(copyAndDestroyJob.input.Length, 1);

            job.Complete();

            Assert.AreEqual(expectedInput0, copyAndDestroyJob.output.ToArray());
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public struct NestedDeallocateStruct
        {
			// This should deallocate even though it's a nested field
            [DeallocateOnJobCompletion]
            public NativeArray<int> input;
        }

        public struct TestNestedDeallocate : IJob
        {
            public NestedDeallocateStruct nested;

            public NativeArray<int> output;

            public void Execute()
            {
                for (int i = 0; i < nested.input.Length; ++i)
                    output[i] = nested.input[i];
            }
        }

        [Test]
        public void TestNestedDeallocateOnJobCompletion()
        {
            var tempNativeArray = new NativeArray<int>(10, Allocator.TempJob);
            var outNativeArray = new NativeArray<int>(10, Allocator.TempJob);
            for (int i = 0; i < 10; i++)
                tempNativeArray[i] = i;

            var job = new TestNestedDeallocate
            {
                nested = new NestedDeallocateStruct() { input = tempNativeArray },
                output = outNativeArray
            };

            var handle = job.Schedule();
            handle.Complete();

            outNativeArray.Dispose();

			// Ensure released safety handle indicating invalid buffer
			Assert.Throws<InvalidOperationException>(() => { AtomicSafetyHandle.CheckExistsAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(tempNativeArray)); });
			Assert.Throws<InvalidOperationException>(() => { AtomicSafetyHandle.CheckExistsAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(job.nested.input)); });
        }

        public struct TestJobProducerJob : IJobTest
        {
			[DeallocateOnJobCompletion]
            public NativeArray<int> jobStructData;

            public void Execute()
            {
            }
        }

        [Test]
        public void TestJobProducerCleansUp()
        {
            var tempNativeArray = new NativeArray<int>(10, Allocator.TempJob);
            var tempNativeArray2 = new NativeArray<byte>(16, Allocator.TempJob);

            var job = new TestJobProducerJob
            {
                jobStructData = tempNativeArray,
            };

            var handle = job.ScheduleTest(tempNativeArray2);
            handle.Complete();

			// Check job data
			Assert.Throws<InvalidOperationException>(() => { AtomicSafetyHandle.CheckExistsAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(tempNativeArray)); });
			Assert.Throws<InvalidOperationException>(() => { AtomicSafetyHandle.CheckExistsAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(job.jobStructData)); });
			// Check job producer
			Assert.Throws<InvalidOperationException>(() => { AtomicSafetyHandle.CheckExistsAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(tempNativeArray2)); });
        }

        public struct CopyJob : IJob
        {
            public NativeList<int> List1;
            public NativeList<int> List2;

            public void Execute()
            {
                List1 = List2;
            }
        }

        [Test]
        public unsafe void TestContainerCopy_EnsureSafetyHandlesCopyAndDisposeProperly()
        {
            var list1 = new NativeList<int>(10, Allocator.TempJob);
            var list2 = new NativeList<int>(10, Allocator.TempJob); 
            list1.Add(1);
            list2.Add(2);

            var job = new CopyJob
            {
                List1 = list1,
                List2 = list2
            };

            job.Schedule().Complete();

            list1.Dispose();
            list2.Dispose();
        }
#endif

        struct LargeJobParallelForDefer : IJobParallelForDefer
        {
            public FixedString4096 StrA;
            public FixedString4096 StrB;
            public FixedString4096 StrC;
            public FixedString4096 StrD;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> TotalLengths;
            [ReadOnly]
            public NativeList<float> Unused; // Schedule() from NativeList.Length requires that the list be passed into the job

            public void Execute(int index)
            {
                TotalLengths[0] = StrA.Length + StrB.Length + StrC.Length + StrD.Length;
            }
        }

        public enum IterationCountMode
        {
            List, Pointer
        }

        [Test]
        public unsafe void IJobParallelForDefer_LargeJobStruct_ScheduleRefWorks(
            [Values(IterationCountMode.List, IterationCountMode.Pointer)] IterationCountMode countMode)
        {
            using(var lengths = new NativeArray<int>(1, Allocator.TempJob))
            {
                var dummyList = new NativeList<float>(Allocator.TempJob);
                dummyList.Add(5.0f);
                var job = new LargeJobParallelForDefer
                {
                    StrA = "A",
                    StrB = "BB",
                    StrC = "CCC",
                    StrD = "DDDD",
                    TotalLengths = lengths,
                    Unused = dummyList,
                };

                if (countMode == IterationCountMode.List)
                {
                    Assert.DoesNotThrow(() => job.ScheduleByRef(dummyList, 1).Complete());
                }
                else if (countMode == IterationCountMode.Pointer)
                {
                    var lengthArray = new NativeArray<int>(1, Allocator.TempJob);
                    lengthArray[0] = 1;
                    Assert.DoesNotThrow(() => job.ScheduleByRef((int*)lengthArray.GetUnsafePtr(), 1).Complete());
                    lengthArray.Dispose();
                }
                dummyList.Dispose();
            }
        }
    }
}
