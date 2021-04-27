using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Diagnostics;

namespace Unity.Services.Rooms.Scheduler
{
	/// <summary>
	/// <para>Provides the means to schedule tasks on a background thread or the 
	/// main thread. Also allows for creation of co-routines from classes that do
	/// not inherit from MonoBehaviour.</para>
	/// 
	/// <para>This is thread safe, through it must be constructed on the main thread.
	/// </para>
	/// </summary>
	public sealed class TaskSchedulerWebGL : TaskScheduler
	{
		private Queue<Action> m_mainThreadTaskQueue = new Queue<Action>();

		/// <summary>
		/// Schedules a new task on a background thread.
		/// NOTE: In WebGL, multi-threading isn't supported, so this will be scheduled on main thread instead.
		/// </summary>
		/// 
		/// <param name="task">The task that should be executed on a background thread.</param>
		public override void ScheduleBackgroundTask(Action task)
		{
			ScheduleMainThreadTask(task);
		}
		
		/// <summary>
		/// Determines whether the current thread is the main thread.
		/// WebGL currently runs on a single thread, so this will always be false.
		/// </summary>
		/// 
		/// <returns>Whether or not this thread is the main thread.</returns>
		public override bool IsMainThread()
		{
			return false;
		}

		/// <summary>
		/// Schedules a new task on the main thread. The task will be executed during the
		/// next update.
		/// </summary>
		/// 
		/// <param name="task">The task that should be executed on the main thread.</param>
		public override void ScheduleMainThreadTask(Action task)
		{
			m_mainThreadTaskQueue.Enqueue(task);
		}

		/// <summary>
		/// The update method which is called every frame. This executes any queued main
		/// thread tasks.
		/// </summary>
		void Update()
        {
            var action = m_mainThreadTaskQueue.Count > 0 ? m_mainThreadTaskQueue.Dequeue() : null;
			action?.Invoke();
        }
	}
}