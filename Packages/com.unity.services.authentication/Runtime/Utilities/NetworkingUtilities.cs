using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityEngine;

[assembly: InternalsVisibleTo("Unity.Services.Authentication.Editor")]
[assembly: InternalsVisibleTo("Unity.Services.Utilities.Tests")]

namespace Unity.Services.Authentication.Utilities
{
    interface INetworkingUtilities
    {
        IWebRequest<T> Get<T>(string url, IDictionary<string, string> headers = null, int maximumAttempts = 1);

        IWebRequest<T> PostJson<T>(string url, object payload, IDictionary<string, string> headers = null,
            int maximumAttempts = 1);

        IWebRequest<T> PostForm<T>(string url, string payload, IDictionary<string, string> headers = null,
            int maximumAttempts = 1);

        IWebRequest<T> Post<T>(string url, IDictionary<string, string> headers = null, int maximumAttempts = 1);

        IWebRequest<T> Put<T>(string url,  object payload, IDictionary<string, string> headers = null, int maximumAttempts = 1);

        IWebRequest<T> Delete<T>(string url, IDictionary<string, string> headers = null, int maximumAttempts = 1);
    }

    class NetworkingUtilities : INetworkingUtilities
    {
        readonly IScheduler m_Scheduler;
        readonly int m_DefaultRedirectLimit;

        public NetworkingUtilities(IScheduler scheduler)
        {
            m_Scheduler = scheduler;
        }

        /// <summary>
        /// The max redirect to follow. By default it's set to 0 and returns the raw 3xx response with a location header.
        /// </summary>
        public int RedirectLimit { get; set; }

        public IWebRequest<T> Get<T>(string url, IDictionary<string, string> headers = null, int maximumAttempts = 1)
        {
            var request = new WebRequest<T>(m_Scheduler,
                WebRequestVerb.Get,
                url,
                headers,
                string.Empty,
                string.Empty,
                RedirectLimit,
                maximumAttempts);

            if (m_Scheduler == null)
            {
                request.Send();
            }
            else
            {
                m_Scheduler.ScheduleAction(request.Send);
            }

            return request;
        }

        public IWebRequest<T> Post<T>(string url, IDictionary<string, string> headers = null, int maximumAttempts = 1)
        {
            var request = new WebRequest<T>(m_Scheduler,
                WebRequestVerb.Post,
                url,
                headers,
                string.Empty,
                string.Empty,
                RedirectLimit,
                maximumAttempts);

            if (m_Scheduler == null)
            {
                request.Send();
            }
            else
            {
                m_Scheduler.ScheduleAction(request.Send);
            }

            return request;
        }

        public IWebRequest<T> PostJson<T>(string url, object payload, IDictionary<string, string> headers = null, int maximumAttempts = 1)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);

            var request = new WebRequest<T>(m_Scheduler,
                WebRequestVerb.Post,
                url,
                headers,
                jsonPayload,
                "application/json",
                RedirectLimit,
                maximumAttempts);

            if (m_Scheduler == null)
            {
                request.Send();
            }
            else
            {
                m_Scheduler.ScheduleAction(request.Send);
            }

            return request;
        }

        public IWebRequest<T> PostForm<T>(string url, string payload, IDictionary<string, string> headers = null, int maximumAttempts = 1)
        {
            var request = new WebRequest<T>(m_Scheduler,
                WebRequestVerb.Post,
                url,
                headers,
                payload,
                "application/x-www-form-urlencoded",
                RedirectLimit,
                maximumAttempts);

            if (m_Scheduler == null)
            {
                request.Send();
            }
            else
            {
                m_Scheduler.ScheduleAction(request.Send);
            }

            return request;
        }

        public IWebRequest<T> Put<T>(string url, object payload, IDictionary<string, string> headers = null, int maximumAttempts = 1)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);

            var request = new WebRequest<T>(m_Scheduler,
                WebRequestVerb.Put,
                url,
                headers,
                jsonPayload,
                "application/json",
                RedirectLimit,
                maximumAttempts);

            if (m_Scheduler == null)
            {
                request.Send();
            }
            else
            {
                m_Scheduler.ScheduleAction(request.Send);
            }

            return request;
        }

        public IWebRequest<T> Delete<T>(string url, IDictionary<string, string> headers = null, int maximumAttempts = 1)
        {
            var request = new WebRequest<T>(m_Scheduler,
                WebRequestVerb.Delete,
                url,
                headers,
                string.Empty,
                string.Empty,
                RedirectLimit,
                maximumAttempts);

            if (m_Scheduler == null)
            {
                request.Send();
            }
            else
            {
                m_Scheduler.ScheduleAction(request.Send);
            }

            return request;
        }
    }
}
