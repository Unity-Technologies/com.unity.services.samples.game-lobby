using System;

namespace Unity.Services.Lobbies.Http
{
    [Serializable]
    public class ResponseDeserializationException : Exception
    {
        public HttpClientResponse response;

        public ResponseDeserializationException() : base()
        {
        }

        public ResponseDeserializationException(string message) : base(message)
        {
        }

        ResponseDeserializationException(string message, Exception inner) : base(message, inner)
        {
        }

        public ResponseDeserializationException(HttpClientResponse httpClientResponse) : base(
            "Unable to Deserialize Http Client Response")
        {
            response = httpClientResponse;
        }

        public ResponseDeserializationException(HttpClientResponse httpClientResponse, string message) : base(
            message)
        {
            response = httpClientResponse;
        }
    }
}
