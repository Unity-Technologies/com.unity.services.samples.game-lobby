using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Unity.Services.Lobbies.Http
{
    public interface IHttpClient
    {
        void Get(string url, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted, int requestTimeout = 10);
        void Delete(string url, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted, int requestTimeout = 10);
        void Post(string url, byte[] body, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted, int requestTimeout = 10);
        void Put(string url, byte[] body, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted, int requestTimeout = 10);
        void MakeRequest(string method, string url, byte[] body, Dictionary<string, string> headers, Action<HttpClientResponse> onCompleted, int requestTimeout = 10);
        Task<HttpClientResponse> MakeRequestAsync(string method, string url, byte[] body, Dictionary<string, string> headers, int requestTimeout);
        Task<HttpClientResponse> MakeRequestAsync(string method, string url, List<IMultipartFormSection> body, Dictionary<string, string> headers, int requestTimeout, string boundary = null);
    }
}
