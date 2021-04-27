using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Networking;
using System.Threading.Tasks;
using Unity.Services.Rooms.Helpers;
using Unity.Services.Rooms.Scheduler;
using TaskScheduler = Unity.Services.Rooms.Scheduler.TaskScheduler;

namespace Unity.Services.Rooms.Http
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
        private TaskScheduler _scheduler;

        public HttpClient(TaskScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Get(string url, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted,
            int requestTimeout = 10)
        {
            _scheduler.ScheduleMainThreadTask(() =>
            {
                _scheduler.StartCoroutine(ProcessRequest(UnityWebRequest.kHttpVerbGET, url, headers, null,
                    requestTimeout, onCompleted));
            });
        }

        public void Delete(string url, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted,
            int requestTimeout = 10)
        {
            _scheduler.ScheduleMainThreadTask(() =>
            {
                _scheduler.StartCoroutine(ProcessRequest(UnityWebRequest.kHttpVerbDELETE, url, headers, null,
                    requestTimeout, onCompleted));
            });
        }

        public void Post(string url, byte[] body, Dictionary<string, string> headers,
            Action<HttpClientResponse> onCompleted, int requestTimeout = 10)
        {
            _scheduler.ScheduleMainThreadTask(() =>
            {
                _scheduler.StartCoroutine(ProcessRequest(UnityWebRequest.kHttpVerbPOST, url, headers, body,
                    requestTimeout, onCompleted));
            });
        }

        public void Put(string url, byte[] body, Dictionary<string, string> headers,
            Action<HttpClientResponse> onCompleted, int requestTimeout = 10)
        {
            _scheduler.ScheduleMainThreadTask(() =>
            {
                _scheduler.StartCoroutine(ProcessRequest(UnityWebRequest.kHttpVerbPUT, url, headers, body,
                    requestTimeout, onCompleted));
            });
        }

        public void MakeRequest(string method, string url, byte[] body, Dictionary<string, string> headers,
            Action<HttpClientResponse> onCompleted, int requestTimeout = 10)
        {
            _scheduler.ScheduleMainThreadTask(() =>
            {
                _scheduler.StartCoroutine(ProcessRequest(method.ToUpper(), url, headers, body, requestTimeout,
                    onCompleted));
            });
        }

        public async Task<HttpClientResponse> MakeRequestAsync(string method, string url, byte[] body,
            Dictionary<string, string> headers, int requestTimeout = 10)
        {
            return await CreateWebRequestAsync(method.ToUpper(), url, body, headers);
        }

        private IEnumerator ProcessRequest(string method, string url, IDictionary<string, string> headers, byte[] body,
            int requestTimeout, Action<HttpClientResponse> onCompleted)
        {
            UnityWebRequestAsyncOperation SetupRequest(int attempt)
            {
//                if (attempt > 1)
//                {
//                    headers.Remove("X-Unity-CloudSave-Retry");
//                    headers.Add("X-Unity-CloudSave-Retry", attempt.ToString());
//                }

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
            var request = new UnityWebRequest(url, method);
            foreach (var header in headers)
            {
                if (DisallowedHeaders.Contains(header.Key.ToLower()))
                {
                    continue;
                }


                request.SetRequestHeader(header.Key, header.Value);
            }

            request.timeout = requestTimeout;
            if (body != null && (method == UnityWebRequest.kHttpVerbPOST || method == UnityWebRequest.kHttpVerbPUT || method == "PATCH"))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        private async Task<HttpClientResponse> CreateWebRequestAsync(string method, string url, byte[] body,
        IDictionary<string, string> headers, int requestTimeout = 10)
        {
            var result = await await Task.Factory.StartNew(async () =>
                {
                    var request = new UnityWebRequest(url, method);
                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }

                    request.timeout = 10;
                    if (body != null && (method == UnityWebRequest.kHttpVerbPOST ||
                                         method == UnityWebRequest.kHttpVerbPUT || 
                                         method == "PATCH"))
                    {
                        request.uploadHandler = new UploadHandlerRaw(body);
                    }

                    request.downloadHandler = new DownloadHandlerBuffer();

                    return await request.SendWebRequest();
                }, CancellationToken.None, TaskCreationOptions.None,
                Scheduler.ThreadHelper.TaskScheduler);
            return result;
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
