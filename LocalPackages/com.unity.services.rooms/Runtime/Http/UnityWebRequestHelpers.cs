using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Unity.Services.Rooms.Http
{
    public static class UnityWebRequestHelpers
    {
        public static TaskAwaiter<HttpClientResponse> GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<HttpClientResponse>();
            
            asyncOp.completed += obj =>
            {
                var result = CreateHttpClientResponse((UnityWebRequestAsyncOperation) obj);
                tcs.SetResult(result);
            };
            return tcs.Task.GetAwaiter();
        }

        internal static HttpClientResponse CreateHttpClientResponse(UnityWebRequestAsyncOperation unityResponse)
        {
            var response = unityResponse.webRequest;
            var result = new HttpClientResponse(
                response.GetResponseHeaders(),
                response.responseCode,
                response.isHttpError,
                response.isNetworkError,
                response.downloadHandler.data,
                response.error);
            return result;
        }
    }
}