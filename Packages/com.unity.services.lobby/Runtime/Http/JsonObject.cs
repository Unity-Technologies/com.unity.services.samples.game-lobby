using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Utilities;
using UnityEngine;

namespace Unity.Services.Lobbies.Http
{
    public class JsonObject
    {
        internal JsonObject(object obj)
        {
            this.obj = obj;
        }

        internal object obj;

        public string GetAsString()
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (System.Exception e)
            {
                throw new System.Exception("Failed to convert JsonObject to string.");
            }
        }

        public T GetAs<T>(DeserializationSettings deserializationSettings = null)
        {
            // Check if derializationSettings is null so we can use the default value.
            deserializationSettings = deserializationSettings ?? new DeserializationSettings();
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings
            {
                MissingMemberHandling = deserializationSettings.MissingMemberHandling == MissingMemberHandling.Error
                    ? Newtonsoft.Json.MissingMemberHandling.Error
                    : Newtonsoft.Json.MissingMemberHandling.Ignore
            };
            try
            {
                return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj), jsonSettings);
            }
            catch (Newtonsoft.Json.JsonSerializationException e)
            {
                throw new DeserializationException(e.Message);
            }
            catch (System.Exception e)
            {
                throw new DeserializationException("Unable to deserialize object.");
            }
        }
    }
}
