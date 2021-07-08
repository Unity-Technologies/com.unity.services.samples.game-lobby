using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Editor.Models
{
    [Serializable]
    class IdProviderResponse
    {
        [Preserve]
        public IdProviderResponse() {}

        [JsonIgnore]
        public bool New;

        [JsonProperty("clientId")]
        public string ClientId;

        [JsonProperty("clientSecret")]
        public string ClientSecret;

        [JsonProperty("type")]
        public string Type;

        [JsonProperty("disabled")]
        public bool Disabled;

        public IdProviderResponse Clone()
        {
            return new IdProviderResponse
            {
                New = New,
                Type = Type,
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                Disabled = Disabled
            };
        }

        public override bool Equals(Object obj)
        {
            // Check for null and compare run-time types.
            if ((obj == null) || !GetType().Equals(obj.GetType()))
            {
                return false;
            }

            IdProviderResponse c = (IdProviderResponse)obj;
            return (New == c.New) &&
                (ClientId == c.ClientId) &&
                (ClientSecret == c.ClientSecret) &&
                (Disabled == c.Disabled) &&
                (Type == c.Type);
        }
    }
}
