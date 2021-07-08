using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Unity.Services.Relay.Models;

namespace Unity.Services.Relay.Http
{
    public static class ResponseHandler
    {
        public static T TryDeserializeResponse<T>(HttpClientResponse response)
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return JsonConvert.DeserializeObject<T>(GetDeserializardJson(response.Data), settings);
        }

        public static object TryDeserializeResponse(HttpClientResponse response, Type type)
        {
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return JsonConvert.DeserializeObject(GetDeserializardJson(response.Data), type, settings);
        }

        private static string GetDeserializardJson(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static void HandleAsyncResponse(HttpClientResponse response, Dictionary<string, Type> statusCodeToTypeMap)
        {
            if (statusCodeToTypeMap.ContainsKey(response.StatusCode.ToString()))
            {
                Type responseType = statusCodeToTypeMap[response.StatusCode.ToString()];
                if (responseType != null && response.IsHttpError || response.IsNetworkError)
                {
                    if (typeof(IOneOf).IsAssignableFrom(responseType))
                    {
                        var instance = CreateOneOfException(response, responseType);
                        throw instance;
                    }
                    else
                    {
                        var instance = CreateHttpException(response, responseType);
                        throw instance;
                    }
                }
            }
            else
            {
                throw new HttpException(response);
            }
        }

        private static HttpException CreateOneOfException(HttpClientResponse response, Type responseType)
        {
            try
            {
                var dataObject = ResponseHandler.TryDeserializeResponse(response, responseType);
                return CreateHttpException(response, ((IOneOf) dataObject).Type);
            }
            catch (ArgumentException e)
            {
                throw new DeserializationException(response, e.Message);
            }
            catch (MissingFieldException)
            {
                throw new DeserializationException(response,
                    "Discriminator field not found in the parsed json response.");
            }
            catch (DeserializationException e) when (e.response == null)
            {
                throw new DeserializationException(response, e.Message);
            }
            catch (DeserializationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new DeserializationException(response);
            }
        }

        private static HttpException CreateHttpException(HttpClientResponse response, Type responseType)
        {
            Type exceptionType = typeof(HttpException<>);
            var genericException = exceptionType.MakeGenericType(responseType);

            try
            {
                if (responseType == typeof(System.IO.Stream))
                {
                    var streamObject = (object)(response.Data == null ? new MemoryStream() : new MemoryStream(response.Data));
                    var streamObjectInstance = Activator.CreateInstance(genericException, new object[] {response, streamObject});
                    return (HttpException) streamObjectInstance;
                }

                var dataObject = ResponseHandler.TryDeserializeResponse(response, responseType);
                var instance = Activator.CreateInstance(genericException, new object[] {response, dataObject});
                return (HttpException) instance;
            }
            catch (ArgumentException e)
            {
                throw new DeserializationException(response, e.Message);
            }
            catch (MissingFieldException)
            {
                throw new DeserializationException(response,
                    "Discriminator field not found in the parsed json response.");
            }
            catch (DeserializationException e) when (e.response == null)
            {
                throw new DeserializationException(response, e.Message);
            }
            catch (DeserializationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new DeserializationException(response);
            }
        }

        public static T HandleAsyncResponse<T>(HttpClientResponse response, Dictionary<string, Type> statusCodeToTypeMap) where T : class
        {
            HandleAsyncResponse(response, statusCodeToTypeMap);

            try
            {
                if (statusCodeToTypeMap[response.StatusCode.ToString()] == typeof(System.IO.Stream))
                {
                    return (response.Data == null ? new MemoryStream() : new MemoryStream(response.Data)) as T;
                }
                return ResponseHandler.TryDeserializeResponse<T>(response);
            }
            catch (ArgumentException e)
            {
                throw new DeserializationException(response, e.Message);
            }
            catch (MissingFieldException)
            {
                throw new DeserializationException(response,
                    "Discriminator field not found in the parsed json response.");
            }
            catch (DeserializationException e) when (e.response == null)
            {
                throw new DeserializationException(response, e.Message);
            }
            catch (DeserializationException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new DeserializationException(response);
            }
        }
    }
}
