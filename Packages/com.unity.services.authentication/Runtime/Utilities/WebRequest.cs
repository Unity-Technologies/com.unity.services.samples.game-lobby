using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Unity.Services.Authentication.Utilities
{
    interface IWebRequest<T>
    {
        event Action<IWebRequest<T>> Completed;
        bool RequestFailed { get; }
        bool NetworkError { get; }
        bool ServerError { get; }
        string ErrorMessage { get; }
        T ResponseBody { get; }
        long ResponseCode { get; }
        IDictionary<string, string> ResponseHeaders { get; }
    }

    enum WebRequestVerb
    {
        Get,
        Post,
        Put,
        Delete
    }

    class WebRequest<T> : IWebRequest<T>
    {
        const int k_DefaultTimeoutSeconds = 10;
        const int k_RetryBackoffSeconds = 10;

        readonly IScheduler m_Scheduler;
        readonly ILogger m_Logger;
        readonly WebRequestVerb m_Verb;
        readonly string m_Url;
        readonly IDictionary<string, string> m_Headers;
        readonly string m_Payload;
        readonly string m_PayloadContentType;
        readonly int m_RedirectLimit;
        int m_Attempts;

        public event Action<IWebRequest<T>> Completed;
        public bool RequestFailed { get; private set; }
        public bool NetworkError { get; private set; }
        public bool ServerError { get; private set; }
        public string ErrorMessage { get; private set; }
        public T ResponseBody { get; private set; }
        public long ResponseCode { get; private set; }
        public IDictionary<string, string> ResponseHeaders { get; private set; }

        internal int RequestTimeout { get; set; }

        internal WebRequest(IScheduler scheduler,
                            ILogger logger,
                            WebRequestVerb verb,
                            string url,
                            IDictionary<string, string> headers,
                            string payload,
                            string payloadContentType,
                            int redirectLimit,
                            int attempts)
        {
            m_Scheduler = scheduler;
            m_Logger = logger;
            m_Verb = verb;
            m_Url = url;
            m_Headers = headers;
            m_Payload = payload;
            m_PayloadContentType = payloadContentType;
            m_RedirectLimit = redirectLimit;
            m_Attempts = attempts;

            RequestTimeout = k_DefaultTimeoutSeconds;
        }

        internal void Send()
        {
            UnityWebRequest unityWebRequest;
            switch (m_Verb)
            {
                case WebRequestVerb.Post:
                    if (string.IsNullOrEmpty(m_Payload))
                    {
                        unityWebRequest = UnityWebRequest.Post(m_Url, string.Empty);
                    }
                    else
                    {
                        var postBytes = Encoding.UTF8.GetBytes(m_Payload);
                        unityWebRequest = new UnityWebRequest(m_Url, UnityWebRequest.kHttpVerbPOST);
                        unityWebRequest.uploadHandler = new UploadHandlerRaw(postBytes)
                        {
                            contentType = m_PayloadContentType
                        };
                        unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
                    }

                    break;
                case WebRequestVerb.Get:
                    unityWebRequest = UnityWebRequest.Get(m_Url);
                    break;
                case WebRequestVerb.Put:
                    if (string.IsNullOrEmpty(m_Payload))
                    {
                        unityWebRequest = UnityWebRequest.Put(m_Url, string.Empty);
                    }
                    else
                    {
                        var putBytes = Encoding.UTF8.GetBytes(m_Payload);
                        unityWebRequest = new UnityWebRequest(m_Url, UnityWebRequest.kHttpVerbPUT);
                        unityWebRequest.uploadHandler = new UploadHandlerRaw(putBytes)
                        {
                            contentType = m_PayloadContentType
                        };
                        unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
                    }
                    break;
                case WebRequestVerb.Delete:
                    unityWebRequest = UnityWebRequest.Delete(m_Url);
                    break;
                default:
                    unityWebRequest = UnityWebRequest.Get(m_Url);
                    break;
            }

            if (m_Headers != null)
            {
                foreach (var headerAndValue in m_Headers)
                {
                    unityWebRequest.SetRequestHeader(headerAndValue.Key, headerAndValue.Value);
                }
            }

            unityWebRequest.redirectLimit = m_RedirectLimit;
            unityWebRequest.timeout = RequestTimeout;

            var asyncOperation = unityWebRequest.SendWebRequest();
            asyncOperation.completed += RequestCompleted;
            m_Attempts--;
        }

        internal void RequestCompleted(AsyncOperation asyncOperation)
        {
            var unityWebRequest = ((UnityWebRequestAsyncOperation)asyncOperation).webRequest;

            RequestCompleted(unityWebRequest.responseCode,
                RequestHasNetworkError(unityWebRequest),
                RequestHasServerError(unityWebRequest),
                unityWebRequest.error,
                unityWebRequest.downloadHandler?.text,
                unityWebRequest.GetResponseHeaders());
        }

        internal void RequestCompleted(long responseCode,
            bool hasNetworkError,
            bool hasServerError,
            string errorText,
            string bodyText,
            IDictionary<string, string> headers)
        {
            NetworkError = hasNetworkError;
            ServerError = hasServerError;

            if (hasNetworkError && m_Attempts > 0)
            {
                m_Logger.Warning("Network error detected, retrying...");

                m_Scheduler.ScheduleAction(Send, k_RetryBackoffSeconds);
            }
            else
            {
                RequestFailed = hasNetworkError || hasServerError;
                ResponseCode = responseCode;
                if (RequestFailed)
                {
                    // If this is a service error rather than a network error, return the response body
                    // as that is likely to contain a service-appropriate error message in this case.
                    if (hasServerError && !string.IsNullOrEmpty(bodyText))
                    {
                        ErrorMessage = bodyText;
                    }
                    else
                    {
                        ErrorMessage = errorText;
                    }

                    m_Logger.Warning("Request completed with error: {0}", ErrorMessage);
                }
                else
                {
                    ResponseHeaders = headers;

                    // Check if the response body has any contents to parse
                    if (string.IsNullOrEmpty(bodyText))
                    {
                        m_Logger.Info("Request completed successfully!");
                    }
                    else
                    {
                        try
                        {
                            ResponseBody = JsonConvert.DeserializeObject<T>(bodyText);

                            m_Logger.Info("Request completed successfully!");
                        }
                        catch (Exception ex)
                        {
                            m_Logger.Warning("Failed to deserialize object!" + ex.Message);
                            ErrorMessage = ex.Message;
                            RequestFailed = true;
                        }
                    }
                }

                Completed?.Invoke(this);
            }
        }

        bool RequestHasServerError(UnityWebRequest request)
        {
            return request.responseCode >= 400;
        }

        bool RequestHasNetworkError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError && request.error != "Redirect limit exceeded";
#else
            return request.isNetworkError && request.error != "Redirect limit exceeded";
#endif
        }
    }
}
