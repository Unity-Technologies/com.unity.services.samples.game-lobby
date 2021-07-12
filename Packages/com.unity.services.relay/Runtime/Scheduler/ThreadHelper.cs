using System.Threading;
using UnityEngine;

namespace Unity.Services.Relay.Scheduler
{
    // "inspired" by UniTask
    public static class ThreadHelper
    {
        public static SynchronizationContext SynchronizationContext => _unitySynchronizationContext;
        public static System.Threading.Tasks.TaskScheduler TaskScheduler => _taskScheduler;
        public static int MainThreadId => _mainThreadId;

        private static SynchronizationContext _unitySynchronizationContext;
        private static System.Threading.Tasks.TaskScheduler _taskScheduler;
        private static int _mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            _unitySynchronizationContext = SynchronizationContext.Current;
            _taskScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }
}
