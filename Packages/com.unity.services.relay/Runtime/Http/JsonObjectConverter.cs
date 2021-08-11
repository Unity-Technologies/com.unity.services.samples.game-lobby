using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;
using UnityEngine;

namespace Unity.Services.Relay.Http
{
    class JsonObjectConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JsonObject jobj = (JsonObject) value;
            JToken t = JToken.FromObject(jobj.obj);            
            t.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanConvert(System.Type objectType)
        {
            throw new System.NotImplementedException();
        }
    }
}
