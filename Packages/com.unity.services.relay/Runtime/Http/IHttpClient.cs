using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Unity.Services.Relay.Http
{
    public interface IHttpClient
    {
        Task<HttpClientResponse> MakeRequestAsync(string method, string url, byte[] body, Dictionary<string, string> headers, int requestTimeout);
        Task<HttpClientResponse> MakeRequestAsync(string method, string url, List<IMultipartFormSection> body, Dictionary<string, string> headers, int requestTimeout, string boundary = null);
    }
}
