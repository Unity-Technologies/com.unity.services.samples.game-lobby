using System;
using Unity.Services.Rooms.Scheduler;

namespace Unity.Services.Rooms.Http
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