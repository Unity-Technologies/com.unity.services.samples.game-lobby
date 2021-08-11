using System;
using Unity.Services.Relay.Scheduler;

namespace Unity.Services.Relay.Http
{
    public abstract class BaseApiClient
    {
        protected readonly IHttpClient HttpClient;

        public BaseApiClient(IHttpClient httpClient)
        {
            HttpClient = httpClient ?? new HttpClient();
        }
    }
}