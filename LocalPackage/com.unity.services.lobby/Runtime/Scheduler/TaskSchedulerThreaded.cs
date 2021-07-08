using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Diagnostics;

namespace Unity.Services.Lobbies.Scheduler
{

    /// <summary>
    /// <para>Provides the means to schedule tasks on a background thread or the 
    /// main thread. Also allows for creation of co-routines from classes that do
    /// not inherit from MonoBehaviour.</para>
    /// 
    /// <para>This is thread safe, through it must be constructed on the main thread.
    /// </para>
    /// </summary>
	public sealed class TaskSchedulerThreaded : TaskScheduler
    {
        private Queue<Action> m_mainThreadTaskQueue = new Queue<Action>();
        private object m_lock = new object();
        private Thread m_mainThread = null;

        /// <summary>
        /// Constructs a new instances of the Task Scheduler. This must be on the main
        /// thread as a reference to the thread is captured.
        /// </summary>
        void Start()
        {
            m_mainThread = System.Threading.Thread.CurrentThread;
        }

        /// <summary>
        /// Determines whether the current thread is the main thread.
        /// </summary>
        /// 
        /// <returns>Whether or not this thread is the main thread.</returns>
        public override bool IsMainThread()
        {
			return (m_mainThread == System.Threading.Thread.CurrentThread);
        }

        /// <summary>
        /// Schedules a new task on a background thread.
        /// </summary>
        /// 
        /// <param name="task">The task that should be executed on a background thread.</param>
		public override void ScheduleBackgroundTask(Action task)
        {
            ThreadPool.QueueUserWorkItem((object state) =>
			{
                task();
            });
        }

        /// <summary>
        /// Schedules a new task on the main thread. The task will be executed during the
        /// next update.
        /// </summary>
        /// 
        /// <param name="task">The task that should be executed on the main thread.</param>
		public override void ScheduleMainThreadTask(Action task)
        {
            lock (m_lock)
            {
                m_mainThreadTaskQueue.Enqueue(task);
            }
        }

        /// <summary>
        /// The update method which is called every frame. This executes any queued main
        /// thread tasks.
        /// </summary>
        void Update()
        {
			Queue<Action> taskQueue = null;
            lock (m_lock)
			{
				taskQueue = new Queue<Action>(m_mainThreadTaskQueue);
				m_mainThreadTaskQueue.Clear();
			}

            foreach (Action action in taskQueue)
            {
                action();
            }
        }
    }
}