using System;
using Unity.Services.Rooms;

namespace Unity.Services.Rooms.Http
{
    [Serializable]
    public class HttpException : Exception
    {
        public HttpClientResponse Response;

        public HttpException() : base()
        {
        }

        public HttpException(string message) : base(message)
        {
        }

        public HttpException(string message, Exception inner) : base(message, inner)
        {
        }

        public HttpException(HttpClientResponse response) : base(response.ErrorMessage)
        {
            Response = response;
        }
    }
}
