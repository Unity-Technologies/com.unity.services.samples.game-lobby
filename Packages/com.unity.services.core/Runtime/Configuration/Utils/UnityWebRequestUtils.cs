using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Unity.Services.Core
{
    static class UnityWebRequestUtils
    {
        public static bool HasSucceeded(this UnityWebRequest self)
        {
#if UNITY_2020_1_OR_NEWER
            return self.result == UnityWebRequest.Result.Success;
#else
            return !self.isHttpError && !self.isNetworkError;
#endif
        }

        public static Task<string> GetTextAsync(string uri)
        {
            var completionSource = new TaskCompletionSource<string>();

            var request = UnityWebRequest.Get(uri);
            request.SendWebRequest()
                .completed += CompleteFetchTaskOnRequestCompleted;

            return completionSource.Task;

            void CompleteFetchTaskOnRequestCompleted(UnityEngine.AsyncOperation rawOperation)
            {
                var operation = (UnityWebRequestAsyncOperation)rawOperation;
                using (operation.webRequest)
                {
                    if (operation.webRequest.HasSucceeded())
                    {
                        completionSource.SetResult(operation.webRequest.downloadHandler.text);
                    }
                    else
                    {
                        var errorMessage = "Couldn't fetch config file." +
                            $"\nURL: {operation.webRequest.url}" +
                            $"\nReason: {operation.webRequest.error}";
                        completionSource.SetException(new Exception(errorMessage));
                    }
                }
            }
        }
    }
}
