using System;

namespace Unity.Services.Rooms.Http
{
    [Serializable]
    public class DeserializationException : Exception
    {
        public HttpClientResponse response;

        public DeserializationException() : base()
        {
        }

        DeserializationException(string message) : base(message)
        {
        }

        DeserializationException(string message, Exception inner) : base(message, inner)
        {
        }

        public DeserializationException(HttpClientResponse httpClientResponse) : base(
            "Unable to Deserialize Http Client Response")
        {
            response = httpClientResponse;
        }
    }
}
