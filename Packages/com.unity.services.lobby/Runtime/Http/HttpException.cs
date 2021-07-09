using System;
using Unity.Services.Lobbies;

namespace Unity.Services.Lobbies.Http
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

    [Serializable]
    public class HttpException<T> : HttpException
    {
        public T ActualError;

        public HttpException() : base()
        {
        }

        public HttpException(string message) : base(message)
        {
        }

        public HttpException(string message, Exception inner) : base(message, inner)
        {
        }

        public HttpException(HttpClientResponse response, T actualError) : base(response)
        {
            ActualError = actualError;
        }
    }
}
