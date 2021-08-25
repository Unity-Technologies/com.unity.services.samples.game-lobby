using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobbyRelaySample
{
    /// <summary>
    /// Both Lobby and Relay have need for asynchronous requests with some basic safety wrappers. This is a shared place for that.
    /// </summary>
    public static class AsyncRequest
    {
        public static async void DoRequest(Task task, Action onComplete)
        {
            string currentTrace = System.Environment.StackTrace; // For debugging. If we don't get the calling context here, it's lost once the async operation begins.
            try
            {   await task;
            }
            catch (Exception e)
            {   Exception eFull = new Exception($"Call stack before async call:\n{currentTrace}\n", e);
                throw eFull;
            }
            finally
            {   onComplete?.Invoke();
            }
        }
        public static async void DoRequest<T>(Task<T> task, Action<T> onComplete)
        {
            T result = default;
            string currentTrace = System.Environment.StackTrace;
            try
            {   result = await task;
            }
            catch (Exception e)
            {   Exception eFull = new Exception($"Call stack before async call:\n{currentTrace}\n", e);
                throw eFull;
            }
            finally
            {   onComplete?.Invoke(result);
            }
        }
    }
}
