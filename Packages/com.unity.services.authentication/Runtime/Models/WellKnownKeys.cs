using System;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Unity.Services.Authentication.Models
{
    class WellKnownKeys
    {
        [Preserve]
        public WellKnownKeys() {}

        [JsonProperty("keys")]
        public WellKnownKey[] Keys;
    }

    class WellKnownKey
    {
        [Preserve]
        public WellKnownKey() {}

        [JsonProperty("use")]
        public string Use;

        [JsonProperty("kty")]
        public string KeyType;

        [JsonProperty("kid")]
        public string KeyId;

        [JsonProperty("alg")]
        public string Algorithm;

        [JsonProperty("n")]
        public string N;

        [JsonProperty("e")]
        public string E;
    }
}
