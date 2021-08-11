using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Helpers;
using Unity.Services.Lobbies.Scheduler;
using TaskScheduler = Unity.Services.Lobbies.Scheduler.TaskScheduler;

namespace Unity.Services.Lobbies.Http
{
    public class HttpClient : IHttpClient
    {
        private static readonly HashSet<string> DisallowedHeaders = new HashSet<string>
        {
            "accept-charset", "access-control-request-headers", "access-control-request-method", "connection", "date",
            "dnt", "expect", "host", "keep-alive", "origin", "referer", "te", "trailer", "transfer-encoding", "upgrade",
            "via", "content-length", "x-unity-version", "user-agent", "cookie", "cookie2"
        };

        private static readonly List<int> ErrorCodes = new List<int> {408, 500, 502, 503, 504};

        public HttpClient()
        {
        }

        public async Task<HttpClientResponse> MakeRequestAsync(string method, string url, byte[] body,
            Dictionary<string, string> headers, int requestTimeout)
        {
            return await CreateWebRequestAsync(method.ToUpper(), url, body, headers, requestTimeout);
        }

        public async Task<HttpClientResponse> MakeRequestAsync(string method, string url,
            List<IMultipartFormSection> body,
            Dictionary<string, string> headers, int requestTimeout, string boundary = null)
        {
            return await CreateWebRequestAsync(method.ToUpper(), url, body, headers, requestTimeout, boundary);
        }

        private IEnumerator ProcessRequest(string method, string url, IDictionary<string, string> headers, byte[] body,
            int requestTimeout, Action<HttpClientResponse> onCompleted)
        {
            UnityWebRequestAsyncOperation SetupRequest(int attempt)
            {
                var webRequest = CreateWebRequest(method, url, body, headers, requestTimeout);
                return webRequest.SendWebRequest();
            }

            bool ShouldRetry(UnityWebRequestAsyncOperation request)
            {
                var responseCode = (int) request.webRequest.responseCode;
                return ErrorCodes.Contains(responseCode);
            }

            void AsyncOpCompleted(UnityWebRequestAsyncOperation request)
            {
                var internalResponse = UnityWebRequestHelpers.CreateHttpClientResponse(request);
                onCompleted(internalResponse);
            }

            yield return AsyncOpRetry.FromCreateAsync(SetupRequest)
                .WithRetryCondition(ShouldRetry)
                .WhenComplete(AsyncOpCompleted)
                .Run();
        }

        private UnityWebRequest CreateWebRequest(string method, string url, byte[] body,
            IDictionary<string, string> headers, int requestTimeout = 10)
        {
            using (var request = new UnityWebRequest(url, method))
            {
                foreach (var header in headers)
                {
                    if (DisallowedHeaders.Contains(header.Key.ToLower()))
                    {
                        continue;
                    }

                    request.SetRequestHeader(header.Key, header.Value);
                }

                request.timeout = requestTimeout;
                if (body != null && (method == UnityWebRequest.kHttpVerbPOST || method == UnityWebRequest.kHttpVerbPUT ||
                                    method == "PATCH"))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                return request;
            }
        }

        private async Task<HttpClientResponse> CreateWebRequestAsync(string method, string url, byte[] body,
            IDictionary<string, string> headers, int requestTimeout)
        {
            var result = await await Task.Factory.StartNew(async () =>
                {
                    using (var request = new UnityWebRequest(url, method))
                    {
                        foreach (var header in headers)
                        {
                            request.SetRequestHeader(header.Key, header.Value);
                        }

                        request.timeout = requestTimeout;
                        if (body != null && (method == UnityWebRequest.kHttpVerbPOST ||
                                            method == UnityWebRequest.kHttpVerbPUT ||
                                            method == "PATCH"))
                        {
                            request.uploadHandler = new UploadHandlerRaw(body);
                        }

                        request.downloadHandler = new DownloadHandlerBuffer();
                        return await request.SendWebRequest();
                    }
                    
                }, CancellationToken.None, TaskCreationOptions.None,
                Scheduler.ThreadHelper.TaskScheduler);
            return result;
        }

        private async Task<HttpClientResponse> CreateWebRequestAsync(string method, string url,
            List<IMultipartFormSection> body,
            IDictionary<string, string> headers, int requestTimeout, string boundary = null)
        {
            var result = await await Task.Factory.StartNew(async () =>
                {
                    byte[] boundaryBytes = string.IsNullOrEmpty(boundary)
                        ? UnityWebRequest.GenerateBoundary()
                        : Encoding.Default.GetBytes(boundary);
                    var request = new UnityWebRequest(url, method);

                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }

                    request.timeout = requestTimeout;
                    request = SetupMultipartRequest(request, body, boundaryBytes);
                    request.downloadHandler = new DownloadHandlerBuffer();

                    return await request.SendWebRequest();
                }, CancellationToken.None, TaskCreationOptions.None,
                Scheduler.ThreadHelper.TaskScheduler);
            return result;
        }

        private static UnityWebRequest SetupMultipartRequest(UnityWebRequest request,
            List<IMultipartFormSection> multipartFormSections, byte[] boundary)
        {
            byte[] data = (byte[]) null;
            if (multipartFormSections != null && (uint) multipartFormSections.Count > 0U)
            {
                data = UnityWebRequest.SerializeFormSections(multipartFormSections, boundary);
            }

            UploadHandler uploadHandler = (UploadHandler) new UploadHandlerRaw(data);
            uploadHandler.contentType =
                "multipart/form-data; boundary=" + Encoding.UTF8.GetString(boundary, 0, boundary.Length);
            request.uploadHandler = uploadHandler;
            request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();

            return request;
        }

        internal static HttpClientResponse CreateHttpClientResponse(UnityWebRequestAsyncOperation unityResponse)
        {
            var response = unityResponse.webRequest;
            var result = new HttpClientResponse(
                response.GetResponseHeaders(),
                response.responseCode,
#if UNITY_2020_1_OR_NEWER
                response.result == UnityWebRequest.Result.ProtocolError,
                response.result == UnityWebRequest.Result.ConnectionError,
#else
                response.isHttpError,
                response.isNetworkError,
#endif
                response.downloadHandler.data,
                response.error);
            return result;
        }
    }
}
