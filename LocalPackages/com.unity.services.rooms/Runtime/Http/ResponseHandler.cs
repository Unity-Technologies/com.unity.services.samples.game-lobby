using System;
using System.Text;
using Unity.Services.Rooms.Http;

namespace Unity.Services.Rooms.Http
{
    public static class ResponseHandler
    {
        public static bool TryDeserializeResponse<T>(HttpClientResponse response, out string decodedJsonString,
            out T dataObject)
        {
            var data = response.Data;
            decodedJsonString = Encoding.UTF8.GetString(data);
            var didDecodeSuccessfully = decodedJsonString.TryParseJson(out dataObject);
            return didDecodeSuccessfully;
        }

        public static IError DeserializeError(HttpClientResponse response)
        {
            var data = response.Data;
            var decodedJsonString = Encoding.UTF8.GetString(data);

            if (!decodedJsonString.TryParseJson(out BasicError error))
            {
                return CommonErrors.CreateUnspecifiedHttpError(decodedJsonString);
            }

            switch (error.Type)
            {
                case "problems/basic":
                    return error;
                default:
                    return CommonErrors.CreateUnspecifiedHttpError(decodedJsonString);
            }
        }

        public static void HandleAsyncResponse(HttpClientResponse response)
        {
            if (response.IsHttpError || response.IsNetworkError)
            {
                throw new HttpException(response);
            }
        }

        public static T HandleAsyncResponse<T>(HttpClientResponse response) where T : class
        {
            HandleAsyncResponse(response);

            var couldDeserialize = ResponseHandler.TryDeserializeResponse(response, out var decodedJson, out T result);
            if (!couldDeserialize)
            {
                throw new DeserializationException(response);
            }

            return result;
        }
    }
}
