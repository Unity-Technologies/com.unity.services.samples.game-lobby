using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Services.Authentication.Utilities
{
    interface IScheduler
    {
        /// <summary>
        /// Schedules the action to be invoked on the main thead, on the next frame
        /// </summary>
        void ScheduleAction(Action action);

        /// <summary>
        /// Schedules the action to be invoked on the main thead, on the first frame that occurs after the given delay in seconds
        /// </summary>
        void ScheduleAction(Action action, int delaySeconds);

        /// <summary>
        /// Removes all instances of the given action from the queue
        /// </summary>
        void CancelAction(Action action);
    }

    class Scheduler : MonoBehaviour, IScheduler
    {
        struct ScheduledInvocation
        {
            public Action Action;
            public DateTime InvocationTime;
        }

        List<ScheduledInvocation> m_Queue = new List<ScheduledInvocation>();

        static Scheduler s_Instance;
        static bool s_Created;

        public static IScheduler Instance
        {
            get
            {
                if (!s_Created)
                {
                    s_Instance = ContainerObject.Container.AddComponent<Scheduler>();
                    s_Created = true;
                }

                return s_Instance;
            }
        }

        public int QueuedActions => m_Queue.Count;

        public void ScheduleAction(Action action)
        {
            m_Queue.Add(new ScheduledInvocation { Action = action, InvocationTime = DateTime.UtcNow });
        }

        public void ScheduleAction(Action action, int delaySeconds)
        {
            m_Queue.Add(new ScheduledInvocation { Action = action, InvocationTime = DateTime.UtcNow.AddSeconds(delaySeconds) });
        }

        public void CancelAction(Action action)
        {
            for (var i = m_Queue.Count - 1; i >= 0; i--)
            {
                if (m_Queue[i].Action == action)
                {
                    m_Queue.RemoveAt(i);
                }
            }
        }

        void Update()
        {
            for (var i = m_Queue.Count - 1; i >= 0; i--)
            {
                var action = m_Queue[i];
                if (action.InvocationTime < DateTime.UtcNow)
                {
                    m_Queue.RemoveAt(i);
                    action.Action();
                }
            }
        }
    }
}
