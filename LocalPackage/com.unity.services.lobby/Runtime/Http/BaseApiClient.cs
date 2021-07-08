using System;
using Unity.Services.Lobbies.Scheduler;

namespace Unity.Services.Lobbies.Http
{
    public abstract class BaseApiClient
    {
        protected readonly IHttpClient HttpClient;

        public BaseApiClient(IHttpClient httpClient, TaskScheduler scheduler)
        {
            HttpClient = httpClient ?? new HttpClient(scheduler);
        }
    }
}