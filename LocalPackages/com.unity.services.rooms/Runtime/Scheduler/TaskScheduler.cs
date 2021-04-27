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
	public abstract class TaskScheduler : MonoBehaviour
	{
		/// <summary>
		/// Schedules a new task on a background thread.
		/// </summary>
		/// 
		/// <param name="task">The task that should be executed on a background thread.</param>
		public abstract void ScheduleBackgroundTask(Action task);

		/// <summary>
		/// Determines whether the current thread is the main thread.
		/// </summary>
		/// 
		/// <returns>Whether or not this thread is the main thread.</returns>
		public abstract bool IsMainThread();

		/// <summary>
		/// Schedules a new task on the main thread. The task will be executed during the
		/// next update.
		/// </summary>
		/// 
		/// <param name="task">The task that should be executed on the main thread.</param>
		public abstract void ScheduleMainThreadTask(Action task);
		
		/// <summary>
		/// Executes immediately if on main thread else queue on main thread for next update.
		/// </summary>
		/// 
		/// <param name="action">The task that should be executed on the main thread.</param>
		public void ScheduleOrExecuteOnMain(Action action)
		{
			if (IsMainThread())
			{
				action?.Invoke();
			}
			else
			{
				ScheduleMainThreadTask(action);
			}
		}
	}
}