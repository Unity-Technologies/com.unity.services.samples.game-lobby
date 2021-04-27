using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Services.Rooms.Http
{
    public class HttpClientResponse
    {
        public HttpClientResponse(Dictionary<string, string> headers, long responseCode, bool isHttpError, bool isNetworkError, byte[] data, string errorMessage)
        {
            Headers = headers;
            ResponseCode = responseCode;
            IsHttpError = isHttpError;
            IsNetworkError = isNetworkError;
            Data = data;
            ErrorMessage = errorMessage;
        }

        public Dictionary<string, string> Headers { get; }
        public long ResponseCode { get; }
        public bool IsHttpError { get; }
        public bool IsNetworkError { get; }
        public byte[] Data { get;}
        public string ErrorMessage { get; }
    }
}